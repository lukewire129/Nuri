using System;
using DeltaUI.Core.VirtualDom;

namespace DeltaUI.Core.Runtime
{
    public sealed class ApplicationRuntime<TElement>
    {
        private readonly object _rebuildLock = new object();
        private readonly Func<TElement> _renderRoot;
        private readonly Func<TElement, VirtualEntry> _toVirtualEntry;
        private TElement? _currentVisualNode;
        private VirtualEntry? _currentVirtualEntry;

        public ApplicationRuntime(Func<TElement> renderRoot, Func<TElement, VirtualEntry> toVirtualEntry)
        {
            _renderRoot = renderRoot ?? throw new ArgumentNullException(nameof(renderRoot));
            _toVirtualEntry = toVirtualEntry ?? throw new ArgumentNullException(nameof(toVirtualEntry));
        }

        public event EventHandler? StateIndexInitialize;

        public TElement CurrentVisualNode => _currentVisualNode ?? throw new InvalidOperationException("Application runtime is not initialized.");

        public VirtualEntry CurrentVirtualEntry => _currentVirtualEntry ?? throw new InvalidOperationException("Application runtime is not initialized.");

        public ApplicationRenderResult<TElement> Initialize()
        {
            var visualNode = _renderRoot();
            var virtualEntry = _toVirtualEntry(visualNode);

            _currentVisualNode = visualNode;
            _currentVirtualEntry = virtualEntry;

            return new ApplicationRenderResult<TElement>(visualNode, virtualEntry, Array.Empty<PatchOperation>());
        }

        public ApplicationRenderResult<TElement> CreateRebuild()
        {
            lock (_rebuildLock)
            {
                StateIndexInitialize?.Invoke(this, EventArgs.Empty);

                var visualNode = _renderRoot();
                var virtualEntry = _toVirtualEntry(visualNode);
                var operations = VirtualTreeDiff.Diff(CurrentVirtualEntry, virtualEntry);

                return new ApplicationRenderResult<TElement>(visualNode, virtualEntry, operations);
            }
        }

        public void Commit(ApplicationRenderResult<TElement> result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _currentVisualNode = result.VisualNode;
            _currentVirtualEntry = result.VirtualEntry;
        }

        public void CommitVirtualEntry(VirtualEntry virtualEntry)
        {
            _currentVirtualEntry = virtualEntry ?? throw new ArgumentNullException(nameof(virtualEntry));
        }
    }
}
