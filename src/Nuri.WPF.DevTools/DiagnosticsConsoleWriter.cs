using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Nuri.Runtime.Diagnostics;

namespace Nuri.WPF.DevTools
{
    internal sealed class DiagnosticsConsoleWriter : TextWriter
    {
        private readonly TextWriter _inner;

        public DiagnosticsConsoleWriter(TextWriter inner)
        {
            _inner = inner;
        }

        public override Encoding Encoding => _inner.Encoding;

        public override void WriteLine(string? value)
        {
            _inner.WriteLine(value);
            WriteDiagnostics(value ?? string.Empty);
        }

        public override void Write(string? value)
        {
            _inner.Write(value);
            if (!string.IsNullOrEmpty(value))
                WriteDiagnostics(value);
        }

        private static void WriteDiagnostics(string message)
        {
            if (!NuriDiagnostics.IsEnabled || string.IsNullOrEmpty(message))
                return;

            var caller = FindCaller();
            NuriDiagnostics.LogConsole(message, caller.SourceType, caller.SourceFile, caller.SourceMember, caller.SourceLine);
        }

        private static ConsoleCaller FindCaller()
        {
            var trace = new StackTrace(true);
            for (var i = 0; i < trace.FrameCount; i++)
            {
                var frame = trace.GetFrame(i);
                var method = frame?.GetMethod();
                var declaringType = method?.DeclaringType;
                var typeName = declaringType?.FullName ?? string.Empty;

                if (string.IsNullOrWhiteSpace(typeName))
                    continue;

                if (typeName.StartsWith("System.", StringComparison.Ordinal)
                    || typeName.StartsWith("Nuri.Runtime.Diagnostics.", StringComparison.Ordinal)
                    || typeName.StartsWith("Nuri.WPF.DevTools.", StringComparison.Ordinal))
                    continue;

                return new ConsoleCaller(
                    typeName,
                    frame?.GetFileName(),
                    method?.Name,
                    frame?.GetFileLineNumber());
            }

            return new ConsoleCaller(null, null, null, null);
        }

        private sealed class ConsoleCaller
        {
            public ConsoleCaller(string? sourceType, string? sourceFile, string? sourceMember, int? sourceLine)
            {
                SourceType = sourceType;
                SourceFile = sourceFile;
                SourceMember = sourceMember;
                SourceLine = sourceLine > 0 ? sourceLine : null;
            }

            public string? SourceType { get; }

            public string? SourceFile { get; }

            public string? SourceMember { get; }

            public int? SourceLine { get; }
        }
    }
}
