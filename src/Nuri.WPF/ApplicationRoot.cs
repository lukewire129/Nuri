using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;
using Nuri.Runtime;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.WPF
{
    public sealed class ApplicationRoot : IDisposable
    {
        private static int _nextTreeIndex;
        private FrameworkElement? _currentRootVisual;
        private ApplicationRuntime<IElement>? _runtime;
        private NuriServiceProvider _services = NuriServiceProvider.Empty;
        private ContentControl? _host;
        private Window? _mainWindow;
        private string _treePrefix = string.Empty;
        private readonly List<Component> _dirtyComponents = new List<Component>();
        private bool _rebuildScheduled;
        private bool _disposed;
        private ApplicationRuntime<IElement> Runtime => _runtime ?? throw new InvalidOperationException("ApplicationRoot is not initialized.");

        private ApplicationRoot()
        {
        }

        public NuriServiceProvider Services => _services;

        public FrameworkElement RootVisual => _currentRootVisual ?? throw new InvalidOperationException("Application root visual is not initialized.");

        public static ApplicationRoot Initialize(IElement rootElement, Window mainWindow, NuriServiceProvider? services = null)
        {
            var instance = new ApplicationRoot();
            instance.InitializeInternal(rootElement, mainWindow, mainWindow, services);
            return instance;
        }

        public static ApplicationRoot Initialize(IElement rootElement, ContentControl host, NuriServiceProvider? services = null)
        {
            var instance = new ApplicationRoot();
            instance.InitializeInternal(rootElement, host, host as Window, services);
            return instance;
        }

        public void Rebuild()
        {
            if (_disposed)
                return;

            if (_currentRootVisual == null)
                return;

            var renderResult = Runtime.CreateRebuild();
            if (_mainWindow != null)
                ApplyWindowProperties(_mainWindow, renderResult.VisualNode);
            WpfVirtualEntryRenderer.ApplyDiff(_currentRootVisual, renderResult.Operations);
            DisposeRemovedOwners(renderResult.Operations, _services);
            EnterAddedOwners(renderResult.Operations, _services);
            Runtime.Commit(renderResult);
        }

        public void DispatchRebuild()
        {
            var dispatcher = _currentRootVisual?.Dispatcher ?? _host?.Dispatcher ?? _mainWindow?.Dispatcher ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                Rebuild();
                return;
            }

            dispatcher.BeginInvoke((Action)Rebuild, DispatcherPriority.Render);
        }

        private void InitializeInternal(IElement rootElement, ContentControl host, Window? mainWindow, NuriServiceProvider? services)
        {
            var treePrefix = $"window{Interlocked.Increment(ref _nextTreeIndex)}";
            _services = services ?? NuriServiceProvider.Empty;
            _treePrefix = treePrefix;
            rootElement.LoadNodeNumber(treePrefix, 0);
            Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(rootElement.Id, rootElement);
            _host = host;
            _mainWindow = mainWindow;

            _runtime = new ApplicationRuntime<IElement>(() =>
            {
                return rootElement;
            }, element =>
            {
                using (NuriRuntimeContext.PushServices(_services))
                    return element.ToVirtualEntry();
            });

            var renderResult = Runtime.Initialize();
            if (mainWindow != null)
                ApplyWindowProperties(mainWindow, rootElement);

            var rootVisual = WpfVirtualEntryRenderer.Build(renderResult.VirtualEntry);

            host.Content = rootVisual;
            _currentRootVisual = rootVisual;
            EnterOwners(renderResult.VirtualEntry, Array.Empty<IDisposable>(), _services);
        }

        public void ScheduleComponentRebuild(Component component)
        {
            if (!IsInThisTree(component))
                return;

            if (!_dirtyComponents.Any(dirty => ReferenceEquals(dirty, component)))
                _dirtyComponents.Add(component);

            if (_rebuildScheduled)
                return;

            _rebuildScheduled = true;

            var dispatcher = _currentRootVisual?.Dispatcher ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                ProcessScheduledRebuild();
                return;
            }

            dispatcher.BeginInvoke((Action)ProcessScheduledRebuild, DispatcherPriority.Render);
        }

        private void ProcessScheduledRebuild()
        {
            var dirtyComponents = _dirtyComponents.ToList();
            _dirtyComponents.Clear();
            _rebuildScheduled = false;

            if (dirtyComponents.Count == 0)
                return;

            if (dirtyComponents.Any(component => string.Equals(component.Id, Runtime.CurrentVirtualEntry.Id, StringComparison.Ordinal)))
            {
                Rebuild();
                return;
            }

            foreach (var component in FilterCoveredDirtyComponents(dirtyComponents))
                Rebuild(component);
        }

        private void Rebuild(Component component)
        {
            if (_currentRootVisual == null)
                return;

            if (string.Equals(component.Id, Runtime.CurrentVirtualEntry.Id, StringComparison.Ordinal))
            {
                Rebuild();
                return;
            }

            var oldEntry = Runtime.CurrentVirtualEntry.FindById(component.Id);
            if (oldEntry == null)
            {
                Rebuild();
                return;
            }

            var newVisual = RenderComponentSubtree(component, _services);
            VirtualEntry newEntry;
            using (NuriRuntimeContext.PushServices(_services))
                newEntry = newVisual.ToVirtualEntry();
            var operations = VirtualTreeDiff.Diff(oldEntry, newEntry);

            WpfVirtualEntryRenderer.ApplyDiff(_currentRootVisual, operations);
            DisposeRemovedOwners(operations, _services);
            EnterAddedOwners(operations, _services);

            if (Runtime.CurrentVirtualEntry.ReplaceDescendant(component.Id, newEntry))
                Runtime.CommitVirtualEntry(Runtime.CurrentVirtualEntry);
            else
                Rebuild();
        }

        private static IEnumerable<Component> FilterCoveredDirtyComponents(IEnumerable<Component> dirtyComponents)
        {
            var ordered = dirtyComponents
                .Where(component => !string.IsNullOrEmpty(component.Id))
                .OrderBy(component => component.Id.Length)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var component = ordered[i];
                var isCoveredByParent = ordered.Take(i).Any(parent => IsDescendantId(component.Id, parent.Id));
                if (!isCoveredByParent)
                    yield return component;
            }
        }

        private static bool IsDescendantId(string childId, string parentId)
        {
            return childId.Length > parentId.Length
                && childId.StartsWith(parentId + "_", StringComparison.Ordinal);
        }

        private bool IsInThisTree(Component component)
        {
            return !string.IsNullOrEmpty(component.Id)
                && component.Id.StartsWith(_treePrefix + "_", StringComparison.Ordinal);
        }

        private static IElement RenderComponentSubtree(Component component, NuriServiceProvider services)
        {
            component.ResetStateIndexForRender();
            IElement renderedChild;
            using (NuriRuntimeContext.PushServices(services))
                renderedChild = component.Render();

            foreach (var property in component.Properties)
            {
                if (!renderedChild.Properties.ContainsKey(property.Key))
                    renderedChild.Properties[property.Key] = property.Value;
            }

            renderedChild.ParentId = component.ParentId;
            renderedChild.Id = component.Id;
            Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(component.Id, renderedChild);
            return renderedChild;
        }

        private static void ApplyWindowProperties(Window mainWindow, IElement rootElement)
        {
            if (rootElement.Properties.TryGetValue("Title", out var title) && title is string titleText)
                mainWindow.Title = titleText;

            if (rootElement.Properties.TryGetValue("Width", out var width) && width is not null)
                mainWindow.Width = Convert.ToDouble(width);

            if (rootElement.Properties.TryGetValue("Height", out var height) && height is not null)
                mainWindow.Height = Convert.ToDouble(height);
        }

        private static void DisposeRemovedOwners(IEnumerable<PatchOperation> operations, NuriServiceProvider services)
        {
            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case RemoveChildPatch removeChild:
                        DisposeOwners(removeChild.Child, Array.Empty<IDisposable>(), services);
                        break;
                    case ReplaceEntryPatch replace:
                        DisposeOwners(replace.OldEntry, CollectOwners(replace.NewEntry), services);
                        break;
                }
            }
        }

        private static void EnterAddedOwners(IEnumerable<PatchOperation> operations, NuriServiceProvider services)
        {
            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case AddChildPatch addChild:
                        EnterOwners(addChild.Child, Array.Empty<IDisposable>(), services);
                        break;
                    case ReplaceEntryPatch replace:
                        EnterOwners(replace.NewEntry, CollectOwners(replace.OldEntry), services);
                        break;
                }
            }
        }

        private static List<IDisposable> CollectOwners(VirtualEntry entry)
        {
            var owners = new List<IDisposable>();
            CollectOwners(entry, owners);
            return owners;
        }

        private static void CollectOwners(VirtualEntry entry, List<IDisposable> owners)
        {
            foreach (var owner in entry.Owners)
            {
                if (!owners.Any(existing => ReferenceEquals(existing, owner)))
                    owners.Add(owner);
            }

            foreach (var child in entry.Children)
                CollectOwners(child, owners);
        }

        private static void DisposeOwners(VirtualEntry entry, IReadOnlyList<IDisposable> retainedOwners, NuriServiceProvider services)
        {
            var owners = CollectOwners(entry);
            for (var i = owners.Count - 1; i >= 0; i--)
            {
                var owner = owners[i];
                if (retainedOwners.Any(retained => ReferenceEquals(retained, owner)))
                    continue;

                using (NuriRuntimeContext.PushServices(services))
                    owner.Dispose();
            }
        }

        private static void EnterOwners(VirtualEntry entry, IReadOnlyList<IDisposable> retainedOwners, NuriServiceProvider services)
        {
            foreach (var owner in CollectOwners(entry))
            {
                if (retainedOwners.Any(retained => ReferenceEquals(retained, owner)))
                    continue;

                if (owner is Component component)
                {
                    using (NuriRuntimeContext.PushServices(services))
                        component.Enter();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_runtime != null)
                DisposeOwners(_runtime.CurrentVirtualEntry, Array.Empty<IDisposable>(), _services);

            _disposed = true;
        }
    }
}
