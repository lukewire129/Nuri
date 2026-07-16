using Nuri.UI.Controls;
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
