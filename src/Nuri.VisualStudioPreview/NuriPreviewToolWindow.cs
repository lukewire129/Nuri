using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Nuri.VisualStudioPreview;

[Guid("fe29afad-c926-4380-94df-882617be7010")]
public sealed class NuriPreviewToolWindow : ToolWindowPane
{
    public NuriPreviewToolWindow()
        : base(null)
    {
        Caption = "Nuri Preview";
        Content = new NuriPreviewControl();
    }
}
