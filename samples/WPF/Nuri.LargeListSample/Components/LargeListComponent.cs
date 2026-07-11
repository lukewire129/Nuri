using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.LargeListSample.Components;

public sealed class LargeListComponent : Component
{
    private static readonly LargeItem[] Seed = Enumerable.Range(1, 1000)
        .Select(index => new LargeItem($"item-{index:0000}", $"Item {index:0000}", index % 3 == 0))
        .ToArray();

    public override IElement Render()
    {
        var (state, setState) = useState(new LargeListState(Seed, string.Empty, null, "Ready"));
        var stateRef = useLatest(state);

        void Update(Func<LargeListState, LargeListState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(next);
        }

        var filtered = useMemo(() => state.Items
            .Where(item => string.IsNullOrWhiteSpace(state.Filter) || item.Title.Contains(state.Filter, StringComparison.OrdinalIgnoreCase))
            .ToArray(), state.Items, state.Filter);
        var visible = filtered.Take(200).ToArray();

        void Toggle(string id)
        {
            Update(current => current with
            {
                Items = current.Items.Select(item => item.Id == id ? item with { Done = !item.Done } : item).ToArray(),
                Status = "Toggled " + id
            });
        }

        void Remove(string id)
        {
            Update(current => current with
            {
                Items = current.Items.Where(item => item.Id != id).ToArray(),
                SelectedId = current.SelectedId == id ? null : current.SelectedId,
                Status = "Removed " + id
            });
        }

        void MoveFirstToEnd()
        {
            Update(current => current.Items.Length < 2
                ? current
                : current with { Items = current.Items.Skip(1).Append(current.Items[0]).ToArray(), Status = "Moved first item to end" });
        }

        return Grid(Rows(Auto, Auto, Star),
                Div(
                    Text("Virtualized-ish Large List").FontSize(26).FontWeight(FontWeightValue.Bold),
                    Text("1,000개 데이터에서 filter, reorder, edit-ish toggle, remove, keyed row diff 검증").FontColor("#6b7280").Margin(top: 6, bottom: 18)).Row(0),
                Div(
                    Grid(
                            TextBox(state.Filter, value => Update(current => current with { Filter = value })).Key("filter").Height(36).Padding(10, 0, 10, 0).TextStart().TextVCenter().Column(0),
                            Button("Move first to end", MoveFirstToEnd).Height(36).Column(1)
                        )
                        .Columns(Star, Pixels(150)),
                    Text($"Total: {state.Items.Length} / Filtered: {filtered.Length} / Rendered: {visible.Length} / {state.Status}").FontSize(12).FontColor("#6b7280").Margin(top: 10))
                    .Padding(18)
                    .Background("#ffffff")
                    .Brush("#e5e7eb")
                    .Thickness(1)
                    .CornerRadius(16)
                    .Row(1),
                Div(DivTypes.Scroll,
                        visible.Select(item => (IElement)new LargeListRow(item, state.SelectedId == item.Id, id => Update(current => current with { SelectedId = id, Status = "Selected " + id }), Toggle, Remove).Key(item.Id)).ToArray())
                    .Margin(top: 16)
                    .Row(2))
            .Padding(24)
            .Background("#f3f4f6");
    }
}

internal sealed class LargeListRow : Component
{
    private readonly LargeItem _item;
    private readonly bool _selected;
    private readonly Action<string> _select;
    private readonly Action<string> _toggle;
    private readonly Action<string> _remove;

    public LargeListRow(LargeItem item, bool selected, Action<string> select, Action<string> toggle, Action<string> remove)
    {
        _item = item;
        _selected = selected;
        _select = select;
        _toggle = toggle;
        _remove = remove;
    }

    public override IElement Render()
    {
        return Grid(
                Button(_item.Title, () => _select(_item.Id)).Height(34).Background(_selected ? "#dbeafe" : "#ffffff").Brush(_selected ? "#93c5fd" : "#d1d5db").Thickness(1).Column(0),
                Button(_item.Done ? "Done" : "Open", () => _toggle(_item.Id)).Height(34).Column(1),
                Button("Remove", () => _remove(_item.Id)).Height(34).Background("#fff1f2").FontColor("#be123c").Brush("#fecdd3").Thickness(1).Column(2))
            .Columns(Star, Pixels(90), Pixels(90))
            .Padding(10)
            .Margin(bottom: 8, right: 8)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(12);
    }
}

internal sealed record LargeItem(string Id, string Title, bool Done);
internal sealed record LargeListState(LargeItem[] Items, string Filter, string? SelectedId, string Status);
