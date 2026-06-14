using System.Collections.Generic;
using Nuri.VirtualDom;

namespace Nuri.Runtime
{
    public sealed class ApplicationRenderResult<TElement>
    {
        public ApplicationRenderResult(TElement visualNode, VirtualEntry virtualEntry, IReadOnlyList<PatchOperation> operations)
        {
            VisualNode = visualNode;
            VirtualEntry = virtualEntry;
            Operations = operations;
        }

        public TElement VisualNode { get; }

        public VirtualEntry VirtualEntry { get; }

        public IReadOnlyList<PatchOperation> Operations { get; }
    }
}
