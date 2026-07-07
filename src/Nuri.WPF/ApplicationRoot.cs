using System;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using Nuri.Platform.Abstractions;
using Nuri.Runtime;
using Nuri.Runtime.Invalidation;
using Nuri.Runtime.Lifecycle;
using Nuri.UI;
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

        public void Rebuild()
        {
            if (_disposed)
                return;

            Coordinator.RebuildAll();
        }

        public void DispatchRebuild()
        {
            Scheduler.Schedule(Rebuild);
        }

        private void InitializeInternal(IElement rootElement, Window mainWindow)
        {
            var treePrefix = $"window{Interlocked.Increment(ref _nextTreeIndex)}";
            _treePrefix = treePrefix;
            rootElement.LoadNodeNumber(treePrefix, 0);
            Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(rootElement.Id, rootElement);

            _runtime = new ApplicationRuntime<IElement>(() =>
            {
                return rootElement;
            }, element => element.ToVirtualEntry());

            _host = new WpfApplicationHost(mainWindow);
            _scheduler = new WpfScheduler(() => _currentRootVisual?.Dispatcher ?? mainWindow.Dispatcher ?? Application.Current?.Dispatcher);
            _coordinator = new RenderCoordinator<IElement, FrameworkElement>(
                Runtime,
                new WpfRendererAdapter(),
                _host,
                () => _currentRootVisual,
                root => _currentRootVisual = root,
                _host.ApplyWindowProperties);

            Coordinator.Initialize();
        }

        public void ScheduleComponentRebuild(Component component)
        {
            if (!IsInThisTree(component))
                return;

            _invalidations.Enqueue(component);

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
                Rebuild(invalidation.Component, invalidation.ComponentId);
        }

        private void Rebuild(Component component, string componentId)
        {
            if (string.Equals(componentId, Runtime.CurrentVirtualEntry.Id, StringComparison.Ordinal))
            {
                Rebuild();
                return;
            }

            var oldEntry = Runtime.CurrentVirtualEntry.FindByComponentId(componentId)
                ?? Runtime.CurrentVirtualEntry.FindById(componentId);
            if (oldEntry == null)
            {
                Rebuild();
                return;
            }

            var newVisual = RenderComponentSubtree(component, componentId, oldEntry.ParentId);
            var newEntry = newVisual.ToVirtualEntry();
            if (!Coordinator.RebuildSubtree(oldEntry, newEntry, componentId))
            {
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

            var renderedId = GetRenderedRootId(component, renderedChild);
            renderedChild.ParentId = component.ParentId;
            renderedChild.Id = renderedId;
            Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(renderedId, renderedChild);
            return renderedChild;
        }

        private static string GetRenderedRootId(Component component, IElement rendered)
        {
            return !string.IsNullOrWhiteSpace(rendered.Key)
                ? component.Id + "#key:" + rendered.Key
                : component.Id;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            ComponentLifecycle.DisposeSubtree(_treePrefix + "_0");
            _disposed = true;
        }
    }
}
