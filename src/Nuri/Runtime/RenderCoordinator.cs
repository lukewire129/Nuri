using System;
using Nuri.Platform.Abstractions;
using Nuri.Runtime.Lifecycle;
using Nuri.VirtualDom;

namespace Nuri.Runtime
{
    public sealed class RenderCoordinator<TElement, TNativeRoot>
    {
        private readonly ApplicationRuntime<TElement> _runtime;
        private readonly IRendererAdapter<TNativeRoot> _renderer;
        private readonly IHostAdapter<TNativeRoot> _host;
        private readonly Func<TNativeRoot?> _getCurrentRoot;
        private readonly Action<TNativeRoot> _setCurrentRoot;
        private readonly Action<TElement> _applyHostProperties;

        public RenderCoordinator(
            ApplicationRuntime<TElement> runtime,
            IRendererAdapter<TNativeRoot> renderer,
            IHostAdapter<TNativeRoot> host,
            Func<TNativeRoot?> getCurrentRoot,
            Action<TNativeRoot> setCurrentRoot,
            Action<TElement> applyHostProperties)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _getCurrentRoot = getCurrentRoot ?? throw new ArgumentNullException(nameof(getCurrentRoot));
            _setCurrentRoot = setCurrentRoot ?? throw new ArgumentNullException(nameof(setCurrentRoot));
            _applyHostProperties = applyHostProperties ?? throw new ArgumentNullException(nameof(applyHostProperties));
        }

        public VirtualEntry CurrentVirtualEntry => _runtime.CurrentVirtualEntry;

        public void Initialize()
        {
            var renderResult = _runtime.Initialize();
            _applyHostProperties(renderResult.VisualNode);
            var root = _renderer.Build(renderResult.VirtualEntry);
            _host.SetContent(root);
            _setCurrentRoot(root);
            ComponentLifecycle.FlushPendingEffects();
        }

        public void RebuildAll()
        {
            var root = _getCurrentRoot();
            if (root == null)
                return;

            var renderResult = _runtime.CreateRebuild();
            ComponentLifecycle.CleanupRemovedComponentState(renderResult.Operations);
            _applyHostProperties(renderResult.VisualNode);
            _renderer.ApplyDiff(root, renderResult.Operations);
            _runtime.Commit(renderResult);
            ComponentLifecycle.FlushPendingEffects();
        }

        public bool RebuildSubtree(VirtualEntry oldEntry, VirtualEntry newEntry, string componentId)
        {
            if (oldEntry == null)
                throw new ArgumentNullException(nameof(oldEntry));
            if (newEntry == null)
                throw new ArgumentNullException(nameof(newEntry));

            var root = _getCurrentRoot();
            if (root == null)
                return true;

            var operations = VirtualTreeDiff.Diff(oldEntry, newEntry);
            ComponentLifecycle.CleanupRemovedComponentState(operations);
            _renderer.ApplyDiff(root, operations);

            if (!_runtime.CurrentVirtualEntry.ReplaceDescendant(componentId, newEntry))
                return false;

            _runtime.CommitVirtualEntry(_runtime.CurrentVirtualEntry);
            ComponentLifecycle.FlushPendingEffects();
            return true;
        }
    }
}
