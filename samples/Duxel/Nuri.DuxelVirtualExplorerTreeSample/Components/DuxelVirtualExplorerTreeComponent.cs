using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.DuxelVirtualExplorerTreeSample.Components;

public sealed class DuxelVirtualExplorerTreeComponent : Component
{
    private const int FolderCount = 100;
    private const int FilesPerFolder = 100;
    private const double RowExtent = 36;
    private static readonly ExplorerNode[] Seed = CreateSeed();
    private static readonly HashSet<string> InitiallyExpanded = CreateInitiallyExpanded();
    private static readonly int TotalNodeCount = CountNodes(Seed);

    public override IElement Render()
    {
        var (state, setState) = useState(new ExplorerState(
            Seed,
            new HashSet<string>(InitiallyExpanded, StringComparer.Ordinal),
            "file-0-0",
            "10,101 visible nodes; Duxel projects only viewport rows each frame."));
        var stateRef = useLatest(state);

        void Update(Func<ExplorerState, ExplorerState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        void Select(ExplorerNode node)
        {
            Update(current => current with
            {
                SelectedId = node.Id,
                Status = $"Selected {node.Name}"
            });
        }

        void Toggle(ExplorerNode node)
        {
            Update(current =>
            {
                var expanded = new HashSet<string>(current.ExpandedIds, StringComparer.Ordinal);
                var wasExpanded = !expanded.Add(node.Id);
                if (wasExpanded)
                    expanded.Remove(node.Id);

                return current with
                {
                    ExpandedIds = expanded,
                    Status = wasExpanded ? $"Collapsed {node.Name}" : $"Expanded {node.Name}"
                };
            });
        }

        var visibleRows = useMemo(
            () => FlattenVisibleRows(state.Nodes, state.ExpandedIds),
            state.Nodes,
            state.ExpandedIds);
        var selected = FindNode(state.Nodes, state.SelectedId);

        return Grid(
                Header(visibleRows.Count).Row(0),
                Grid(
                        TreePanel(VirtualizedTree(visibleRows, state.SelectedId, Select, Toggle))
                            .Column(0),
                        DetailPanel(
                                selected,
                                visibleRows.Count,
                                state.Status,
                                () => Update(current => current with
                                {
                                    ExpandedIds = new HashSet<string>(InitiallyExpanded, StringComparer.Ordinal),
                                    Status = "Expanded all 100 folders."
                                }),
                                () => Update(current => current with
                                {
                                    ExpandedIds = new HashSet<string>(new[] { "workspace" }, StringComparer.Ordinal),
                                    Status = "Collapsed all generated folders."
                                }))
                            .Column(1))
                    .Columns(Star, 330)
                    .Row(1),
                StatusBar(state.Status, visibleRows.Count).Row(2))
            .Rows("Auto,*,Auto")
            .Padding(24)
            .Background("#f3f4f6");
    }

    private static IElement Header(int visibleCount)
    {
        return Div(
                Text("Virtual Explorer Tree - Duxel")
                    .FontSize(28)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#111827"),
                Text($"{TotalNodeCount:N0} generated nodes, {visibleCount:N0} currently visible, nominal {RowExtent:0}px rows.")
                    .FontSize(13)
                    .FontColor("#6b7280")
                    .Margin(top: 6, bottom: 18));
    }

    private static IElement VirtualizedTree(
        IReadOnlyList<VisibleExplorerRow> rows,
        string selectedId,
        Action<ExplorerNode> select,
        Action<ExplorerNode> toggle)
    {
        return VirtualizedItems(
            rows,
            row => NodeRow(row, selectedId, select, toggle),
            itemExtent: RowExtent,
            itemKey: row => row.Node.Id);
    }

    private static IElement TreePanel(IElement tree)
    {
        return Grid(
                Text("Generated workspace")
                    .FontSize(16)
                    .FontWeight(FontWeightValue.Bold)
                    .Margin(bottom: 12)
                    .Row(0),
                tree.Row(1))
            .Rows("Auto,*")
            .Padding(16)
            .Margin(right: 16)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }

    private static IElement DetailPanel(
        ExplorerNode? selected,
        int visibleCount,
        string status,
        Action expandAll,
        Action collapseAll)
    {
        return Div(
                Text("Immediate-mode row clipping")
                    .FontSize(18)
                    .FontWeight(FontWeightValue.Bold),
                Text($"{visibleCount:N0} virtual rows")
                    .FontSize(24)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#1d4ed8")
                    .Margin(top: 14),
                Text("The full data set and scroll extent stay intact while Duxel projects only the fixed-extent rows intersecting the viewport, plus the configured item buffer (five before and after by default).")
                    .FontColor("#4b5563")
                    .Margin(top: 8, bottom: 20),
                Button("Expand all", expandAll)
                    .Height(36)
                    .Background("#111827")
                    .FontColor("#ffffff")
                    .Brush("#111827")
                    .Thickness(1),
                Button("Collapse folders", collapseAll)
                    .Height(36)
                    .Margin(top: 8),
                Div(
                        Text("Selected")
                            .FontSize(13)
                            .FontWeight(FontWeightValue.Bold),
                        Text(selected?.Name ?? "No selection")
                            .FontSize(18)
                            .FontWeight(FontWeightValue.Bold)
                            .Margin(top: 8),
                        Text(selected?.Id ?? "-")
                            .FontSize(12)
                            .FontColor("#6b7280")
                            .Margin(top: 4),
                        Text(status)
                            .FontSize(12)
                            .FontColor("#4b5563")
                            .Margin(top: 14))
                    .Padding(16)
                    .Margin(top: 20)
                    .Background("#f9fafb")
                    .Brush("#e5e7eb")
                    .Thickness(1)
                    .CornerRadius(12))
            .Padding(20)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }

    private static IElement StatusBar(string status, int visibleCount)
    {
        return Grid(
                Text(status).FontSize(12).FontColor("#4b5563").Column(0),
                Text($"{visibleCount:N0} visible / {TotalNodeCount:N0} total")
                    .FontSize(12)
                    .FontColor("#6b7280")
                    .End()
                    .Column(1))
            .Columns(Star, Auto)
            .Margin(top: 14);
    }

    private static IElement NodeRow(
        VisibleExplorerRow row,
        string selectedId,
        Action<ExplorerNode> select,
        Action<ExplorerNode> toggle)
    {
        var selected = row.Node.Id == selectedId;
        var toggleText = row.Node.IsFolder ? (row.IsExpanded ? "-" : "+") : " ";
        var kindText = row.Node.IsFolder ? "[D]" : "[F]";

        return Grid(
                Button(toggleText, () =>
                    {
                        if (row.Node.IsFolder)
                            toggle(row.Node);
                        else
                            select(row.Node);
                    })
                    .Height(31)
                    .Background("#f9fafb")
                    .Brush("#e5e7eb")
                    .Thickness(1)
                    .Column(0),
                Button($"{kindText}  {row.Node.Name}", () => select(row.Node))
                    .Height(31)
                    .TextStart()
                    .Padding(10, 4, 10, 4)
                    .Margin(left: 5)
                    .Background(selected ? "#dbeafe" : "#ffffff")
                    .FontColor(selected ? "#1d4ed8" : "#111827")
                    .Brush(selected ? "#93c5fd" : "#e5e7eb")
                    .Thickness(1)
                    .Column(1))
            .Columns(34, Star)
            .Margin(left: row.Depth * 18, bottom: 5)
            .Key(row.Node.Id);
    }

    private static List<VisibleExplorerRow> FlattenVisibleRows(
        ExplorerNode[] nodes,
        HashSet<string> expandedIds)
    {
        var rows = new List<VisibleExplorerRow>(TotalNodeCount);
        AddVisibleRows(nodes, 0, expandedIds, rows);
        return rows;
    }

    private static void AddVisibleRows(
        ExplorerNode[] nodes,
        int depth,
        HashSet<string> expandedIds,
        List<VisibleExplorerRow> rows)
    {
        foreach (var node in nodes)
        {
            var expanded = node.IsFolder && expandedIds.Contains(node.Id);
            rows.Add(new VisibleExplorerRow(node, depth, expanded));
            if (expanded)
                AddVisibleRows(node.Children, depth + 1, expandedIds, rows);
        }
    }

    private static ExplorerNode? FindNode(IEnumerable<ExplorerNode> nodes, string id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id)
                return node;
            var found = FindNode(node.Children, id);
            if (found != null)
                return found;
        }

        return null;
    }

    private static ExplorerNode[] CreateSeed()
    {
        var folders = new ExplorerNode[FolderCount];
        for (var folderIndex = 0; folderIndex < FolderCount; folderIndex++)
        {
            var files = new ExplorerNode[FilesPerFolder];
            for (var fileIndex = 0; fileIndex < FilesPerFolder; fileIndex++)
            {
                files[fileIndex] = new ExplorerNode(
                    $"file-{folderIndex}-{fileIndex}",
                    $"document-{folderIndex:D3}-{fileIndex:D3}.txt",
                    false,
                    Array.Empty<ExplorerNode>());
            }

            folders[folderIndex] = new ExplorerNode(
                $"folder-{folderIndex}",
                $"Generated Folder {folderIndex:D3}",
                true,
                files);
        }

        return new[] { new ExplorerNode("workspace", "Nuri.Generated", true, folders) };
    }

    private static HashSet<string> CreateInitiallyExpanded()
    {
        var expanded = new HashSet<string>(StringComparer.Ordinal) { "workspace" };
        for (var index = 0; index < FolderCount; index++)
            expanded.Add($"folder-{index}");
        return expanded;
    }

    private static int CountNodes(IEnumerable<ExplorerNode> nodes)
    {
        return nodes.Sum(node => 1 + CountNodes(node.Children));
    }
}

internal sealed record ExplorerNode(string Id, string Name, bool IsFolder, ExplorerNode[] Children);

internal sealed record VisibleExplorerRow(ExplorerNode Node, int Depth, bool IsExpanded);

internal sealed record ExplorerState(
    ExplorerNode[] Nodes,
    HashSet<string> ExpandedIds,
    string SelectedId,
    string Status);
