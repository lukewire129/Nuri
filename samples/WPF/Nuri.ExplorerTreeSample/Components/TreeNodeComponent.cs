using Nuri.UI.Controls;
using Nuri.UI.Dsl;

namespace Nuri.ExplorerTreeSample.Components;

internal sealed class TreeNodeComponent : Component
{
    private readonly ExplorerNode _node;
    private readonly int _depth;
    private readonly string _selectedId;
    private readonly string[] _expandedIds;
    private readonly Action<ExplorerNode> _select;
    private readonly Action<ExplorerNode> _toggle;
    private readonly Action<string> _reportLifecycle;

    public TreeNodeComponent(
        ExplorerNode node,
        int depth,
        string selectedId,
        string[] expandedIds,
        Action<ExplorerNode> select,
        Action<ExplorerNode> toggle,
        Action<string> reportLifecycle)
    {
        _node = node;
        _depth = depth;
        _selectedId = selectedId;
        _expandedIds = expandedIds;
        _select = select;
        _toggle = toggle;
        _reportLifecycle = reportLifecycle;
    }

    public override IElement Render()
    {
        var expanded = _node.IsFolder && _expandedIds.Contains(_node.Id, StringComparer.Ordinal);
        var selected = _selectedId == _node.Id;
        var nodeName = _node.Name;

        useEffect(() =>
        {
            _reportLifecycle($"Mounted {nodeName}");
            return () => _reportLifecycle($"Unmounted {nodeName}");
        }, []);

        var elements = new List<IElement>
        {
            NodeRow(expanded, selected)
        };

        if (expanded)
        {
            elements.AddRange(_node.Children.Select(child =>
                (IElement)new TreeNodeComponent(
                    child,
                    _depth + 1,
                    _selectedId,
                    _expandedIds,
                    _select,
                    _toggle,
                    _reportLifecycle).Key(child.Id)));
        }

        return Div(elements.ToArray());
    }

    private IElement NodeRow(bool expanded, bool selected)
    {
        var toggleText = _node.IsFolder ? (expanded ? "-" : "+") : " ";
        var kindText = _node.IsFolder ? "[D]" : "[F]";

        return Grid(
                Button(toggleText, () =>
                    {
                        if (_node.IsFolder)
                            _toggle(_node);
                        else
                            _select(_node);
                    })
                    .Height(32)
                    .Background("#f9fafb")
                    .Brush("#e5e7eb")
                    .Thickness(1)
                    .Column(0),
                Button($"{kindText}  {_node.Name}", () => _select(_node))
                    .Height(32)
                    .TextStart()
                    .Padding(10, 4, 10, 4)
                    .Margin(left: 5)
                    .Background(selected ? "#dbeafe" : "#ffffff")
                    .FontColor(selected ? "#1d4ed8" : "#111827")
                    .Brush(selected ? "#93c5fd" : "#e5e7eb")
                    .Thickness(1)
                    .Column(1))
            .Columns(34, Star)
            .Margin(left: _depth * 18, bottom: 5);
    }
}
