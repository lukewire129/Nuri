using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.MasterDetailSample.Components;

public sealed class MasterDetailComponent : Component
{
    private static readonly Item[] Seed =
    {
        new("alpha", "Alpha", "First editable detail item."),
        new("bravo", "Bravo", "Delete or edit this item."),
        new("charlie", "Charlie", "Selection should survive edits.")
    };

    public override IElement Render()
    {
        var (state, setState) = useState(new MasterDetailState(Seed, "alpha", ""));
        var stateRef = useLatest(state);

        void Update(Func<MasterDetailState, MasterDetailState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        var selected = state.Items.FirstOrDefault(item => item.Id == state.SelectedId);

        return Grid(Rows(Auto, Star),
                Div(Text("Master / Detail").FontSize(26).FontWeight(FontWeightValue.Bold), Text("list selection, detail edit, delete selected, empty state 검증").FontColor("#6b7280").Margin(top: 6, bottom: 18)).Row(0),
                Grid(
                        Div(DivTypes.Scroll, state.Items.Select(item => (IElement)Row(item, item.Id == state.SelectedId, () => Update(current => current with { SelectedId = item.Id }))).ToArray())
                            .Padding(12)
                            .Background("#ffffff")
                            .Brush("#e5e7eb")
                            .Thickness(1)
                            .CornerRadius(16)
                            .Column(0),
                        Detail(selected, text => Update(current => current with
                            {
                                Items = current.Items.Select(item => item.Id == current.SelectedId ? item with { Notes = text } : item).ToArray()
                            }),
                            () => Update(current =>
                            {
                                var remaining = current.Items.Where(item => item.Id != current.SelectedId).ToArray();
                                return current with { Items = remaining, SelectedId = remaining.FirstOrDefault()?.Id };
                            }))
                            .Column(1)
                    )
                    .Columns(Pixels(280), Star)
                    .Row(1))
            .Padding(24)
            .Background("#f3f4f6");
    }

    private static IElement Row(Item item, bool selected, Action onClick)
    {
        return Button(item.Title, onClick)
            .Key(item.Id)
            .Height(40)
            .Margin(bottom: 8)
            .Background(selected ? "#dbeafe" : "#ffffff")
            .Brush(selected ? "#93c5fd" : "#d1d5db")
            .Thickness(1);
    }

    private static IElement Detail(Item? item, Action<string> updateNotes, Action delete)
    {
        if (item == null)
            return Div(Text("No selection").FontSize(20).FontWeight(FontWeightValue.Bold), Text("Select or keep an item to show details.").FontColor("#6b7280").Margin(top: 8)).Padding(20).Column(1);

        return Div(
                Text(item.Title).FontSize(22).FontWeight(FontWeightValue.Bold),
                Text(item.Id).FontSize(12).FontColor("#6b7280").Margin(top: 4, bottom: 14),
                TextBox(item.Notes, updateNotes).Key("detail-" + item.Id).Height(80).Padding(10, 8, 10, 8).TextStart(),
                Button("Delete selected", delete).Height(36).Margin(top: 16).Background("#fff1f2").FontColor("#be123c").Brush("#fecdd3").Thickness(1))
            .Padding(20)
            .Margin(left: 16)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }
}

internal sealed record Item(string Id, string Title, string Notes);
internal sealed record MasterDetailState(Item[] Items, string? SelectedId, string Status);
