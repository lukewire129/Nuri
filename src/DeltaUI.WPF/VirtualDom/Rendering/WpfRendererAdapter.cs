using DeltaUI.Core.VirtualDom;
using DeltaUI.Core.VirtualDom.Rendering;
using System.Collections.Generic;
using System.Windows;

namespace DeltaUI.WPF
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
