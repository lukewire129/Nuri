using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Nuri.VisualStudioPreview;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("Nuri Preview", "Visual Studio preview and format-on-save tooling for Nuri components.", "0.3.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideToolWindow(typeof(NuriPreviewToolWindow))]
[Guid(PackageGuidString)]
public sealed class NuriPreviewPackage : AsyncPackage
{
    public const string PackageGuidString = "1cb9d1ed-4f3d-4dd7-bf9d-8cf526062de7";
    private static readonly Guid OutputPaneGuid = new Guid("23984f29-df3d-4a98-8c80-315bced6e687");
    private IVsOutputWindowPane? _outputPane;

    protected override async System.Threading.Tasks.Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var outputWindow = await GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
        if (outputWindow != null)
        {
            var paneGuid = OutputPaneGuid;
            outputWindow.CreatePane(ref paneGuid, "Nuri Preview", 1, 1);
            outputWindow.GetPane(ref paneGuid, out _outputPane);
        }

        await NuriPreviewCommand.InitializeAsync(this);
        WriteLine("Nuri Preview package initialized.");
    }

    internal void WriteLine(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _outputPane?.OutputStringThreadSafe($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}
