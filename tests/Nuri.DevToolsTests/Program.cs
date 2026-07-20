using Nuri.DevTools;
using Nuri.Runtime.Diagnostics;

namespace Nuri.DevToolsTests;

internal static class Program
{
    private const string LateConfigurationMessage =
        "UseDebug was configured after the application started. Initial diagnostics may be incomplete. Configure UseDebug before Show() or Run().";

    private static void Main()
    {
        UseDebugEnablesDiagnosticsAndConfiguresShortcuts();
        LateUseDebugWarnsOnceAndContinues();
        ClosedAndInvalidHostsAreRejected();
        Console.WriteLine("Nuri.DevToolsTests passed.");
    }

    private static void UseDebugEnablesDiagnosticsAndConfiguresShortcuts()
    {
        NuriDiagnostics.Disable();
        NuriDiagnostics.ClearLogs();
        var host = new FakeDebugHost();

        var returned = host.UseDebug();

        AssertSame(host, returned, "UseDebug should preserve the concrete host for fluent chaining.");
        AssertEqual(true, NuriDiagnostics.IsEnabled, "UseDebug should enable diagnostics immediately.");
        AssertEqual(DebugKey.F12, host.Key, "UseDebug should configure F12 by default.");
        AssertEqual(1, host.ConfigurationCount, "UseDebug should configure one shortcut.");

        host.UseDebug(DebugKey.F1);
        AssertEqual(DebugKey.F1, host.Key, "UseDebug should accept a custom function key.");
        AssertEqual(2, host.ConfigurationCount, "Repeated UseDebug should replace the host configuration through one setter.");
    }

    private static void LateUseDebugWarnsOnceAndContinues()
    {
        NuriDiagnostics.ClearLogs();
        var host = new FakeDebugHost
        {
            HasStarted = true
        };

        host.UseDebug(DebugKey.F8);
        host.UseDebug(DebugKey.F9);

        AssertEqual(DebugKey.F9, host.Key, "Late UseDebug should still apply the latest shortcut.");
        AssertEqual(
            1,
            NuriDiagnostics.GetSnapshot().RecentLogs.Count(entry =>
                entry.Kind == RuntimeLogKind.Diagnostics
                && entry.Message == LateConfigurationMessage),
            "Late UseDebug should record its guidance once per host.");
    }

    private static void ClosedAndInvalidHostsAreRejected()
    {
        var closedHost = new FakeDebugHost
        {
            IsClosed = true
        };
        AssertThrows<ObjectDisposedException>(
            () => closedHost.UseDebug(),
            "UseDebug should reject a closed host.");

        var activeHost = new FakeDebugHost();
        AssertThrows<ArgumentOutOfRangeException>(
            () => activeHost.UseDebug((DebugKey)13),
            "UseDebug should reject keys outside F1 through F12.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message} Expected: {expected}; Actual: {actual}");
    }

    private static void AssertSame(object expected, object actual, string message)
    {
        if (!ReferenceEquals(expected, actual))
            throw new InvalidOperationException(message);
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private sealed class FakeDebugHost : INuriDebugHost
    {
        public bool HasStarted { get; set; }

        public bool IsClosed { get; set; }

        public DebugKey Key { get; private set; }

        public int ConfigurationCount { get; private set; }

        public Action? OpenInspector { get; private set; }

        public void SetDebugShortcut(DebugKey key, Action openInspector)
        {
            Key = key;
            OpenInspector = openInspector;
            ConfigurationCount++;
        }

        public RuntimeSnapshot CaptureSnapshot()
        {
            return NuriDiagnostics.GetSnapshot();
        }

        public void HighlightComponent(string? componentId)
        {
        }
    }
}
