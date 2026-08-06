using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Events;
using Nuri.UI.Values;

namespace Nuri.WorkflowSample.Components;

public sealed class WorkflowComponent : Component
{
    private const double NodeWidth = 150;
    private const double NodeHeight = 62;
    private const double SurfaceWidth = 860;
    private const double SurfaceHeight = 430;

    private static readonly WorkflowNode[] InitialNodes =
    {
        new("trigger", "Trigger", 80, 90, "Starts the workflow"),
        new("prepare", "Prepare", 350, 90, "Prepare the input"),
        new("publish", "Publish", 620, 90, "Publish the result")
    };

    public override IElement Render()
    {
        var (selectedId, setSelectedId) = useState<string?>("prepare");
        var (nodes, setNodes) = useState(InitialNodes);
        var dragRef = useRef<DragState?>(null);
        var nodesRef = useLatest(nodes);
        var dragHandlers = useMemo(() => InitialNodes.ToDictionary(
            node => node.Id,
            node => new DragHandlers(
                pointer =>
                {
                    var currentNode = nodesRef.Current.First(current => current.Id == node.Id);
                    dragRef.Current = new DragState(node.Id, pointer.X, pointer.Y, currentNode.X, currentNode.Y);
                    setSelectedId(_ => node.Id);
                },
                pointer =>
                {
                    if (!pointer.IsPrimaryButtonPressed)
                    {
                        dragRef.Current = null;
                        return;
                    }

                    if (dragRef.Current is not DragState drag || drag.Id != node.Id)
                        return;

                    var deltaX = pointer.X - drag.StartX;
                    var deltaY = pointer.Y - drag.StartY;
                    var x = Math.Clamp(drag.OriginX + deltaX, 0, SurfaceWidth - NodeWidth);
                    var y = Math.Clamp(drag.OriginY + deltaY, 0, SurfaceHeight - NodeHeight);
                    setNodes(current => current
                        .Select(currentNode => currentNode.Id == drag.Id
                            ? currentNode with { X = x, Y = y }
                            : currentNode)
                        .ToArray());
                },
                _ => dragRef.Current = null)));

        var selected = nodes.FirstOrDefault(node => node.Id == selectedId);
        var nodesById = nodes.ToDictionary(node => node.Id);
        var surfaceChildren = Connection(nodesById["trigger"], nodesById["prepare"])
        .Concat(Connection(nodesById["prepare"], nodesById["publish"]))
        .Concat(nodes.Select(node => NodeView(
            node,
            node.Id == selectedId,
            dragHandlers[node.Id])))
        .ToArray();
        var surface = Component.Absolute(surfaceChildren)
            .Size(SurfaceWidth, SurfaceHeight)
            .Background("#f8fafc")
            .Brush("#cbd5e1")
            .Thickness(1);

        return
            Component.Grid(
                Component.Div(
                    Component.Text("Workflow Editor")
                        .FontSize(26)
                        .FontWeight(FontWeightValue.Bold),
                    Component.Text("Drag nodes to update Position without rebuilding their native identity.")
                        .FontColor("#64748b")
                        .Margin(top: 6)
                )
                .Row(0),
                Component.Grid(
                    Component.Div(
                        DivTypes.Scroll,
                        surface
                    )
                    .Column(0),
                    Inspector(selected, nodes, setNodes)
                        .Column(1)
                )
                .Columns(Star, Pixels(240))
                .Row(1)
            )
            .Rows(Auto, Star)
            .Padding(24)
            .Background("#eef2ff");
    }

    private static IElement NodeView(
        WorkflowNode node,
        bool selected,
        DragHandlers handlers)
    {
        return Component.Button(node.Title)
            .Key(node.Id)
            .Position(node.X, node.Y)
            .Size(NodeWidth, NodeHeight)
            .Background(selected ? "#dbeafe" : "#ffffff")
            .Brush(selected ? "#2563eb" : "#cbd5e1")
            .Thickness(1)
            .FontWeight(FontWeightValue.Bold)
            .OnPointerDown(handlers.PointerDown, EventRouting.Tunnel, capturePointer: true)
            .OnPointerMove(handlers.PointerMove, EventRouting.Tunnel)
            .OnPointerUp(handlers.PointerUp, EventRouting.Tunnel, releasePointerCapture: true);
    }

    private static IEnumerable<IElement> Connection(WorkflowNode from, WorkflowNode to)
    {
        var startX = from.X + NodeWidth;
        var startY = from.Y + (NodeHeight / 2);
        var endX = to.X;
        var endY = to.Y + (NodeHeight / 2);
        var middleX = (startX + endX) / 2;

        return new[]
        {
            ConnectionSegment($"{from.Id}-{to.Id}-start", startX, startY, middleX, startY),
            ConnectionSegment($"{from.Id}-{to.Id}-middle", middleX, startY, middleX, endY),
            ConnectionSegment($"{from.Id}-{to.Id}-end", middleX, endY, endX, endY)
        };
    }

    private static IElement ConnectionSegment(string key, double startX, double startY, double endX, double endY)
    {
        var isVertical = startX == endX;
        var x = Math.Min(startX, endX);
        var y = Math.Min(startY, endY);
        var width = isVertical ? 2 : Math.Max(2, Math.Abs(endX - startX));
        var height = isVertical ? Math.Max(2, Math.Abs(endY - startY)) : 2;

        return Component.Div()
            .Key(key)
            .Position(x, y)
            .Size(width, height)
            .Background("#94a3b8");
    }

    private static IElement Inspector(
        WorkflowNode? selected,
        WorkflowNode[] nodes,
        Action<Func<WorkflowNode[], WorkflowNode[]>> update)
    {
        if (selected == null)
            return Component.Div(Component.Text("Select a node")).Padding(18).Background("#ffffff");

        void Move(double x, double y)
        {
            update(current => current
                .Select(node => node.Id == selected.Id
                    ? node with { X = x, Y = y }
                    : node)
                .ToArray());
        }

        return Component.Div(
                Component.Text("Selected node").FontWeight(FontWeightValue.Bold),
                Component.Text(selected.Title)
                .FontSize(20).Margin(top: 8),
                Component.Text(selected.Description).FontColor("#64748b").Margin(top: 6, bottom: 18),
                Component.Text($"Position: {selected.X:0}, {selected.Y:0}").FontColor("#475569"),
                Component.Button("Move right", () => Move(selected.X + 20, selected.Y)).Margin(top: 16),
                Component.Button("Reset position", () =>
                {
                    var original = InitialNodes.First(node => node.Id == selected.Id);
                    Move(original.X, original.Y);
                }).Margin(top: 8))
            .Padding(18)
            .Margin(left: 16)
            .Background("#ffffff")
            .Brush("#cbd5e1")
            .Thickness(1)
            .CornerRadius(12);
    }
}

internal sealed record WorkflowNode(string Id, string Title, double X, double Y, string Description);

internal sealed record DragState(string Id, double StartX, double StartY, double OriginX, double OriginY);

internal sealed record DragHandlers(
    Action<PointerEvent> PointerDown,
    Action<PointerEvent> PointerMove,
    Action<PointerEvent> PointerUp);
