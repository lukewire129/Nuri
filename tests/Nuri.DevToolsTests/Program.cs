using Nuri.Constants;
using Nuri.Diagnostics.Internal;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.UI.Virtualization;

namespace Nuri.DevToolsTests;

internal static class Program
{
    private const string LateConfigurationMessage =
        "UseAttachDevTools was configured after the application started. Initial diagnostics may be incomplete. Configure UseAttachDevTools before Show() or Run().";

    private static void Main()
    {
        AttachDevToolsEnablesDiagnosticsAndConfiguresShortcuts();
        LateAttachDevToolsWarnsOnceAndContinues();
        ClosedAndInvalidHostsAreRejected();
        ComponentTreeUsesVirtualizedRows();
        DiagnosticListsUseVirtualizedRows();
        ConsoleSurfaceUsesStarHeight();
        InspectorSurfaceConstrainsScrollableContent();
        Console.WriteLine("Nuri.DevToolsTests passed.");
    }

    private static void AttachDevToolsEnablesDiagnosticsAndConfiguresShortcuts()
    {
        NuriDiagnostics.Disable();
        NuriDiagnostics.ClearLogs();
        var host = new FakeDebugHost();

        DevToolsRuntime.Configure(host, DebugKey.F12, static () => { });

        AssertEqual(true, NuriDiagnostics.IsEnabled, "UseAttachDevTools should enable diagnostics immediately.");
        AssertEqual(DebugKey.F12, host.Key, "UseAttachDevTools should configure F12 by default.");
        AssertEqual(1, host.ConfigurationCount, "UseAttachDevTools should configure one shortcut.");

        DevToolsRuntime.Configure(host, DebugKey.F1, static () => { });
        AssertEqual(DebugKey.F1, host.Key, "UseAttachDevTools should accept a custom function key.");
        AssertEqual(2, host.ConfigurationCount, "Repeated UseAttachDevTools should replace the host configuration through one setter.");
    }

    private static void LateAttachDevToolsWarnsOnceAndContinues()
    {
        NuriDiagnostics.ClearLogs();
        var host = new FakeDebugHost
        {
            HasStarted = true
        };

        DevToolsRuntime.Configure(host, DebugKey.F8, static () => { });
        DevToolsRuntime.Configure(host, DebugKey.F9, static () => { });

        AssertEqual(DebugKey.F9, host.Key, "Late UseAttachDevTools should still apply the latest shortcut.");
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
            () => DevToolsRuntime.Configure(closedHost, DebugKey.F12, static () => { }),
            "UseAttachDevTools should reject a closed host.");

        var activeHost = new FakeDebugHost();
        AssertThrows<ArgumentOutOfRangeException>(
            () => DevToolsRuntime.Configure(activeHost, (DebugKey)13, static () => { }),
            "UseAttachDevTools should reject keys outside F1 through F12.");
    }

    private static void ComponentTreeUsesVirtualizedRows()
    {
        var children = Enumerable.Range(0, 500)
            .Select(index => new ComponentSnapshot(
                $"child-{index}",
                "ChildComponent",
                "Div",
                $"child-{index}",
                index + 1,
                null,
                null,
                Array.Empty<HookSnapshot>(),
                Array.Empty<ComponentSnapshot>()))
            .ToArray();
        var rootComponent = new ComponentSnapshot(
            "tree-root",
            "TreeRoot",
            "Div",
            null,
            1,
            null,
            null,
            Array.Empty<HookSnapshot>(),
            children);
        var snapshot = new RuntimeSnapshot(
            DateTimeOffset.UtcNow,
            new[] { new ApplicationRootSnapshot("root-id", "WPF", rootComponent) },
            Array.Empty<StoreSnapshot>(),
            Array.Empty<RuntimeLogEntry>());
        var rows = RuntimeInspectorComponent.BuildTreeRows(
            snapshot,
            "child-42",
            new HashSet<string>(StringComparer.Ordinal),
            static _ => { },
            static _ => { });

        AssertEqual(502, rows.Count, "The flat tree model should contain one application root and every expanded component.");
        AssertEqual(true, rows.Single(row => row.NodeId == "component:child-42").Selected, "The selected component row should remain selected in the flat model.");

        var tree = (ItemsView)RuntimeInspectorComponent.BuildVirtualizedTree(rows);
        AssertEqual(ItemsTypes.Virtualized, tree.Kind, "The DevTools component tree should use the virtualized items contract.");
        var source = (IVirtualizedItemsSource)tree.Properties[PropertyKeys.VirtualizedItemsSource];
        AssertEqual(rows.Count, source.Count, "The virtualized source should receive the full lightweight row model.");
        AssertEqual(36d, source.ItemExtent, "The DevTools tree should use the fixed-extent virtualization fast path.");
        _ = source.RenderItem(42);

        var collapsedRows = RuntimeInspectorComponent.BuildTreeRows(
            snapshot,
            null,
            new HashSet<string>(StringComparer.Ordinal) { "component:tree-root" },
            static _ => { },
            static _ => { });
        AssertEqual(2, collapsedRows.Count, "Collapsing a component should remove its descendants from the visible row model.");
    }

    private static void DiagnosticListsUseVirtualizedRows()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var logs = Enumerable.Range(1, 500)
            .Select(index => new RuntimeLogEntry(
                index,
                timestamp.AddMilliseconds(index),
                index % 2 == 0 ? RuntimeLogKind.Console : RuntimeLogKind.ComponentRendered,
                "root-id",
                $"component-{index}",
                $"Message {index}",
                sourceType: "SampleComponent",
                sourceMember: "Render"))
            .ToArray();
        var snapshot = new RuntimeSnapshot(
            timestamp,
            Array.Empty<ApplicationRootSnapshot>(),
            Array.Empty<StoreSnapshot>(),
            logs);

        var runtimeList = (ItemsView)RuntimeInspectorComponent.BuildRuntimeLogs(snapshot);
        var runtimeSource = (IVirtualizedItemsSource)runtimeList.Properties[PropertyKeys.VirtualizedItemsSource];
        AssertEqual(200, runtimeSource.Count, "Runtime events should retain the existing visible-log limit.");
        AssertEqual(false, runtimeSource.MeasuresItemExtent, "Single-line runtime events should use fixed-extent virtualization.");
        AssertEqual(26d, runtimeSource.ItemExtent, "Runtime event virtualization should use the configured row extent.");
        _ = runtimeSource.RenderItem(0);

        var consoleList = (ItemsView)RuntimeInspectorComponent.BuildConsoleLogs(snapshot);
        var consoleSource = (IVirtualizedItemsSource)consoleList.Properties[PropertyKeys.VirtualizedItemsSource];
        AssertEqual(200, consoleSource.Count, "Console output should retain the existing visible-log limit.");
        AssertEqual(true, consoleSource.MeasuresItemExtent, "Console output should measure variable-height messages.");
        AssertEqual(58d, consoleSource.ItemExtent, "Console output should use its estimate until rows are measured.");
        _ = consoleSource.RenderItem(0);
    }

    private static void ConsoleSurfaceUsesStarHeight()
    {
        var snapshot = new RuntimeSnapshot(
            DateTimeOffset.UtcNow,
            Array.Empty<ApplicationRootSnapshot>(),
            Array.Empty<StoreSnapshot>(),
            Array.Empty<RuntimeLogEntry>());

        var console = (Div)RuntimeInspectorComponent.BuildConsole(snapshot);
        AssertEqual(DivTypes.Grid, console.Kind, "The Console surface should use a Grid so Duxel can assign the remaining height.");
        var rows = (IReadOnlyList<LengthValue>)console.Properties["RowDefinitions"];
        AssertEqual(1, rows.Count, "The Console surface Grid should contain one row.");
        AssertEqual(LengthUnit.Star, rows[0].Unit, "The Console surface should occupy a Star row.");
    }

    private static void InspectorSurfaceConstrainsScrollableContent()
    {
        var content = new Text("Scrollable content");
        var surface = (Div)RuntimeInspectorComponent.BuildSurface("Tree", content);

        AssertEqual(DivTypes.Grid, surface.Kind, "A DevTools surface should use Grid layout on WPF.");
        var rows = (IReadOnlyList<LengthValue>)surface.Properties["RowDefinitions"];
        AssertEqual(2, rows.Count, "A DevTools surface should contain heading and content rows.");
        AssertEqual(LengthUnit.Auto, rows[0].Unit, "The surface heading should use its natural height.");
        AssertEqual(LengthUnit.Star, rows[1].Unit, "The surface content should receive the constrained remaining height.");
        AssertEqual(1, content.Properties["Grid.Row"], "Scrollable content should occupy the Star row.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message} Expected: {expected}; Actual: {actual}");
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
