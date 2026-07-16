using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;

namespace Nuri.UI.Virtualization
{
    public interface IVirtualizedItemsSource
    {
        int Count { get; }

        double ItemExtent { get; }

        bool MeasuresItemExtent { get; }

        int BufferBefore { get; }

        int BufferAfter { get; }

        double BufferBeforePixels { get; }

        double BufferAfterPixels { get; }

        string GetKey(int index);

        IReadOnlyList<string> GetIdentities();

        IElement RenderItem(int index);

        bool ItemsEqual(int index, IVirtualizedItemsSource other, int otherIndex);

        bool HasSameTemplate(IVirtualizedItemsSource other);
    }

    internal sealed class VirtualizedItemsSource<T> : IVirtualizedItemsSource
    {
        private readonly IReadOnlyList<T> _items;
        private readonly Func<T, string>? _itemKey;
        private readonly Func<T, IElement> _itemTemplate;
        private readonly IEqualityComparer<T> _comparer;
        private string[]? _identities;

        public VirtualizedItemsSource(
            IReadOnlyList<T> items,
            Func<T, string>? itemKey,
            double itemExtent,
            bool measuresItemExtent,
            int bufferBefore,
            int bufferAfter,
            double bufferBeforePixels,
            double bufferAfterPixels,
            Func<T, IElement> itemTemplate,
            IEqualityComparer<T>? comparer)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            _items = items.ToArray();
            _itemKey = itemKey;
            _itemTemplate = itemTemplate ?? throw new ArgumentNullException(nameof(itemTemplate));
            _comparer = comparer ?? EqualityComparer<T>.Default;

            if (double.IsNaN(itemExtent) || double.IsInfinity(itemExtent) || itemExtent <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemExtent), "Item extent must be a finite value greater than zero.");
            if (bufferBefore < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferBefore), "Virtualization buffer must be zero or greater.");
            if (bufferAfter < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferAfter), "Virtualization buffer must be zero or greater.");
            if (double.IsNaN(bufferBeforePixels) || double.IsInfinity(bufferBeforePixels) || bufferBeforePixels < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferBeforePixels), "Virtualization pixel buffer must be a finite value zero or greater.");
            if (double.IsNaN(bufferAfterPixels) || double.IsInfinity(bufferAfterPixels) || bufferAfterPixels < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferAfterPixels), "Virtualization pixel buffer must be a finite value zero or greater.");

            ItemExtent = itemExtent;
            MeasuresItemExtent = measuresItemExtent;
            BufferBefore = bufferBefore;
            BufferAfter = bufferAfter;
            BufferBeforePixels = bufferBeforePixels;
            BufferAfterPixels = bufferAfterPixels;
        }

        public int Count => _items.Count;

        public double ItemExtent { get; }

        public bool MeasuresItemExtent { get; }

        public int BufferBefore { get; }

        public int BufferAfter { get; }

        public double BufferBeforePixels { get; }

        public double BufferAfterPixels { get; }

        public string GetKey(int index)
        {
            if (_itemKey == null)
                return $"index:{index}";

            var key = _itemKey(_items[index]);
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException($"Virtualized item at index {index} produced an empty key.");

            return key;
        }

        public IReadOnlyList<string> GetIdentities()
        {
            if (_identities != null)
                return _identities;

            var identities = new string[Count];
            var keys = new string[Count];
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < Count; index++)
            {
                var key = GetKey(index);
                keys[index] = key;
                counts.TryGetValue(key, out var count);
                counts[key] = count + 1;
            }

            var reported = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Count; index++)
            {
                var key = keys[index];
                identities[index] = counts[key] == 1 ? key : $"{key}#duplicate:{index}";
                if (counts[key] <= 1 || !reported.Add(key))
                    continue;

                var message = $"Duplicate virtualized item key '{key}'. Falling back to index-qualified identity.";
                Debug.WriteLine(message);
                NuriDiagnostics.Log(RuntimeLogKind.DuplicateKey, null, null, message);
            }

            _identities = identities;
            return identities;
        }

        public IElement RenderItem(int index)
        {
            var element = _itemTemplate(_items[index])
                ?? throw new InvalidOperationException($"Virtualized item template returned null for key '{GetKey(index)}'.");
            RejectComponents(element, GetKey(index));
            return element;
        }

        public bool ItemsEqual(int index, IVirtualizedItemsSource other, int otherIndex)
        {
            return other is VirtualizedItemsSource<T> typed
                && _comparer.Equals(_items[index], typed._items[otherIndex]);
        }

        public bool HasSameTemplate(IVirtualizedItemsSource other)
        {
            return other is VirtualizedItemsSource<T> typed
                && Equals(_itemTemplate, typed._itemTemplate);
        }

        private static void RejectComponents(IElement element, string key)
        {
            if (element is Component component)
                throw new InvalidOperationException($"Virtualized item template for key '{key}' contains component '{component.GetType().FullName}'. Item templates must contain stateless elements only.");

            foreach (var child in element.Children)
                RejectComponents(child, key);
        }
    }
}
