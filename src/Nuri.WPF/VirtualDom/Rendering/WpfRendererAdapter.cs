using Nuri.VirtualDom;
using Nuri.VirtualDom.Rendering;
using System.Collections.Generic;
using System.Windows;

namespace Nuri.WPF
{
    public sealed class WpfRendererAdapter : IVirtualEntryRenderer<FrameworkElement>
    {
        public FrameworkElement Build(VirtualEntry entry)
        {
            return WpfVirtualEntryRenderer.Build(entry);
        }

        public void ApplyDiff(FrameworkElement root, IReadOnlyList<PatchOperation> operations)
        {
            WpfVirtualEntryRenderer.ApplyDiff(root, operations);
        }
    }
}
