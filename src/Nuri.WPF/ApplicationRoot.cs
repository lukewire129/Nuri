using System;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using Nuri.Platform.Abstractions;
using Nuri.Runtime;
using Nuri.Runtime.Diagnostics;
using Nuri.Runtime.Invalidation;
using Nuri.Runtime.Lifecycle;
using Nuri.UI;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.VirtualDom;
using Nuri.WPF.Diagnostics;
using Nuri.WPF.DevTools;

namespace Nuri.WPF
{
    public sealed class ApplicationRoot : IDisposable
    {
        private static int _nextTreeIndex;
        private FrameworkElement? _currentRootVisual;
        private IElement? _rootElement;
        private ApplicationRuntime<IElement>? _runtime;
        private RenderCoordinator<IElement, FrameworkElement>? _coordinator;
        private WpfApplicationHost? _host;
        private IUiScheduler? _scheduler;
        private string _treePrefix = string.Empty;
        private readonly ComponentInvalidationQueue _invalidations = new ComponentInvalidationQueue();
        private bool _rebuildScheduled;
        private bool _disposed;
        private ApplicationRuntime<IElement> Runtime => _runtime ?? throw new InvalidOperationException("ApplicationRoot is not initialized.");
        private RenderCoordinator<IElement, FrameworkElement> Coordinator => _coordinator ?? throw new InvalidOperationException("ApplicationRoot is not initialized.");
        private IUiScheduler Scheduler => _scheduler ?? throw new InvalidOperationException("ApplicationRoot is not initialized.");

        private ApplicationRoot()
        {
        }

        public static ApplicationRoot Initialize(IElement rootElement, Window mainWindow)
        {
            var instance = new ApplicationRoot();
            instance.InitializeInternal(rootElement, mainWindow);
            return instance;
        }

        public static ApplicationRoot Initialize(
            IElement rootElement,
            IHostAdapter<FrameworkElement> host,
            Func<Dispatcher?> dispatcherProvider,
            Action<IElement>? applyHostProperties = null)
        {
            var instance = new ApplicationRoot();
            var devTools = NuriApplication.LockDevToolsConfiguration();
            instance.InitializeInternal(rootElement, host, dispatcherProvider, applyHostProperties ?? (_ => { }), devTools);
            return instance;
        }

        public void Rebuild()
        {
            if (_disposed)
                return;

            Coordinator.RebuildAll();
        }

        public void ReplaceRoot(IElement rootElement, bool resetState)
        {
            if (_disposed)
                return;

            if (rootElement == null)
                throw new ArgumentNullException(nameof(rootElement));

            if (resetState)
                ComponentLifecycle.DisposeSubtree(_treePrefix + "_0");

            PrepareRoot(rootElement, _treePrefix);
            _rootElement = rootElement;
            Coordinator.RebuildAll();
        }

        public void DispatchRebuild()
        {
            Scheduler.Schedule(Rebuild);
        }

        private void InitializeInternal(IElement rootElement, Window mainWindow)
        {
            var devTools = NuriApplication.LockDevToolsConfiguration();

            InitializeInternal(
                rootElement,
                new WpfApplicationHost(mainWindow),
                () => _currentRootVisual?.Dispatcher ?? mainWindow.Dispatcher ?? Application.Current?.Dispatcher,
                element =>
                {
                    if (_host is not null)
                        _host.ApplyWindowProperties(element);
                },
                devTools);

            if (devTools.Enabled)
                NuriDevTools.AttachHotKey(mainWindow, devTools.ToggleKey);
        }

        private void InitializeInternal(
            IElement rootElement,
            IHostAdapter<FrameworkElement> host,
            Func<Dispatcher?> dispatcherProvider,
            Action<IElement> applyHostProperties,
            DevToolsConfiguration devTools)
        {
            if (devTools.Enabled)
                NuriDevTools.Enable();

            var treePrefix = $"window{Interlocked.Increment(ref _nextTreeIndex)}";
            _treePrefix = treePrefix;
            PrepareRoot(rootElement, treePrefix);
            _rootElement = rootElement;

            _runtime = new ApplicationRuntime<IElement>(() =>
            {
                return _rootElement ?? throw new InvalidOperationException("Application root element is not initialized.");
            }, element => element.ToVirtualEntry());

            _host = host as WpfApplicationHost;
            _scheduler = new WpfScheduler(dispatcherProvider);
            _coordinator = new RenderCoordinator<IElement, FrameworkElement>(
                Runtime,
                new WpfRendererAdapter(),
                host,
                () => _currentRootVisual,
                root => _currentRootVisual = root,
                applyHostProperties,
                _treePrefix);

            Coordinator.Initialize();
            NuriDiagnostics.RegisterRoot(_treePrefix, "WPF", () => _runtime?.CurrentVirtualEntry);
            WpfDevToolsRuntime.RegisterRoot(_treePrefix, () => _currentRootVisual, () => _runtime?.CurrentVirtualEntry);
        }

        private static void PrepareRoot(IElement rootElement, string treePrefix)
        {
            rootElement.LoadNodeNumber(treePrefix, 0);
            Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(rootElement.Id, rootElement);
        }

        public void ScheduleComponentRebuild(Component component)
        {
            if (!IsInThisTree(component))
                return;

            _invalidations.Enqueue(component);
            NuriDiagnostics.Log(RuntimeLogKind.ComponentInvalidated, _treePrefix, component.Id, "Component scheduled for rebuild.");

            if (_rebuildScheduled)
                return;

            _rebuildScheduled = true;
            Scheduler.Schedule(ProcessScheduledRebuild);
        }

        private void ProcessScheduledRebuild()
        {
            var dirtyComponents = _invalidations.DrainCoveredByParents();
            _rebuildScheduled = false;

            if (dirtyComponents.Count == 0)
                return;

            foreach (var invalidation in dirtyComponents)
            {
                NuriDiagnostics.Log(RuntimeLogKind.SubtreeRebuild, _treePrefix, invalidation.ComponentId, "Subtree rebuild scheduled.");
                Rebuild(invalidation.Component, invalidation.ComponentId);
            }
        }

        private void Rebuild(Component component, string componentId)
        {
            if (string.Equals(componentId, Runtime.CurrentVirtualEntry.Id, StringComparison.Ordinal))
            {
                NuriDiagnostics.Log(RuntimeLogKind.FullRebuild, _treePrefix, componentId, "Dirty root requested full rebuild.");
                Rebuild();
                return;
            }

            var oldEntry = Runtime.CurrentVirtualEntry.FindByComponentId(componentId)
                ?? Runtime.CurrentVirtualEntry.FindById(componentId);
            if (oldEntry == null)
            {
                NuriDiagnostics.Log(RuntimeLogKind.FullRebuild, _treePrefix, componentId, "Dirty subtree was not found.");
                Rebuild();
                return;
            }

            var newVisual = RenderComponentSubtree(component, componentId, oldEntry.ParentId);
            var newEntry = newVisual.ToVirtualEntry();
            if (!Coordinator.RebuildSubtree(oldEntry, newEntry, componentId))
            {
                NuriDiagnostics.Log(RuntimeLogKind.FullRebuild, _treePrefix, componentId, "Subtree replacement failed.");
                Rebuild();
            }
        }

        private bool IsInThisTree(Component component)
        {
            return !string.IsNullOrEmpty(component.Id)
                && component.Id.StartsWith(_treePrefix + "_", StringComparison.Ordinal);
        }

        private static IElement RenderComponentSubtree(Component component, string componentId, string? parentId)
        {
            component.Id = componentId;
            component.ParentId = parentId ?? string.Empty;
            component.ResetStateIndexForRender();
            var renderedChild = component.Render();
            component.CompleteRenderHooks();

            foreach (var property in component.Properties)
            {
                if (!renderedChild.Properties.ContainsKey(property.Key))
                    renderedChild.Properties[property.Key] = property.Value;
            }

            ApplyComponentKey(component, renderedChild);

            var renderedId = GetRenderedRootId(component, renderedChild);
            renderedChild.ParentId = component.ParentId;
            renderedChild.Id = renderedId;
            Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(renderedId, renderedChild);
            return renderedChild;
        }

        private static string GetRenderedRootId(Component component, IElement rendered)
        {
            if (!string.IsNullOrWhiteSpace(component.Key)
                && string.Equals(component.Key, rendered.Key, StringComparison.Ordinal))
                return component.Id;

            return !string.IsNullOrWhiteSpace(rendered.Key)
                ? component.Id + "#key:" + rendered.Key
                : component.Id;
        }

        private static void ApplyComponentKey(Component component, IElement rendered)
        {
            if (string.IsNullOrWhiteSpace(rendered.Key) && !string.IsNullOrWhiteSpace(component.Key))
                rendered.Key = component.Key;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_currentRootVisual != null)
                RemoveVirtualizationDiagnostics(_currentRootVisual);
            ComponentLifecycle.DisposeSubtree(_treePrefix + "_0");
            NuriDiagnostics.UnregisterRoot(_treePrefix);
            WpfDevToolsRuntime.UnregisterRoot(_treePrefix);
            _disposed = true;
        }

        private static void RemoveVirtualizationDiagnostics(FrameworkElement element)
        {
            if (element is WpfVirtualizedItemsHost virtualizedItems)
                virtualizedItems.RemoveDiagnostics();

            foreach (var child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is FrameworkElement childElement)
                    RemoveVirtualizationDiagnostics(childElement);
            }
        }
    }
}
