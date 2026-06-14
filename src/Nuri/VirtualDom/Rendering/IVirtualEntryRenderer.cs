using System.Collections.Generic;

namespace Nuri.VirtualDom.Rendering
{
    public interface IVirtualEntryRenderer<TNative>
    {
        TNative Build(VirtualEntry entry);

        void ApplyDiff(TNative root, IReadOnlyList<PatchOperation> operations);
    }
}
