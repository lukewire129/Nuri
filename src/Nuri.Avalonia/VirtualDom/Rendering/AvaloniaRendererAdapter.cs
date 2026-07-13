using System.Collections.Generic;
using Avalonia.Controls;
using Nuri.Platform.Abstractions;
using Nuri.VirtualDom;

namespace Nuri.Avalonia
{
    public sealed class AvaloniaRendererAdapter : IRendererAdapter<Control>
    {
        public Control Build(VirtualEntry entry)
        {
            return AvaloniaVirtualEntryRenderer.Build(entry);
        }

        public void ApplyDiff(Control root, IReadOnlyList<PatchOperation> operations)
        {
            AvaloniaVirtualEntryRenderer.ApplyDiff(root, operations);
        }
    }
}
