using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;

namespace Nuri.DevTools;

internal sealed class RuntimeInspectorComponent(
    Action<string?>? highlightRequested = null,
    Func<RuntimeSnapshot>? snapshotProvider = null) : Component
{
    private const int MaximumVisibleLogs = 200;
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
            .Background("#F3F3F3")
            .Padding(14)
            .Spacing(10);
    }

    private static IElement BuildHeader(RuntimeSnapshot snapshot)
    {
        var componentCount = snapshot.Roots.Sum(root => CountComponents(root.RootComponent));
        return Grid(
                Div(
                        Text("Nuri DevTools").FontSize(22).FontColor("#1F1F1F"),
                        Text("Runtime inspector").FontColor("#616161"))
                    .Spacing(2)
                    .Column(0),
                Text($"{snapshot.Roots.Count} roots  |  {componentCount} components  |  {snapshot.RecentLogs.Count} logs")
                    .FontColor("#616161")
                    .End()
                    .Column(1))
            .Columns(Star, Auto)
            .Rows(Auto)
            .Padding(14)
            .Background("#FFFFFF")
            .Brush("#E5E5E5")
            .Thickness(1)
            .CornerRadius(8);
    }

    private static IElement BuildMainTabs(
        DevToolsTab selected,
        Action<Func<DevToolsTab, DevToolsTab>> setSelected)
    {
        return Div(
                DivTypes.Row,
                TabButton("Inspector", selected == DevToolsTab.Inspector, () => setSelected(_ => DevToolsTab.Inspector)),
                TabButton("Console", selected == DevToolsTab.Console, () => setSelected(_ => DevToolsTab.Console)),
                Button("Clear logs", NuriDiagnostics.ClearLogs)
                    .Size(100, 32)
                    .Background("#F7F7F7")
                    .FontColor("#1F1F1F")
                    .Brush("#D8D8D8")
                    .Thickness(1))
            .Spacing(8)
            .Padding(8)
            .Background("#FFFFFF")
            .Brush("#E5E5E5")
            .Thickness(1)
            .CornerRadius(8);
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
                Surface(
                        "Component tree",
                        Div(DivTypes.Scroll, Div(treeRows.ToArray())
                                .Spacing(4))
                )
                    .Column(0),
                Grid(
                        Surface(
                                "Component inspector",
                                BuildDetails(selectedComponent, detailTab, setDetailTab, snapshot))
                            .Row(0),
                        Surface("Runtime events", BuildRuntimeLogs(snapshot))
                            .Row(1))
                    .Rows(Stars(2), Star)
                    .RowSpacing(10)
                    .Column(1))
            .Columns(Pixels(390), Star)
            .Rows(Star)
            .ColumnSpacing(10)
            .Grow();
    }

    private static IReadOnlyList<IElement> BuildTreeRows(
        RuntimeSnapshot snapshot,
        string? selectedComponentId,
        HashSet<string> collapsedNodes,
        Action<Func<HashSet<string>, HashSet<string>>> setCollapsedNodes,
        Action<ComponentSnapshot> selectComponent)
    {
        var rows = new List<IElement>();
        foreach (var root in snapshot.Roots)
        {
            var nodeId = "root:" + root.RootId;
            var expanded = !collapsedNodes.Contains(nodeId);
            rows.Add(BuildTreeRow(
                nodeId,
                0,
                expanded,
                root.RootComponent is not null,
                $"{root.RootId} ({root.Renderer})  patches={root.PatchCount}",
                () => ToggleNode(nodeId, setCollapsedNodes),
                null,
                false));

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
            rows.Add(Text("No diagnostic roots are registered.").FontColor("#616161"));
        }

        return rows;
    }

    private static void AddComponentRows(
        ComponentSnapshot component,
        string? selectedComponentId,
        int depth,
        ICollection<IElement> rows,
        HashSet<string> collapsedNodes,
        Action<Func<HashSet<string>, HashSet<string>>> setCollapsedNodes,
        Action<ComponentSnapshot> selectComponent)
    {
        var nodeId = "component:" + component.ComponentId;
        var expanded = !collapsedNodes.Contains(nodeId);
        rows.Add(BuildTreeRow(
            nodeId,
            depth,
            expanded,
            component.Children.Count > 0,
            $"{component.TypeName}  renders={component.RenderCount}",
            () => ToggleNode(nodeId, setCollapsedNodes),
            () => selectComponent(component),
            string.Equals(component.ComponentId, selectedComponentId, StringComparison.Ordinal)));

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

    private static IElement BuildTreeRow(
        string nodeId,
        int depth,
        bool expanded,
        bool hasChildren,
        string label,
        Action toggle,
        Action? select,
        bool selected)
    {
        var toggleButton = Button(hasChildren ? (expanded ? "-" : "+") : "·", hasChildren ? toggle : () => { })
            .Size(30, 28)
            .Background("#FFFFFF")
            .FontColor("#3B3B3B")
            .Brush("#D8D8D8")
            .Thickness(0)
            .Column(0);
        var selectButton = Button(label, select ?? toggle)
            .Height(28)
            .TextStart()
            .Background(selected ? "#E5F1FB" : depth == 0 ? "#F0F6FA" : "#FFFFFF")
            .FontColor(selected ? "#005A9E" : "#1F1F1F")
            .Brush(selected ? "#99C9EF" : "#E5E5E5")
            .Thickness(0)
            .Column(1);

        return Grid(toggleButton, selectButton)
            .Key(nodeId)
            .Columns(Pixels(32), Star)
            .Rows(Pixels(28))
            .ColumnSpacing(4)
            .Background ("#FFFFFF")
            .Margin(left: depth * 14);
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

    private static IElement BuildRuntimeLogs(RuntimeSnapshot snapshot)
    {
        var logs = snapshot.RecentLogs
            .Where(log => log.Kind != RuntimeLogKind.Console && log.Kind != RuntimeLogKind.AppLog)
            .Reverse()
            .Take(MaximumVisibleLogs)
            .Select(log => (IElement)Text($"{log.LocalTime}  {log.Kind,-20}  {log.Message}").FontColor(LogColor(log.Kind)))
            .ToArray();

        return Div(
            DivTypes.Scroll,
            logs.Length == 0
                ? Text("No runtime events.").FontColor("#616161")
                : Div(logs).Spacing(3));
    }

    private static IElement BuildConsole(RuntimeSnapshot snapshot)
    {
        var logs = snapshot.RecentLogs
            .Where(log => log.Kind is RuntimeLogKind.Console or RuntimeLogKind.AppLog or RuntimeLogKind.FullRebuild)
            .Reverse()
            .Take(MaximumVisibleLogs)
            .Select(log => (IElement)Div(
                    Text($"{log.LocalTime}  {log.Message}").FontColor("#1F1F1F"),
                    Text(log.SourceDisplay).FontColor("#707070"))
                .Spacing(2)
                .Padding(6)
                .Background("#FAFAFA")
                .Brush("#EDEDED")
                .Thickness(1)
                .CornerRadius(4))
            .ToArray();

        return Surface(
                "Console output",
                Div(
                    DivTypes.Scroll,
                    logs.Length == 0
                        ? Text("No console output.").FontColor("#616161")
                        : Div(logs).Spacing(3)))
            .Grow();
    }

    private static Div Surface(string title, IElement content)
    {
        return Div(
                Text(title).FontSize(15).FontColor("#1F1F1F"),
                content)
            .Padding(10)
            .Spacing(8)
            .Background("#FFFFFF")
            .Brush("#E5E5E5")
            .Thickness(1)
            .CornerRadius(8);
    }

    private static Input TabButton(string label, bool selected, Action handler)
    {
        return Button(label, handler)
            .Size(100, 32)
            .Background(selected ? "#0067C0" : "#F7F7F7")
            .FontColor(selected ? "#FFFFFF" : "#1F1F1F")
            .Brush(selected ? "#0067C0" : "#D8D8D8")
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
