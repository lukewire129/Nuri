using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.ExplorerTreeSample.Components;

public sealed class ExplorerTreeComponent : Component
{
    private static readonly ExplorerNode[] Seed =
    {
        Folder("workspace", "Nuri", new[]
        {
            Folder("src", "src", new[]
            {
                Folder("components", "Components", new[]
                {
                    File("explorer-component", "ExplorerTreeComponent.cs"),
                    File("tree-node-component", "TreeNodeComponent.cs")
                }),
                Folder("runtime", "Runtime", new[]
                {
                    File("application-root", "ApplicationRoot.cs"),
                    File("render-coordinator", "RenderCoordinator.cs")
                })
            }),
            Folder("docs", "docs", new[]
            {
                File("runtime-architecture", "RUNTIME_ARCHITECTURE.md"),
                File("runtime-identity", "RUNTIME_IDENTITY.md"),
                File("lifecycle", "LIFECYCLE.md")
            }),
            File("readme", "README.md")
        })
    };

    public override IElement Render()
    {
        var (state, setState) = useState(new ExplorerState(
            Seed,
            "explorer-component",
            "ExplorerTreeComponent.cs",
            new[] { "workspace", "src", "components" },
            1,
            "Select a node or collapse a folder to exercise keyed lifecycle."));
        var stateRef = useLatest(state);

        void Update(Func<ExplorerState, ExplorerState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        void SelectNode(ExplorerNode node)
        {
            Update(current => current with
            {
                SelectedId = node.Id,
                DraftName = node.Name,
                Status = $"Selected {node.Name}"
            });
        }

        void ToggleFolder(ExplorerNode node)
        {
            Update(current =>
            {
                var expanded = current.ExpandedIds.Contains(node.Id, StringComparer.Ordinal);
                return current with
                {
                    ExpandedIds = expanded
                        ? current.ExpandedIds.Where(id => id != node.Id).ToArray()
                        : current.ExpandedIds.Append(node.Id).Distinct(StringComparer.Ordinal).ToArray(),
                    Status = expanded ? $"Collapsed {node.Name}" : $"Expanded {node.Name}"
                };
            });
        }

        void ReportLifecycle(string message)
        {
            Update(current => current with { Status = message });
        }

        var selected = FindNode(state.Nodes, state.SelectedId);
        var tree = state.Nodes
            .Select(node => (IElement)new TreeNodeComponent(
                node,
                depth: 0,
                state.SelectedId,
                state.ExpandedIds,
                SelectNode,
                ToggleFolder,
                ReportLifecycle).Key(node.Id))
            .ToArray();

        return Grid(
                Header().Row(0),
                Grid(
                        TreePanel(tree).Column(0),
                        DetailPanel(
                                state,
                                selected,
                                value => Update(current => current with { DraftName = value }),
                                () => RenameSelected(Update),
                                () => AddChild(Update, isFolder: true),
                                () => AddChild(Update, isFolder: false),
                                () => DeleteSelected(Update))
                            .Column(1))
                    .Columns(390, Star)
                    .Row(1),
                StatusBar(state).Row(2))
            .Rows("Auto,*,Auto")
            .Padding(24)
            .Background("#f3f4f6");
    }

    private static IElement Header()
    {
        return Div(
                Text("Explorer Tree")
                    .FontSize(28)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#111827"),
                Text("Recursive keyed components, subtree updates, selection, rename, add/delete, and effect cleanup.")
                    .FontSize(13)
                    .FontColor("#6b7280")
                    .Margin(top: 6, bottom: 18));
    }

    private static IElement TreePanel(IElement[] tree)
    {
        return Div(
                Text("Workspace")
                    .FontSize(16)
                    .FontWeight(FontWeightValue.Bold)
                    .Margin(bottom: 12),
                Div(DivTypes.Scroll, tree))
            .Padding(16)
            .Margin(right: 16)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }

    private static IElement DetailPanel(
        ExplorerState state,
        ExplorerNode? selected,
        Action<string> updateDraft,
        Action rename,
        Action addFolder,
        Action addFile,
        Action delete)
    {
        if (selected == null)
        {
            return Div(
                    Text("No selection").FontSize(20).FontWeight(FontWeightValue.Bold),
                    Text("Select a node from the tree.").FontColor("#6b7280").Margin(top: 8))
                .Padding(20)
                .Background("#ffffff")
                .Brush("#e5e7eb")
                .Thickness(1)
                .CornerRadius(16);
        }

        var path = FindPath(state.Nodes, selected.Id);
        var actions = new List<IElement>
        {
            Button("Rename", rename)
                .Height(36)
                .Background("#111827")
                .FontColor("#ffffff")
                .Brush("#111827")
                .Thickness(1)
        };

        if (selected.IsFolder)
        {
            actions.Add(Button("Add folder", addFolder).Height(36).Margin(left: 8));
            actions.Add(Button("Add file", addFile).Height(36).Margin(left: 8));
        }

        actions.Add(Button("Delete", delete)
            .Height(36)
            .Margin(left: 8)
            .Background("#fff1f2")
            .FontColor("#be123c")
            .Brush("#fecdd3")
            .Thickness(1));

        return Div(
                Text(selected.IsFolder ? "Folder details" : "File details")
                    .FontSize(13)
                    .FontColor("#6b7280"),
                Text(selected.Name)
                    .FontSize(24)
                    .FontWeight(FontWeightValue.Bold)
                    .Margin(top: 6),
                Text(path)
                    .FontSize(12)
                    .FontColor("#6b7280")
                    .Margin(top: 5, bottom: 20),
                Text("Name").FontSize(13).FontWeight(FontWeightValue.Bold),
                TextBox(state.DraftName, updateDraft)
                    .Key("rename-" + selected.Id)
                    .Height(38)
                    .Padding(10, 6, 10, 6)
                    .Margin(top: 8),
                Div(DivTypes.Row, actions.ToArray()).Margin(top: 16),
                Div(
                        Text("Tree pressure")
                            .FontSize(15)
                            .FontWeight(FontWeightValue.Bold),
                        Text($"{CountNodes(selected)} node(s) in this subtree")
                            .FontColor("#4b5563")
                            .Margin(top: 8),
                        Text("Collapse folders to unmount descendants, then expand them to remount keyed components.")
                            .FontColor("#4b5563")
                            .Margin(top: 6))
                    .Padding(16)
                    .Margin(top: 24)
                    .Background("#f9fafb")
                    .Brush("#e5e7eb")
                    .Thickness(1)
                    .CornerRadius(12))
            .Padding(24)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }

    private static IElement StatusBar(ExplorerState state)
    {
        return Grid(
                Text(state.Status).FontSize(12).FontColor("#4b5563").Column(0),
                Text($"{CountNodes(state.Nodes)} total nodes").FontSize(12).FontColor("#6b7280").End().Column(1))
            .Columns(Star, Auto)
            .Margin(top: 14);
    }

    private static void RenameSelected(Action<Func<ExplorerState, ExplorerState>> update)
    {
        update(current =>
        {
            var name = current.DraftName.Trim();
            if (name.Length == 0)
                return current with { Status = "Name cannot be empty." };

            var selected = FindNode(current.Nodes, current.SelectedId);
            if (selected == null)
                return current with { Status = "Select a node before renaming." };

            return current with
            {
                Nodes = UpdateNode(current.Nodes, selected.Id, node => node with { Name = name }),
                DraftName = name,
                Status = $"Renamed {selected.Name} to {name}"
            };
        });
    }

    private static void AddChild(Action<Func<ExplorerState, ExplorerState>> update, bool isFolder)
    {
        update(current =>
        {
            var selected = FindNode(current.Nodes, current.SelectedId);
            if (selected == null || !selected.IsFolder)
                return current with { Status = "Select a folder before adding a child." };

            var id = $"new-{current.NextId}";
            var name = isFolder ? $"New Folder {current.NextId}" : $"new-file-{current.NextId}.txt";
            var child = new ExplorerNode(id, name, isFolder, Array.Empty<ExplorerNode>());

            return current with
            {
                Nodes = UpdateNode(current.Nodes, selected.Id, node => node with { Children = node.Children.Append(child).ToArray() }),
                SelectedId = id,
                DraftName = name,
                ExpandedIds = current.ExpandedIds.Append(selected.Id).Distinct(StringComparer.Ordinal).ToArray(),
                NextId = current.NextId + 1,
                Status = $"Added {name} to {selected.Name}"
            };
        });
    }

    private static void DeleteSelected(Action<Func<ExplorerState, ExplorerState>> update)
    {
        update(current =>
        {
            var selected = FindNode(current.Nodes, current.SelectedId);
            if (selected == null)
                return current with { Status = "Select a node before deleting." };

            var parent = FindParent(current.Nodes, selected.Id);
            if (parent == null)
                return current with { Status = "The workspace root cannot be deleted." };

            var removedIds = EnumerateIds(selected).ToHashSet(StringComparer.Ordinal);
            return current with
            {
                Nodes = RemoveNode(current.Nodes, selected.Id),
                SelectedId = parent.Id,
                DraftName = parent.Name,
                ExpandedIds = current.ExpandedIds.Where(id => !removedIds.Contains(id)).ToArray(),
                Status = $"Deleted {selected.Name}"
            };
        });
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

    private static ExplorerNode? FindParent(IEnumerable<ExplorerNode> nodes, string childId)
    {
        foreach (var node in nodes)
        {
            if (node.Children.Any(child => child.Id == childId))
                return node;

            var found = FindParent(node.Children, childId);
            if (found != null)
                return found;
        }

        return null;
    }

    private static ExplorerNode[] UpdateNode(
        IEnumerable<ExplorerNode> nodes,
        string id,
        Func<ExplorerNode, ExplorerNode> change)
    {
        return nodes
            .Select(node => node.Id == id
                ? change(node)
                : node with { Children = UpdateNode(node.Children, id, change) })
            .ToArray();
    }

    private static ExplorerNode[] RemoveNode(IEnumerable<ExplorerNode> nodes, string id)
    {
        return nodes
            .Where(node => node.Id != id)
            .Select(node => node with { Children = RemoveNode(node.Children, id) })
            .ToArray();
    }

    private static IEnumerable<string> EnumerateIds(ExplorerNode node)
    {
        yield return node.Id;
        foreach (var child in node.Children)
        {
            foreach (var id in EnumerateIds(child))
                yield return id;
        }
    }

    private static string FindPath(IEnumerable<ExplorerNode> nodes, string id)
    {
        return TryFindPath(nodes, id, Array.Empty<string>(), out var path)
            ? string.Join(" / ", path)
            : id;
    }

    private static bool TryFindPath(
        IEnumerable<ExplorerNode> nodes,
        string id,
        string[] parentPath,
        out string[] path)
    {
        foreach (var node in nodes)
        {
            var currentPath = parentPath.Append(node.Name).ToArray();
            if (node.Id == id)
            {
                path = currentPath;
                return true;
            }

            if (TryFindPath(node.Children, id, currentPath, out path))
                return true;
        }

        path = Array.Empty<string>();
        return false;
    }

    private static int CountNodes(IEnumerable<ExplorerNode> nodes)
    {
        return nodes.Sum(CountNodes);
    }

    private static int CountNodes(ExplorerNode node)
    {
        return 1 + node.Children.Sum(CountNodes);
    }

    private static ExplorerNode Folder(string id, string name, ExplorerNode[] children)
    {
        return new ExplorerNode(id, name, true, children);
    }

    private static ExplorerNode File(string id, string name)
    {
        return new ExplorerNode(id, name, false, Array.Empty<ExplorerNode>());
    }
}

internal sealed record ExplorerNode(string Id, string Name, bool IsFolder, ExplorerNode[] Children);

internal sealed record ExplorerState(
    ExplorerNode[] Nodes,
    string SelectedId,
    string DraftName,
    string[] ExpandedIds,
    int NextId,
    string Status);
