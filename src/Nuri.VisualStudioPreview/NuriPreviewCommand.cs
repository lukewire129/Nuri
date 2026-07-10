using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Nuri.VisualStudioPreview;

internal sealed class NuriPreviewCommand
{
    private static readonly Guid CommandSet = new Guid("95fbb680-ad86-4e65-b8da-7fbd7e5f397f");
    private static readonly int[] CommandIds = { 0x0100, 0x0101, 0x0102 };
    private readonly AsyncPackage _package;

    private NuriPreviewCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package;
        foreach (var commandIdValue in CommandIds)
        {
            var commandId = new CommandID(CommandSet, commandIdValue);
            var command = new MenuCommand(Execute, commandId);
            commandService.AddCommand(command);
        }
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService != null)
            _ = new NuriPreviewCommand(package, commandService);
    }

    private void Execute(object sender, EventArgs e)
    {
        _package.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var window = await _package.ShowToolWindowAsync(
                typeof(NuriPreviewToolWindow),
                0,
                true,
                _package.DisposalToken) as NuriPreviewToolWindow;

            if (window?.Content is NuriPreviewControl control)
                await control.StartPreviewAsync(_package);
        }).FileAndForget("NuriPreview/OpenToolWindow");
    }
}
