using System.Diagnostics;
using System.Text;
using Nuri.Runtime.Diagnostics;

namespace Nuri.Diagnostics.Internal;

internal sealed class DiagnosticsConsoleWriter(TextWriter inner) : TextWriter
{
    public override Encoding Encoding => inner.Encoding;

    public override void WriteLine(string? value)
    {
        inner.WriteLine(value);
        WriteDiagnostics(value ?? string.Empty);
    }

    public override void Write(string? value)
    {
        inner.Write(value);
        if (!string.IsNullOrEmpty(value))
        {
            WriteDiagnostics(value);
        }
    }

    private static void WriteDiagnostics(string message)
    {
        if (!NuriDiagnostics.IsEnabled || string.IsNullOrEmpty(message))
        {
            return;
        }

        var caller = FindCaller();
        NuriDiagnostics.LogConsole(
            message,
            caller.SourceType,
            caller.SourceFile,
            caller.SourceMember,
            caller.SourceLine);
    }

    private static ConsoleCaller FindCaller()
    {
        var trace = new StackTrace(true);
        for (var index = 0; index < trace.FrameCount; index++)
        {
            var frame = trace.GetFrame(index);
            var method = frame?.GetMethod();
            var typeName = method?.DeclaringType?.FullName ?? string.Empty;

            if (string.IsNullOrWhiteSpace(typeName)
                || typeName.StartsWith("System.", StringComparison.Ordinal)
                || typeName.StartsWith("Nuri.Runtime.Diagnostics.", StringComparison.Ordinal)
                || typeName.StartsWith("Nuri.Diagnostics.", StringComparison.Ordinal)
                || typeName.StartsWith("Nuri.WPF.Diagnostics.", StringComparison.Ordinal)
                || typeName.StartsWith("Nuri.Duxel.Diagnostics.", StringComparison.Ordinal))
            {
                continue;
            }

            return new ConsoleCaller(
                typeName,
                frame?.GetFileName(),
                method?.Name,
                frame?.GetFileLineNumber() is > 0 ? frame.GetFileLineNumber() : null);
        }

        return new ConsoleCaller(null, null, null, null);
    }

    private sealed record ConsoleCaller(
        string? SourceType,
        string? SourceFile,
        string? SourceMember,
        int? SourceLine);
}
