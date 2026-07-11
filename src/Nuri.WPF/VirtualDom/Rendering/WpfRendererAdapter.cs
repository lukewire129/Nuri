using Nuri.VirtualDom;
using Nuri.Platform.Abstractions;
using System.Collections.Generic;
using System.Windows;

namespace Nuri.WPF
{
    public sealed class WpfRendererAdapter : IRendererAdapter<FrameworkElement>
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
