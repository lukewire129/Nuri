using System.Runtime.CompilerServices;
using Nuri.Runtime.Diagnostics;

namespace Nuri.DevTools;

public static class NuriDevToolsExtensions
{
    private const string LateConfigurationMessage =
        "UseDebug was configured after the application started. Initial diagnostics may be incomplete. Configure UseDebug before Show() or Run().";
    private static readonly ConditionalWeakTable<object, LateWarningState> LateWarnings = new();

    public static THost UseDebug<THost>(this THost host)
        where THost : INuriDebugHost
    {
        return UseDebug(host, DebugKey.F12);
    }

    public static THost UseDebug<THost>(this THost host, DebugKey key)
        where THost : INuriDebugHost
    {
        if (host is null)
            throw new ArgumentNullException(nameof(host));

        if (key < DebugKey.F1 || key > DebugKey.F12)
            throw new ArgumentOutOfRangeException(nameof(key), key, "DebugKey must be between F1 and F12.");

        if (host.IsClosed)
            throw new ObjectDisposedException(host.GetType().FullName);

        var configuredLate = host.HasStarted;
        NuriDevTools.Enable();
        host.SetDebugShortcut(
            key,
            () => NuriDevTools.OpenInspector(host.HighlightComponent, host.CaptureSnapshot));

        if (configuredLate)
            WarnAboutLateConfiguration(host);

        return host;
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
