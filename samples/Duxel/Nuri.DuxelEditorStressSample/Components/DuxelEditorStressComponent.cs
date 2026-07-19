using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.DuxelEditorStressSample.Components;

public sealed class DuxelEditorStressComponent : Component
{
    private const int LineCount = 100_000;
    private const double RowExtent = 24;
    private static readonly EditorLine[] Seed = CreateSeed();

    public override IElement Render()
    {
        var (state, setState) = useState(new EditorState(Seed, false, 0, "Ready"));
        var stateRef = useLatest(state);

        void Update(Func<EditorState, EditorState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        void EditMiddle()
        {
            Update(current =>
            {
                var lines = (EditorLine[])current.Lines.Clone();
                var index = lines.Length / 2;
                var version = current.EditVersion + 1;
                lines[index] = lines[index] with
                {
                    Text = $"var value_{index} = compute({index}); // edit {version}"
                };
                return current with
                {
                    Lines = lines,
                    EditVersion = version,
                    Status = $"Edited line {index + 1:N0}"
                };
            });
        }

        void SwapMiddle()
        {
            Update(current =>
            {
                var lines = (EditorLine[])current.Lines.Clone();
                var index = lines.Length / 2;
                (lines[index - 1], lines[index]) = (lines[index], lines[index - 1]);
                return current with
                {
                    Lines = lines,
                    Status = $"Swapped lines {index:N0} and {index + 1:N0}"
                };
            });
        }

        var visibleLines = useMemo(
            () => state.Filtered
                ? state.Lines.Where((_, index) => index % 100 == 0).ToArray()
                : state.Lines,
            state.Lines,
            state.Filtered);
        var diagnostics = NuriDiagnostics.GetSnapshot();
        var root = diagnostics.Roots.FirstOrDefault();
        var virtualized = diagnostics.VirtualizedItems
            .OrderByDescending(item => item.ItemCount)
            .FirstOrDefault();

        return Grid(
                Header(
                        state,
                        visibleLines.Length,
                        EditMiddle,
                        SwapMiddle,
                        () => Update(current => current with
                        {
                            Filtered = !current.Filtered,
                            Status = current.Filtered ? "Showing all lines" : "Showing every 100th line"
                        }))
                    .Row(0),
                Workspace(visibleLines).Row(1),
                StatusBar(state.Status, root, virtualized).Row(2))
            .Rows("Auto,*,Auto")
            .Padding(16)
            .Background("#111827");
    }

    private static IElement Header(
        EditorState state,
        int visibleCount,
        Action editMiddle,
        Action swapMiddle,
        Action toggleFilter)
    {
        return Grid(
                Div(
                        Text("Nuri Editor Stress")
                            .FontSize(22)
                            .FontWeight(FontWeightValue.Bold)
                            .FontColor("#f9fafb"),
                        Text($"{visibleCount:N0} visible / {LineCount:N0} document lines")
                            .FontSize(12)
                            .FontColor("#9ca3af")
                            .Margin(top: 4))
                    .Column(0),
                Grid(
                        Button("Edit middle", editMiddle).Height(34).Column(0),
                        Button("Swap lines", swapMiddle).Height(34).Margin(left: 8).Column(1),
                        Button(state.Filtered ? "Show all" : "Filter 1/100", toggleFilter)
                            .Height(34)
                            .Margin(left: 8)
                            .Column(2))
                    .Columns(110, 110, 120)
                    .Column(1))
            .Columns(Star, Auto)
            .Margin(bottom: 14);
    }

    private static IElement Workspace(EditorLine[] lines)
    {
        return Grid(
                Sidebar().Column(0),
                Editor(lines).Column(1))
            .Columns(220, Star);
    }

    private static IElement Sidebar()
    {
        return Div(
                Text("EXPLORER").FontSize(11).FontColor("#9ca3af"),
                Text("Nuri").FontSize(13).FontColor("#e5e7eb").Margin(top: 14),
                Text("  src").FontSize(13).FontColor("#d1d5db").Margin(top: 8),
                Text("    Runtime").FontSize(13).FontColor("#d1d5db").Margin(top: 6),
                Text("      VirtualTreeDiff.cs").FontSize(13).FontColor("#93c5fd").Margin(top: 6),
                Text("  samples").FontSize(13).FontColor("#d1d5db").Margin(top: 6))
            .Padding(14)
            .Margin(right: 1)
            .Background("#1f2937");
    }

    private static IElement Editor(EditorLine[] lines)
    {
        return Grid(
                Text("VirtualTreeDiff.cs")
                    .FontSize(13)
                    .FontColor("#e5e7eb")
                    .Padding(14, 9, 14, 9)
                    .Background("#1f2937")
                    .Row(0),
                VirtualizedItems(
                        lines,
                        LineRow,
                        itemExtent: RowExtent,
                        itemKey: line => line.Id)
                    .Background("#0f172a")
                    .Row(1))
            .Rows("Auto,*");
    }

    private static IElement LineRow(EditorLine line)
    {
        return Grid(
                Text((line.Number + 1).ToString())
                    .FontSize(12)
                    .FontColor("#64748b")
                    .End()
                    .Margin(right: 14)
                    .Column(0),
                Text(line.Text)
                    .FontSize(12)
                    .FontColor("#cbd5e1")
                    .Column(1))
            .Columns(70, Star)
            .Height(RowExtent)
            .Padding(8, 3, 8, 3)
            .Key(line.Id);
    }

    private static IElement StatusBar(
        string status,
        ApplicationRootSnapshot? root,
        VirtualizedItemsSnapshot? virtualized)
    {
        return Grid(
                Text(status).FontSize(11).FontColor("#dbeafe").Column(0),
                Text($"last patches {root?.LastPatchCount ?? 0:N0} | total {root?.PatchCount ?? 0:N0} | projected {virtualized?.RealizedCount ?? 0:N0}/{virtualized?.ItemCount ?? 0:N0}")
                    .FontSize(11)
                    .FontColor("#dbeafe")
                    .End()
                    .Column(1))
            .Columns(Star, Auto)
            .Padding(10, 7, 10, 7)
            .Background("#1d4ed8");
    }

    private static EditorLine[] CreateSeed()
    {
        var lines = new EditorLine[LineCount];
        for (var index = 0; index < lines.Length; index++)
        {
            lines[index] = new EditorLine(
                $"line-{index}",
                index,
                $"var value_{index} = compute({index});");
        }

        return lines;
    }
}

internal sealed record EditorLine(string Id, int Number, string Text);

internal sealed record EditorState(
    EditorLine[] Lines,
    bool Filtered,
    int EditVersion,
    string Status);
