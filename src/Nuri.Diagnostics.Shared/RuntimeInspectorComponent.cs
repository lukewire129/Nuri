using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.Diagnostics.Internal;

internal sealed class RuntimeInspectorComponent(
    Action<string?>? highlightRequested = null,
    Func<RuntimeSnapshot>? snapshotProvider = null) : Component
{
    private const int MaximumVisibleLogs = 200;
    private const double TreeRowExtent = 36;
    private const double RuntimeLogRowExtent = 26;
    private const double ConsoleLogEstimatedExtent = 58;
    private readonly Func<RuntimeSnapshot> _snapshotProvider = snapshotProvider ?? NuriDiagnostics.GetSnapshot;

    public override IElement Render()
    {
        var (mainTab, setMainTab) = useState(DevToolsTab.Inspector);
        var (detailTab, setDetailTab) = useState(DetailTab.Details);
        var (selectedComponentId, setSelectedComponentId) = useState<string?>(null);
        var (collapsedNodes, setCollapsedNodes) = useState(new HashSet<string>(StringComparer.Ordinal));
        var snapshot = _snapshotProvider();
        var selectedComponent = FindComponent(snapshot, selectedComponentId);

        void SelectComponent(ComponentSnapshot component)
        {
            setSelectedComponentId(_ => component.ComponentId);
            highlightRequested?.Invoke(component.ComponentId);
        }

        return Div(
                BuildHeader(snapshot),
                BuildMainTabs(mainTab, setMainTab),
                mainTab == DevToolsTab.Inspector
                    ? BuildInspector(
                        snapshot,
                        selectedComponent,
                        detailTab,
                        setDetailTab,
                        collapsedNodes,
                        setCollapsedNodes,
                        SelectComponent)
                    : BuildConsole(snapshot))
            .Background("#EAF0F7")
            .Padding(18)
            .Spacing(12);
    }

    private static IElement BuildHeader(RuntimeSnapshot snapshot)
    {
        var componentCount = snapshot.Roots.Sum(root => CountComponents(root.RootComponent));
        return Grid(
                Div(
                        Text("NURI DEVTOOLS").FontSize(12).FontColor("#7DD3FC"),
                        Text("Runtime inspector").FontSize(24).FontColor("#FFFFFF"),
                        Text("Inspect components, hooks, stores, and renderer activity in real time.")
                            .FontColor("#AFC4DF"))
                    .Spacing(4)
                    .Column(0),
                Div(
                        Text("LIVE SNAPSHOT").FontSize(11).FontColor("#7DD3FC"),
                        Text($"{snapshot.Roots.Count} roots  |  {componentCount} components  |  {snapshot.RecentLogs.Count} logs")
                            .FontColor("#F8FAFC"))
                    .Padding(14, 10, 14, 10)
                    .Spacing(3)
                    .Background("#1D3352")
                    .Brush("#2F4D73")
                    .Thickness(1)
                    .CornerRadius(8)
                    .Column(1))
            .Columns(Star, Auto)
            .Rows(Auto)
            .Padding(18)
            .Background("#14233A")
            .Brush("#223A5E")
            .Thickness(1)
            .CornerRadius(12);
    }

    private static IElement BuildMainTabs(
        DevToolsTab selected,
        Action<Func<DevToolsTab, DevToolsTab>> setSelected)
    {
        return Grid(
                    Div(DivTypes.Row,
                        TabButton("Inspector", selected == DevToolsTab.Inspector, () => setSelected(_ => DevToolsTab.Inspector)),
                        TabButton("Console", selected == DevToolsTab.Console, () => setSelected(_ => DevToolsTab.Console))
                        )
                        .Spacing(8)
                        .VCenter ()
                        .Column(0),
                    Button("Clear logs", NuriDiagnostics.ClearLogs)
                        .Size(100, 32)
                        .Background("#EEF3F8")
                        .FontColor("#334155")
                        .Brush("#CBD7E5")
                        .VCenter ()
                        .Thickness(1)
                        .Column(1)
                )
                .Columns(Star, Auto)
                .Rows(Auto)
                .Padding(18)
                .Background("#F8FAFD")
                .Brush("#D8E2EE")
                .Thickness(1)
                .CornerRadius(10);
    }

    private static IElement BuildInspector(
        RuntimeSnapshot snapshot,
        ComponentSnapshot? selectedComponent,
        DetailTab detailTab,
        Action<Func<DetailTab, DetailTab>> setDetailTab,
        HashSet<string> collapsedNodes,
        Action<Func<HashSet<string>, HashSet<string>>> setCollapsedNodes,
        Action<ComponentSnapshot> selectComponent)
    {
        var treeRows = BuildTreeRows(
            snapshot,
            selectedComponent?.ComponentId,
            collapsedNodes,
            setCollapsedNodes,
            selectComponent);

        return Grid(
                BuildSurface(
                        "Component tree",
                        BuildVirtualizedTree(treeRows),
                        $"{treeRows.Count:N0} visible"
                )
                    .Column(0),
                Grid(
                        BuildSurface(
                                "Component inspector",
                                BuildDetails(selectedComponent, detailTab, setDetailTab, snapshot))
                            .Row(0),
                        BuildSurface("Runtime events", BuildRuntimeLogs(snapshot))
                            .Row(1))
                    .Rows(Stars(2), Star)
                    .RowSpacing(10)
                    .Column(1))
            .Columns(Pixels(390), Star)
            .Rows(Star)
            .ColumnSpacing(10)
            .Grow();
    }

    internal static IReadOnlyList<InspectorTreeRow> BuildTreeRows(
        RuntimeSnapshot snapshot,
        string? selectedComponentId,
        HashSet<string> collapsedNodes,
        Action<Func<HashSet<string>, HashSet<string>>> setCollapsedNodes,
        Action<ComponentSnapshot> selectComponent)
    {
        var rows = new List<InspectorTreeRow>();
        foreach (var root in snapshot.Roots)
        {
            var nodeId = "root:" + root.RootId;
            var expanded = !collapsedNodes.Contains(nodeId);
            rows.Add(new InspectorTreeRow(
                nodeId,
                0,
                expanded,
                root.RootComponent is not null,
                $"{root.RootId} ({root.Renderer})  patches={root.PatchCount}",
                () => ToggleNode(nodeId, setCollapsedNodes),
                null,
                false,
                false,
                rows.Count));

            if (expanded && root.RootComponent is not null)
            {
                AddComponentRows(
                    root.RootComponent,
                    selectedComponentId,
                    1,
                    rows,
                    collapsedNodes,
                    setCollapsedNodes,
                    selectComponent);
            }
        }

        if (rows.Count == 0)
        {
            rows.Add(new InspectorTreeRow(
                "empty",
                0,
                false,
                false,
                "No diagnostic roots are registered.",
                static () => { },
                null,
                false,
                true,
                0));
        }

        return rows;
    }

    internal static IElement BuildVirtualizedTree(IReadOnlyList<InspectorTreeRow> rows)
    {
        return VirtualizedItems(
                rows,
                row => row.NodeId,
                TreeRowExtent,
                BuildTreeRow)
            .Grow();
    }

    private static void AddComponentRows(
        ComponentSnapshot component,
        string? selectedComponentId,
        int depth,
        ICollection<InspectorTreeRow> rows,
        HashSet<string> collapsedNodes,
        Action<Func<HashSet<string>, HashSet<string>>> setCollapsedNodes,
        Action<ComponentSnapshot> selectComponent)
    {
        var nodeId = "component:" + component.ComponentId;
        var expanded = !collapsedNodes.Contains(nodeId);
        rows.Add(new InspectorTreeRow(
            nodeId,
            depth,
            expanded,
            component.Children.Count > 0,
            $"{component.TypeName}  renders={component.RenderCount}",
            () => ToggleNode(nodeId, setCollapsedNodes),
            () => selectComponent(component),
            string.Equals(component.ComponentId, selectedComponentId, StringComparison.Ordinal),
            false,
            rows.Count));

        if (!expanded)
        {
            return;
        }

        foreach (var child in component.Children)
        {
            AddComponentRows(
                child,
                selectedComponentId,
                depth + 1,
                rows,
                collapsedNodes,
                setCollapsedNodes,
                selectComponent);
        }
    }

    private static IElement BuildTreeRow(InspectorTreeRow row)
    {
        if (row.IsPlaceholder)
        {
            return Div(Text(row.Label).FontColor("#64748B"))
                .Height(TreeRowExtent)
                .Padding(10, 8, 10, 8)
                .Background("#F7FAFC")
                .Key(row.NodeId);
        }

        var idleBackground = row.Depth == 0
            ? "#E7F0FA"
            : row.Index % 2 == 0 ? "#FBFDFF" : "#F5F8FC";
        var rowBackground = row.Selected ? "#DCEEFF" : idleBackground;
        var toggleButton = Button(
                row.HasChildren ? (row.Expanded ? "-" : "+") : ".",
                row.HasChildren ? row.Toggle : static () => { })
            .Size(30, 32)
            .Background(rowBackground)
            .FontColor(row.Selected ? "#0369A1" : "#64748B")
            .Brush(row.Selected ? "#7DD3FC" : "#D8E2EE")
            .Thickness(0)
            .Column(0);
        var selectButton = Button(row.Label, row.Select ?? row.Toggle)
            .Height(32)
            .TextStart()
            .Background(rowBackground)
            .FontColor(row.Selected ? "#075985" : row.Depth == 0 ? "#1E3A5F" : "#26364A")
            .Brush(row.Selected ? "#7DD3FC" : "#D8E2EE")
            .Thickness(0)
            .Column(1);

        return Grid(toggleButton, selectButton)
            .Key(row.NodeId)
            .Columns(Pixels(32), Star)
            .Rows(Pixels(32))
            .ColumnSpacing(2)
            .Background(rowBackground)
            .Margin(left: row.Depth * 14, bottom: 4);
    }

    private static IElement BuildDetails(
        ComponentSnapshot? component,
        DetailTab selectedTab,
        Action<Func<DetailTab, DetailTab>> setSelectedTab,
        RuntimeSnapshot snapshot)
    {
        var tabs = Div(
                DivTypes.Row,
                TabButton("Details", selectedTab == DetailTab.Details, () => setSelectedTab(_ => DetailTab.Details)),
                TabButton("Hooks", selectedTab == DetailTab.Hooks, () => setSelectedTab(_ => DetailTab.Hooks)),
                TabButton("Stores", selectedTab == DetailTab.Stores, () => setSelectedTab(_ => DetailTab.Stores)))
            .Spacing(6);

        IElement content = selectedTab switch
        {
            DetailTab.Hooks => BuildHooks(component),
            DetailTab.Stores => BuildStores(snapshot),
            _ => BuildComponentDetails(component)
        };

        return Div(tabs, Div(DivTypes.Scroll, content).Grow()).Spacing(8);
    }

    private static IElement BuildComponentDetails(ComponentSnapshot? component)
    {
        if (component is null)
        {
            return Div(
                    Text("Select a component").FontSize(18).FontColor("#1F1F1F"),
                    Text("Choose a node from the component tree to inspect it.").FontColor("#616161"))
                .Spacing(6);
        }

        return Div(
                Text(component.TypeName).FontSize(18).FontColor("#1F1F1F"),
                DetailLine("ComponentId", component.ComponentId),
                DetailLine("Virtual entry", component.EntryType),
                DetailLine("Key", component.Key ?? string.Empty),
                DetailLine("Render count", component.RenderCount.ToString()),
                DetailLine("Last invalidated", component.LastInvalidatedSequence?.ToString() ?? string.Empty),
                DetailLine("Last rendered", component.LastRenderedSequence?.ToString() ?? string.Empty))
            .Spacing(5);
    }

    private static IElement BuildHooks(ComponentSnapshot? component)
    {
        if (component is null)
        {
            return Text("Select a component to inspect hooks.").FontColor("#616161");
        }

        var rows = component.Hooks
            .Select(hook => (IElement)Text($"#{hook.Index}  {hook.Kind}  <{hook.DisplayType}>  {hook.Summary}"))
            .ToArray();
        return rows.Length == 0
            ? Text("This component has no recorded hooks.").FontColor("#616161")
            : Div(rows).Spacing(5);
    }

    private static IElement BuildStores(RuntimeSnapshot snapshot)
    {
        var rows = new List<IElement>();
        foreach (var store in snapshot.Stores)
        {
            rows.Add(Text($"{store.StoreType}  {store.StoreId} = {store.ValueSummary}").FontColor("#1F1F1F"));
            rows.AddRange(store.Subscriptions.Select(subscription =>
                (IElement)Text($"  -> {subscription.ComponentId} hook #{subscription.HookIndex} selected {subscription.SelectedType} = {subscription.SelectedValueSummary}")
                    .FontColor("#616161")));
        }

        return rows.Count == 0
            ? Text("No stores are registered.").FontColor("#616161")
            : Div(rows.ToArray()).Spacing(5);
    }

    internal static IElement BuildRuntimeLogs(RuntimeSnapshot snapshot)
    {
        var logs = snapshot.RecentLogs
            .Where(log => log.Kind != RuntimeLogKind.Console && log.Kind != RuntimeLogKind.AppLog)
            .Reverse()
            .Take(MaximumVisibleLogs)
            .ToArray();

        return logs.Length == 0
            ? Text("No runtime events.").FontColor("#616161")
            : VirtualizedItems(
                    logs,
                    log => $"runtime:{log.Sequence}",
                    RuntimeLogRowExtent,
                    RenderRuntimeLog)
                .Grow();
    }

    internal static IElement BuildConsole(RuntimeSnapshot snapshot)
    {
        return Grid(
                BuildSurface(
                        "Console output",
                        BuildConsoleLogs(snapshot))
                    .Row(0))
            .Rows(Star)
            .Grow();
    }

    internal static IElement BuildConsoleLogs(RuntimeSnapshot snapshot)
    {
        var logs = snapshot.RecentLogs
            .Where(log => log.Kind is RuntimeLogKind.Console or RuntimeLogKind.AppLog or RuntimeLogKind.FullRebuild)
            .Reverse()
            .Take(MaximumVisibleLogs)
            .ToArray();

        return logs.Length == 0
            ? Text("No console output.").FontColor("#616161")
            : VirtualizedItems(
                    logs,
                    RenderConsoleLog,
                    estimatedItemExtent: ConsoleLogEstimatedExtent,
                    bufferPixels: 320,
                    itemKey: log => $"console:{log.Sequence}")
                .Grow();
    }

    private static IElement RenderRuntimeLog(RuntimeLogEntry log)
    {
        var background = log.Kind is RuntimeLogKind.UnsupportedEvent
            or RuntimeLogKind.UnsupportedProperty
            or RuntimeLogKind.DuplicateKey
            ? "#FFF7E6"
            : "#F7FAFC";

        return Div(
                Text($"{log.LocalTime}  {log.Kind,-20}  {log.Message}")
                    .FontColor(LogColor(log.Kind)))
            .Height(RuntimeLogRowExtent)
            .Padding(6, 3, 6, 3)
            .Background(background);
    }

    private static IElement RenderConsoleLog(RuntimeLogEntry log)
    {
        return Div(
                Text($"{log.LocalTime}  {log.Message}").FontColor("#26364A"),
                Text(log.SourceDisplay).FontColor("#708197"))
            .Spacing(2)
            .Padding(8)
            .Margin(bottom: 6)
            .Background("#F5F8FC")
            .Brush("#DCE5F0")
            .Thickness(1)
            .CornerRadius(6);
    }

    internal static IElement BuildSurface(string title, IElement content, string? caption = null)
    {
        var heading = Grid(
                Text(title).FontSize(15).FontColor("#26364A").Column(0),
                Text(caption ?? string.Empty).FontSize(12).FontColor("#708197").End().Column(1))
            .Columns(Star, Auto)
            .Rows(Auto);

        return Grid(
                heading.Row(0),
                content.Row(1))
            .Rows(Auto, Star)
            .Padding(12)
            .RowSpacing(10)
            .Background(Colors.White)
            .Brush(Colors.Black)
            .Thickness(0.3)
            .CornerRadius(12);
    }

    private static Input TabButton(string label, bool selected, Action handler)
    {
        return Button(label, handler)
            .Size(100, 32)
            .Background(selected ? "#1479B8" : "#EEF3F8")
            .FontColor(selected ? "#FFFFFF" : "#334155")
            .Brush(selected ? "#1479B8" : "#CBD7E5")
            .Thickness(1);
    }

    private static IElement DetailLine(string label, string value)
    {
        return Text($"{label}: {value}").FontColor("#3B3B3B");
    }

    private static string LogColor(RuntimeLogKind kind)
    {
        return kind is RuntimeLogKind.UnsupportedEvent or RuntimeLogKind.UnsupportedProperty or RuntimeLogKind.DuplicateKey
            ? "#9A6700"
            : "#3B3B3B";
    }

    private static void ToggleNode(
        string nodeId,
        Action<Func<HashSet<string>, HashSet<string>>> setCollapsedNodes)
    {
        setCollapsedNodes(current =>
        {
            var next = new HashSet<string>(current, StringComparer.Ordinal);
            if (!next.Add(nodeId))
            {
                next.Remove(nodeId);
            }

            return next;
        });
    }

    private static ComponentSnapshot? FindComponent(RuntimeSnapshot snapshot, string? componentId)
    {
        if (string.IsNullOrWhiteSpace(componentId))
        {
            return null;
        }

        foreach (var root in snapshot.Roots)
        {
            var match = FindComponent(root.RootComponent, componentId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static ComponentSnapshot? FindComponent(ComponentSnapshot? component, string componentId)
    {
        if (component is null || string.Equals(component.ComponentId, componentId, StringComparison.Ordinal))
        {
            return component;
        }

        foreach (var child in component.Children)
        {
            var match = FindComponent(child, componentId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static int CountComponents(ComponentSnapshot? component)
    {
        return component is null ? 0 : 1 + component.Children.Sum(CountComponents);
    }

    private enum DevToolsTab
    {
        Inspector,
        Console
    }

    private enum DetailTab
    {
        Details,
        Hooks,
        Stores
    }
}

internal sealed record InspectorTreeRow(
    string NodeId,
    int Depth,
    bool Expanded,
    bool HasChildren,
    string Label,
    Action Toggle,
    Action? Select,
    bool Selected,
    bool IsPlaceholder,
    int Index);
