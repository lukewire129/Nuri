using System.Collections.Generic;
using Nuri.VirtualDom;

namespace Nuri.Platform.Abstractions
{
    public interface IRendererAdapter<TNativeRoot>
    {
        TNativeRoot Build(VirtualEntry entry);

        void ApplyDiff(TNativeRoot root, IReadOnlyList<PatchOperation> operations);
    }
}
