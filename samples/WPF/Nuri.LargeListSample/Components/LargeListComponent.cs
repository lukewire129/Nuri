using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.LargeListSample.Components;

public sealed class LargeListComponent : Component
{
    private const int ItemCount = 10_000;
    private const double RowExtent = 42;
    private static readonly LargeItem[] Seed = CreateItems("item", 0);

    public override IElement Render()
    {
        var renderCount = useRef(0);
        renderCount.Current++;

        var (state, setState) = useState(new LargeListState(Seed, string.Empty, null, 1, "Ready"));
        var stateRef = useLatest(state);

        void Update(Func<LargeListState, LargeListState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        var filtered = useMemo(
            () => state.Items
                .Where(item => string.IsNullOrWhiteSpace(state.Filter)
                    || item.Title.Contains(state.Filter, StringComparison.OrdinalIgnoreCase))
                .ToArray(),
            state.Items,
            state.Filter);

        void ToggleFirst()
        {
            Update(current => current.Items.Length == 0
                ? current with { Status = "Nothing to update" }
                : current with
                {
                    Items = current.Items
                        .Select((item, index) => index == 0 ? item with { Done = !item.Done } : item)
                        .ToArray(),
                    Status = "Updated one retained key"
                });
        }

        void SwapFirstTwo()
        {
            Update(current =>
            {
                if (current.Items.Length < 2)
                    return current with { Status = "Need at least two rows" };

                var items = current.Items.ToArray();
                (items[0], items[1]) = (items[1], items[0]);
                return current with { Items = items, Status = "Swapped the first two keys" };
            });
        }

        void ReverseAll()
        {
            Update(current => current with
            {
                Items = current.Items.Reverse().ToArray(),
                Status = $"Reversed {current.Items.Length:N0} keys"
            });
        }

        void RemoveFirst()
        {
            Update(current => current.Items.Length == 0
                ? current with { Status = "Nothing to remove" }
                : current with
                {
                    Items = current.Items.Skip(1).ToArray(),
                    Status = "Removed the first key"
                });
        }

        void AddFirst()
        {
            Update(current =>
            {
                var id = $"added-{current.NextId:00000}";
                return current with
                {
                    Items = new[] { new LargeItem(id, $"Added {current.NextId:00000}", false) }
                        .Concat(current.Items)
                        .ToArray(),
                    NextId = current.NextId + 1,
                    Status = "Added one new key"
                };
            });
        }

        void ReplaceAll()
        {
            Update(current => current with
            {
                Items = CreateItems("replacement", current.NextId),
                SelectedId = null,
                NextId = current.NextId + 1,
                Status = $"Replaced all {ItemCount:N0} keys"
            });
        }

        void Reset()
        {
            Update(current => current with
            {
                Items = Seed,
                Filter = string.Empty,
                SelectedId = null,
                Status = "Reset the source"
            });
        }

        void Select(string id)
        {
            Update(current => current with { SelectedId = id, Status = "Selected " + id });
        }

        var diagnostics = NuriDiagnostics.GetSnapshot();
        var rootMetrics = diagnostics.Roots.FirstOrDefault();
        var virtualizationMetrics = diagnostics.VirtualizedItems
            .OrderByDescending(item => item.ItemCount)
            .FirstOrDefault();
        var patchTypes = rootMetrics == null || rootMetrics.LastPatchCounts.Count == 0
            ? "none"
            : string.Join(", ", rootMetrics.LastPatchCounts
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}={pair.Value}"));

        return Grid(
                Header(renderCount.Current).Row(0),
                StressControls(state, filtered.Length, Update, ToggleFirst, SwapFirstTwo, ReverseAll, RemoveFirst, AddFirst, ReplaceAll, Reset).Row(1),
                MetricsPanel(rootMetrics, virtualizationMetrics, patchTypes).Row(2),
                VirtualizedItems(
                        filtered,
                        item => item.Id,
                        RowExtent,
                        item => Row(item, state.SelectedId == item.Id, Select))
                    .Margin(top: 14)
                    .Row(3),
                Text(state.Status)
                    .FontSize(12)
                    .FontColor("#4b5563")
                    .Margin(top: 10)
                    .Row(4))
            .Rows("Auto,Auto,Auto,*,Auto")
            .Padding(24)
            .Background("#f3f4f6");
    }

    private static IElement Header(int renderCount)
    {
        return Div(
            Text("WPF Virtualized List Stress")
                .FontSize(26)
                .FontWeight(FontWeightValue.Bold),
            Text($"{ItemCount:N0} rows, fixed {RowExtent:0}px extent, component render #{renderCount:N0}")
                .FontColor("#6b7280")
                .Margin(top: 6, bottom: 16));
    }

    private static IElement StressControls(
        LargeListState state,
        int filteredCount,
        Action<Func<LargeListState, LargeListState>> update,
        Action toggleFirst,
        Action swapFirstTwo,
        Action reverseAll,
        Action removeFirst,
        Action addFirst,
        Action replaceAll,
        Action reset)
    {
        return Div(
                Grid(
                        TextBox(state.Filter, value => update(current => current with { Filter = value, Status = "Filtered rows" }))
                            .Key("filter")
                            .Height(36)
                            .Padding(10, 0, 10, 0)
                            .TextStart()
                            .TextVCenter()
                            .Column(0),
                        Button("Update one", toggleFirst).Height(36).Column(1),
                        Button("Swap two", swapFirstTwo).Height(36).Column(2),
                        Button("Reverse all", reverseAll).Height(36).Column(3))
                    .Columns(Star, 110, 100, 110),
                Grid(
                        Button("Remove first", removeFirst).Height(36).Column(0),
                        Button("Add first", addFirst).Height(36).Column(1),
                        Button("Replace all", replaceAll).Height(36).Column(2),
                        Button("Reset", reset).Height(36).Column(3),
                        Text($"Source {state.Items.Length:N0} / filtered {filteredCount:N0}")
                            .FontSize(12)
                            .FontColor("#6b7280")
                            .End()
                            .VCenter()
                            .Column(4))
                    .Columns(110, 100, 110, 90, Star)
                    .Margin(top: 8))
            .Padding(14)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(14);
    }

    private static IElement MetricsPanel(
        ApplicationRootSnapshot? root,
        VirtualizedItemsSnapshot? virtualization,
        string patchTypes)
    {
        return Grid(
                Metric("Previous patch batch", (root?.LastPatchCount ?? 0).ToString("N0"), patchTypes).Column(0),
                Metric("Total patches", (root?.PatchCount ?? 0).ToString("N0"), $"{root?.PatchBatchCount ?? 0:N0} batches").Column(1),
                Metric("Native rows", (virtualization?.RealizedCount ?? 0).ToString("N0"), $"{virtualization?.ItemCount ?? 0:N0} virtual items").Column(2))
            .Columns(Star, Star, Star)
            .Margin(top: 10);
    }

    private static IElement Metric(string title, string value, string detail)
    {
        return Div(
                Text(title).FontSize(11).FontColor("#6b7280"),
                Text(value).FontSize(22).FontWeight(FontWeightValue.Bold).Margin(top: 4),
                Text(detail).FontSize(10).FontColor("#4b5563").Margin(top: 3))
            .Padding(12)
            .Margin(right: 8)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(10);
    }

    private static IElement Row(LargeItem item, bool selected, Action<string> select)
    {
        return Grid(
                Button(item.Title, () => select(item.Id))
                    .Height(34)
                    .TextStart()
                    .Padding(10, 4, 10, 4)
                    .Background(selected ? "#dbeafe" : "#ffffff")
                    .FontColor(selected ? "#1d4ed8" : "#111827")
                    .Brush(selected ? "#93c5fd" : "#e5e7eb")
                    .Thickness(1)
                    .Column(0),
                Text(item.Done ? "Done" : "Open")
                    .FontSize(12)
                    .FontColor(item.Done ? "#15803d" : "#6b7280")
                    .End()
                    .VCenter()
                    .Column(1))
            .Columns(Star, 70)
            .Margin(bottom: 4, right: 8);
    }

    private static LargeItem[] CreateItems(string prefix, int generation)
    {
        return Enumerable.Range(1, ItemCount)
            .Select(index => new LargeItem(
                $"{prefix}-{generation:000}-{index:00000}",
                $"{prefix} {generation:000}/{index:00000}",
                index % 3 == 0))
            .ToArray();
    }
}

internal sealed record LargeItem(string Id, string Title, bool Done);

internal sealed record LargeListState(
    LargeItem[] Items,
    string Filter,
    string? SelectedId,
    int NextId,
    string Status);
