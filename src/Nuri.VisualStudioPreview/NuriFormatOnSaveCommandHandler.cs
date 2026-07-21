using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Nuri.Formatting;

namespace Nuri.VisualStudioPreview;

[Export(typeof(ICommandHandler))]
[ContentType("CSharp")]
[Name("Nuri Format On Save")]
internal sealed class NuriFormatOnSaveCommandHandler : ICommandHandler<SaveCommandArgs>
{
    public string DisplayName => "Nuri Format On Save";

    public CommandState GetCommandState(SaveCommandArgs args)
    {
        return CommandState.Unspecified;
    }

    public bool ExecuteCommand(SaveCommandArgs args, CommandExecutionContext executionContext)
    {
        try
        {
            var snapshot = args.SubjectBuffer.CurrentSnapshot;
            var source = snapshot.GetText();
            var formatted = NuriCodeFormatter.Format(source);
            if (string.Equals(source, formatted, StringComparison.Ordinal))
            {
                return false;
            }

            using var edit = args.SubjectBuffer.CreateEdit();
            edit.Replace(new Span(0, snapshot.Length), formatted);
            edit.Apply();
        }
        catch (Exception exception)
        {
            Debug.WriteLine("Nuri format on save skipped: " + exception);
        }

        // Continue to Visual Studio's save handler after updating the buffer.
        return false;
    }
}
