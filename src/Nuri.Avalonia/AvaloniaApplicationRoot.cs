using System;
using System.Threading;
using Avalonia.Controls;
using Nuri.Platform.Abstractions;
using Nuri.Runtime;
using Nuri.Runtime.Invalidation;
using Nuri.Runtime.Lifecycle;
using Nuri.UI;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.Avalonia
{
    public sealed class AvaloniaApplicationRoot : IDisposable
    {
        private static int _nextTreeIndex;
        private Control? _currentRootVisual;
        private IElement? _rootElement;
        private ApplicationRuntime<IElement>? _runtime;
        private RenderCoordinator<IElement, Control>? _coordinator;
        private IUiScheduler? _scheduler;
        private string _treePrefix = string.Empty;
        private readonly ComponentInvalidationQueue _invalidations = new ComponentInvalidationQueue();
        private bool _rebuildScheduled;
        private bool _disposed;
        private ApplicationRuntime<IElement> Runtime => _runtime ?? throw new InvalidOperationException("AvaloniaApplicationRoot is not initialized.");
        private RenderCoordinator<IElement, Control> Coordinator => _coordinator ?? throw new InvalidOperationException("AvaloniaApplicationRoot is not initialized.");
        private IUiScheduler Scheduler => _scheduler ?? throw new InvalidOperationException("AvaloniaApplicationRoot is not initialized.");

        private AvaloniaApplicationRoot()
        {
        }

        public static AvaloniaApplicationRoot Initialize(IElement rootElement, Window mainWindow)
        {
            if (mainWindow == null)
                throw new ArgumentNullException(nameof(mainWindow));

            var instance = new AvaloniaApplicationRoot();
            var host = new AvaloniaApplicationHost(mainWindow);
            instance.InitializeInternal(rootElement, host, new AvaloniaScheduler(), host.ApplyWindowProperties);
            return instance;
        }

        public static AvaloniaApplicationRoot Initialize(
            IElement rootElement,
            IHostAdapter<Control> host,
            IUiScheduler scheduler,
            Action<IElement>? applyHostProperties = null)
        {
            var instance = new AvaloniaApplicationRoot();
            instance.InitializeInternal(rootElement, host, scheduler, applyHostProperties ?? (_ => { }));
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

        private void InitializeInternal(
            IElement rootElement,
            IHostAdapter<Control> host,
            IUiScheduler scheduler,
            Action<IElement> applyHostProperties)
        {
            if (rootElement == null)
                throw new ArgumentNullException(nameof(rootElement));
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (applyHostProperties == null)
                throw new ArgumentNullException(nameof(applyHostProperties));

            _treePrefix = $"avalonia{Interlocked.Increment(ref _nextTreeIndex)}";
            PrepareRoot(rootElement, _treePrefix);
            _rootElement = rootElement;
            _scheduler = scheduler;

            _runtime = new ApplicationRuntime<IElement>(() =>
            {
                return _rootElement ?? throw new InvalidOperationException("Application root element is not initialized.");
            }, element => element.ToVirtualEntry());

            _coordinator = new RenderCoordinator<IElement, Control>(
                Runtime,
                new AvaloniaRendererAdapter(),
                host,
                () => _currentRootVisual,
                root => _currentRootVisual = root,
                applyHostProperties);

            Coordinator.Initialize();
        }

        private static void PrepareRoot(IElement rootElement, string treePrefix)
        {
            rootElement.LoadNodeNumber(treePrefix, 0);
            ElementTree<IElement, AnimationValue>.AssignDescendantIds(rootElement.Id, rootElement);
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
                Rebuild();
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
            ElementTree<IElement, AnimationValue>.AssignDescendantIds(renderedId, renderedChild);
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

            ComponentLifecycle.DisposeSubtree(_treePrefix + "_0");
            _disposed = true;
        }
    }
}
