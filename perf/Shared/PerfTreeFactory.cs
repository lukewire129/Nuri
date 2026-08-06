using Nuri.UI.Controls;
using Nuri.Constants;
using Nuri.VirtualDom;

internal static class PerfTreeFactory
{
    public static (VirtualEntry Old, VirtualEntry New) CreateReorderedTree(int size)
    {
        var oldChildren = new List<VirtualEntry>(size);
        var newChildren = new List<VirtualEntry>(size);

        for (var i = 0; i < size; i++)
            oldChildren.Add(CreateItem(i, "value"));

        for (var i = 1; i < size; i++)
            newChildren.Add(CreateItem(i, "value"));

        newChildren.Add(CreateItem(0, "value"));
        return (CreateRoot(oldChildren), CreateRoot(newChildren));
    }

    public static (VirtualEntry Old, VirtualEntry New) CreateTodoTree(int size, bool reorder)
    {
        var oldItems = Enumerable.Range(0, size).Select(CreateTodoItem).ToArray();
        var newItems = reorder
            ? Enumerable.Range(1, Math.Max(0, size - 1)).Append(0).Select(CreateTodoItem).ToArray()
            : oldItems;

        return (CreateTodoRoot(oldItems), CreateTodoRoot(newItems));
    }

    public static (VirtualEntry Old, VirtualEntry New) CreateEditorTree(int size, bool editLine)
    {
        var oldLines = Enumerable.Range(0, size).Select(index => CreateEditorLine(index, false)).ToArray();
        var newLines = Enumerable.Range(0, size).Select(index => CreateEditorLine(index, editLine && index == size / 2)).ToArray();
        return (CreateEditorRoot(oldLines, size), CreateEditorRoot(newLines, size));
    }

    public static (VirtualEntry Old, VirtualEntry New) CreateAbsoluteTree(int size)
    {
        var oldChildren = Enumerable.Range(0, size)
            .Select(index => CreateAbsoluteItem(index, index == size / 2 ? 24 : 0, index == size / 2 ? 12 : 0))
            .ToArray();
        var newChildren = Enumerable.Range(0, size)
            .Select(index => CreateAbsoluteItem(index, index == size / 2 ? 48 : 0, index == size / 2 ? 24 : 0))
            .ToArray();

        return (
            new VirtualEntry(VirtualControlTypes.Div, kind: DivTypes.Absolute, children: oldChildren).WithIdentity("absolute-root", null),
            new VirtualEntry(VirtualControlTypes.Div, kind: DivTypes.Absolute, children: newChildren).WithIdentity("absolute-root", null));
    }

    private static VirtualEntry CreateEditorRoot(IEnumerable<VirtualEntry> lines, int documentSize)
    {
        var viewport = new VirtualEntry(VirtualControlTypes.Div, kind: DivTypes.Column, children: lines)
            .WithIdentity("editor-viewport", null);
        return new VirtualEntry(VirtualControlTypes.Div, kind: DivTypes.Column, children: new[]
        {
            new VirtualEntry(VirtualControlTypes.Text, properties: new[] { KeyValuePair.Create<string, object?>("Text", "Nuri Editor Stress") }),
            new VirtualEntry(VirtualControlTypes.Text, properties: new[] { KeyValuePair.Create<string, object?>("Text", $"{documentSize:N0} visible lines") }),
            viewport,
            new VirtualEntry(VirtualControlTypes.Text, properties: new[] { KeyValuePair.Create<string, object?>("Text", "Ln 1, Col 1 | UTF-8 | Ready") })
        }).WithIdentity("editor-root", null);
    }

    private static VirtualEntry CreateEditorLine(int index, bool edited)
    {
        return new VirtualEntry(VirtualControlTypes.Text, key: $"line-{index}", properties: new[]
        {
            KeyValuePair.Create<string, object?>("Text", edited ? $"{index + 1,5}  // edited line {index}" : $"{index + 1,5}  let value_{index} = compute({index});"),
            KeyValuePair.Create<string, object?>("Tag", index)
        });
    }

    private static VirtualEntry CreateAbsoluteItem(int index, double xOffset, double yOffset)
    {
        return new VirtualEntry(VirtualControlTypes.Text, key: $"absolute-{index}", properties: new[]
        {
            KeyValuePair.Create<string, object?>("Text", $"Node {index}"),
            KeyValuePair.Create<string, object?>(PropertyKeys.PositionX, (index % 50) * 24d + xOffset),
            KeyValuePair.Create<string, object?>(PropertyKeys.PositionY, (index / 50) * 24d + yOffset)
        });
    }

    private static VirtualEntry CreateTodoRoot(IEnumerable<VirtualEntry> items)
    {
        var list = new VirtualEntry(
            VirtualControlTypes.Div,
            kind: DivTypes.Column,
            children: items).WithIdentity("0_2", null);

        return new VirtualEntry(
            VirtualControlTypes.Div,
            kind: DivTypes.Column,
            children: new[]
            {
                new VirtualEntry(
                    VirtualControlTypes.Text,
                    properties: new[]
                    {
                        KeyValuePair.Create<string, object?>("Text", "할 일 검증 샘플")
                    }),
                new VirtualEntry(
                    VirtualControlTypes.Text,
                    properties: new[]
                    {
                        KeyValuePair.Create<string, object?>("Text", "controlled input, list diff, filter")
                    }),
                list
            }).WithIdentity("0", null);
    }

    private static VirtualEntry CreateTodoItem(int index)
    {
        return new VirtualEntry(
            VirtualControlTypes.Text,
            key: $"todo-{index}",
            properties: new[]
            {
                KeyValuePair.Create<string, object?>("Text", $"Todo item {index}"),
                KeyValuePair.Create<string, object?>("Tag", index)
            });
    }

    private static VirtualEntry CreateRoot(IEnumerable<VirtualEntry> children)
    {
        return new VirtualEntry(
            VirtualControlTypes.Div,
            kind: DivTypes.Column,
            children: children).WithIdentity("0", null);
    }

    private static VirtualEntry CreateItem(int index, string value)
    {
        return new VirtualEntry(
            VirtualControlTypes.Text,
            key: $"item-{index}",
            properties: new[]
            {
                KeyValuePair.Create<string, object?>("Text", value),
                KeyValuePair.Create<string, object?>("Tag", index)
            });
    }
}
