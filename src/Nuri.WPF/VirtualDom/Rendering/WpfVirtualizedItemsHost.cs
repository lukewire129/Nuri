using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Nuri.Runtime.Lifecycle;
using Nuri.Runtime.Diagnostics;
using Nuri.UI;
using Nuri.UI.Values;
using Nuri.UI.Virtualization;
using Nuri.VirtualDom;

namespace Nuri.WPF
{
    internal sealed class WpfVirtualizedItemsHost : ListBox
    {
        private const int IncrementalReconcileLimit = 256;
        private readonly BulkObservableCollection<ItemHandle> _items = new BulkObservableCollection<ItemHandle>();
        private readonly Dictionary<ListBoxItem, RealizedRow> _rows = new Dictionary<ListBoxItem, RealizedRow>();
        private IVirtualizedItemsSource? _source;

        public WpfVirtualizedItemsHost()
        {
            ItemsSource = _items;
            BorderThickness = new Thickness(0);
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            ScrollViewer.SetCanContentScroll(this, true);
            VirtualizingPanel.SetIsVirtualizing(this, true);
            VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
            VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
            VirtualizingPanel.SetCacheLength(this, new VirtualizationCacheLength(1));
            VirtualizingPanel.SetCacheLengthUnit(this, VirtualizationCacheLengthUnit.Page);

            var panelFactory = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
            ItemsPanel = new ItemsPanelTemplate(panelFactory);
            Loaded += (_, __) => RestoreRealizedRows();
            Unloaded += (_, __) =>
            {
                ClearRealizedRows();
                NuriDiagnostics.RemoveVirtualizedItems(this.GetUniqueId());
            };
        }

        internal int RealizedCount => _rows.Count;

        internal void RemoveDiagnostics()
        {
            NuriDiagnostics.RemoveVirtualizedItems(this.GetUniqueId());
        }

        internal void SetSource(IVirtualizedItemsSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _source = source;
            Reconcile(source);
            RefreshRealized(new HashSet<string>(source.GetIdentities(), StringComparer.Ordinal));
            RecordDiagnostics();
        }

        internal void ApplyPatch(UpdateVirtualizedItemsPatch patch)
        {
            _source = patch.Source;
            Reconcile(patch.Source);

            var changed = patch.RefreshRealizedItems
                ? new HashSet<string>(patch.Source.GetIdentities(), StringComparer.Ordinal)
                : new HashSet<string>(
                    patch.Changes
                        .Where(change => change.Type == VirtualizedItemChangeType.Update)
                        .Select(change => change.Key),
                    StringComparer.Ordinal);
            RefreshRealized(changed);
            RecordDiagnostics();
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ListBoxItem
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                IsTabStop = false
            };
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is not ListBoxItem container || item is not ItemHandle handle)
                return;

            container.Height = handle.Source.ItemExtent;
            RenderRow(container, handle);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is ListBoxItem container)
                ClearRow(container);
            base.ClearContainerForItemOverride(element, item);
        }

        private void Reconcile(IVirtualizedItemsSource source)
        {
            var identities = source.GetIdentities();
            if (_items.Count == identities.Count)
            {
                var sameOrder = true;
                for (var index = 0; index < identities.Count; index++)
                {
                    if (!string.Equals(_items[index].Identity, identities[index], StringComparison.Ordinal))
                    {
                        sameOrder = false;
                        break;
                    }
                }

                if (sameOrder)
                {
                    for (var index = 0; index < _items.Count; index++)
                        _items[index].Update(source, index);
                    return;
                }
            }

            var currentByIdentity = _items.ToDictionary(item => item.Identity, StringComparer.Ordinal);
            var desired = new ItemHandle[identities.Count];
            var retainedOldIndexes = new List<int>(Math.Min(_items.Count, identities.Count));
            var oldIndexes = new Dictionary<string, int>(currentByIdentity.Count, StringComparer.Ordinal);
            for (var index = 0; index < _items.Count; index++)
                oldIndexes[_items[index].Identity] = index;

            var added = 0;
            for (var index = 0; index < identities.Count; index++)
            {
                var identity = identities[index];
                if (currentByIdentity.TryGetValue(identity, out var retainedHandle))
                {
                    retainedHandle.Update(source, index);
                    desired[index] = retainedHandle;
                    retainedOldIndexes.Add(oldIndexes[identity]);
                }
                else
                {
                    desired[index] = new ItemHandle(identity, source, index);
                    added++;
                }
            }

            if (_items.Count == 0)
            {
                _items.ReplaceAll(desired);
                return;
            }

            var removed = _items.Count - retainedOldIndexes.Count;
            var moved = retainedOldIndexes.Count - LongestIncreasingSubsequenceLength(retainedOldIndexes);
            if (added + removed + moved > IncrementalReconcileLimit)
            {
                _items.ReplaceAll(desired);
                return;
            }

            var retained = new HashSet<string>(identities, StringComparer.Ordinal);
            for (var index = _items.Count - 1; index >= 0; index--)
            {
                if (!retained.Contains(_items[index].Identity))
                    _items.RemoveAt(index);
            }

            for (var targetIndex = 0; targetIndex < identities.Count; targetIndex++)
            {
                var identity = identities[targetIndex];
                var currentIndex = FindIdentity(identity, targetIndex);
                if (currentIndex < 0)
                {
                    _items.Insert(targetIndex, new ItemHandle(identity, source, targetIndex));
                }
                else if (currentIndex != targetIndex)
                {
                    _items.Move(currentIndex, targetIndex);
                }

                _items[targetIndex].Update(source, targetIndex);
            }

            while (_items.Count > identities.Count)
                _items.RemoveAt(_items.Count - 1);

            for (var index = 0; index < _items.Count; index++)
                _items[index].Update(source, index);
        }

        private static int LongestIncreasingSubsequenceLength(IReadOnlyList<int> values)
        {
            if (values.Count == 0)
                return 0;

            var tails = new int[values.Count];
            var length = 0;
            foreach (var value in values)
            {
                var low = 0;
                var high = length;
                while (low < high)
                {
                    var middle = low + ((high - low) / 2);
                    if (tails[middle] < value)
                        low = middle + 1;
                    else
                        high = middle;
                }

                tails[low] = value;
                if (low == length)
                    length++;
            }

            return length;
        }

        private int FindIdentity(string identity, int startIndex)
        {
            for (var index = startIndex; index < _items.Count; index++)
            {
                if (string.Equals(_items[index].Identity, identity, StringComparison.Ordinal))
                    return index;
            }

            return -1;
        }

        private void RefreshRealized(HashSet<string> changed)
        {
            if (changed.Count == 0)
                return;

            foreach (var handle in _items)
            {
                if (!changed.Contains(handle.Identity))
                    continue;

                if (ItemContainerGenerator.ContainerFromItem(handle) is ListBoxItem container)
                {
                    container.Height = handle.Source.ItemExtent;
                    RenderRow(container, handle);
                }
            }
        }

        private void RenderRow(ListBoxItem container, ItemHandle handle)
        {
            var element = handle.Source.RenderItem(handle.Index);
            var rowId = $"{this.GetUniqueId()}#item:{handle.Identity}";
            element.Id = rowId;
            element.ParentId = this.GetUniqueId();
            ElementTree<Nuri.UI.Dsl.IElement, AnimationValue>.AssignDescendantIds(rowId, element);
            var nextEntry = element.ToVirtualEntry();

            if (_rows.TryGetValue(container, out var current)
                && string.Equals(current.Identity, handle.Identity, StringComparison.Ordinal))
            {
                nextEntry.RewriteIdentity(current.Entry.Id, current.Entry.ParentId);
                var operations = VirtualTreeDiff.Diff(current.Entry, nextEntry);
                if (!operations.OfType<ReplaceEntryPatch>().Any(operation => operation.OldEntry.Id == current.Entry.Id))
                {
                    WpfVirtualEntryRenderer.ApplyDiff(current.NativeRoot, operations);
                    _rows[container] = new RealizedRow(handle.Identity, nextEntry, current.NativeRoot);
                    return;
                }
            }

            ClearRow(container);
            var nativeRoot = WpfVirtualEntryRenderer.Build(nextEntry);
            container.Content = nativeRoot;
            _rows[container] = new RealizedRow(handle.Identity, nextEntry, nativeRoot);
            RecordDiagnostics();
        }

        private void ClearRow(ListBoxItem container)
        {
            if (!_rows.TryGetValue(container, out var row))
                return;

            ComponentLifecycle.DisposeSubtree(row.Entry.Id);
            _rows.Remove(container);
            container.Content = null;
            RecordDiagnostics();
        }

        private void ClearRealizedRows()
        {
            foreach (var container in _rows.Keys.ToArray())
                ClearRow(container);
        }

        private void RestoreRealizedRows()
        {
            if (_source == null || _rows.Count != 0)
                return;

            RefreshRealized(new HashSet<string>(_source.GetIdentities(), StringComparer.Ordinal));
        }

        private void RecordDiagnostics()
        {
            NuriDiagnostics.RecordVirtualizedItems(this.GetUniqueId(), _source?.Count ?? 0, _rows.Count);
        }

        private sealed class ItemHandle
        {
            public ItemHandle(string identity, IVirtualizedItemsSource source, int index)
            {
                Identity = identity;
                Source = source;
                Index = index;
            }

            public string Identity { get; }
            public IVirtualizedItemsSource Source { get; private set; }
            public int Index { get; private set; }

            public void Update(IVirtualizedItemsSource source, int index)
            {
                Source = source;
                Index = index;
            }
        }

        private sealed class RealizedRow
        {
            public RealizedRow(string identity, VirtualEntry entry, FrameworkElement nativeRoot)
            {
                Identity = identity;
                Entry = entry;
                NativeRoot = nativeRoot;
            }

            public string Identity { get; }
            public VirtualEntry Entry { get; }
            public FrameworkElement NativeRoot { get; }
        }

        private sealed class BulkObservableCollection<T> : ObservableCollection<T>
        {
            public void ReplaceAll(IEnumerable<T> items)
            {
                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
