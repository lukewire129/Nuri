using System.Runtime.CompilerServices;
using Nuri.Runtime.Diagnostics;

namespace Nuri.Diagnostics.Internal;

internal static class DevToolsRuntime
{
    private const string LateConfigurationMessage =
        "UseAttachDevTools was configured after the application started. Initial diagnostics may be incomplete. Configure UseAttachDevTools before Show() or Run().";
    private static readonly object SyncRoot = new();
    private static readonly ConditionalWeakTable<object, LateWarningState> LateWarnings = new();
    private static bool _consoleCaptured;

    public static void Configure(INuriDebugHost host, DebugKey key, Action openInspector)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(openInspector);

        if (key < DebugKey.F1 || key > DebugKey.F12)
            throw new ArgumentOutOfRangeException(nameof(key), key, "DebugKey must be between F1 and F12.");
        if (host.IsClosed)
            throw new ObjectDisposedException(host.GetType().FullName);

        var configuredLate = host.HasStarted;
        Enable();
        host.SetDebugShortcut(key, openInspector);

        if (configuredLate)
            WarnAboutLateConfiguration(host);
    }

    public static void Enable()
    {
        NuriDiagnostics.Enable();
        lock (SyncRoot)
        {
            if (_consoleCaptured)
                return;

            Console.SetOut(new DiagnosticsConsoleWriter(Console.Out));
            _consoleCaptured = true;
        }
    }

    private static void WarnAboutLateConfiguration(INuriDebugHost host)
    {
        var warningState = LateWarnings.GetValue(host, static _ => new LateWarningState());
        if (Interlocked.Exchange(ref warningState.Written, 1) != 0)
            return;

        NuriDiagnostics.Log(RuntimeLogKind.Diagnostics, null, null, LateConfigurationMessage);
        System.Diagnostics.Debug.WriteLine(LateConfigurationMessage);
    }

    private sealed class LateWarningState
    {
        public int Written;
    }
}
