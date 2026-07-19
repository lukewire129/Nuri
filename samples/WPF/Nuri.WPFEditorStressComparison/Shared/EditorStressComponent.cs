using System.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.WPFEditorStressComparison;

public sealed class EditorStressComponent : Component
{
    private const int LineCount = 1_000;
    private static readonly EditorLine[] Seed = CreateSeed();

    public override IElement Render()
    {
        var (state, setState) = useState(EditorStressState.Create(Seed));

        useEffect(() =>
        {
            if (!state.Running)
                return null;

            if (state.Remaining > 0)
            {
                setState(current =>
                {
                    var lines = (EditorLine[])current.Lines.Clone();
                    var completed = current.TotalIterations - current.Remaining;
                    var index = completed % lines.Length;
                    lines[index] = lines[index] with
                    {
                        Text = $"var value_{index} = compute({index}); // edit {current.Revision + 1}"
                    };

                    return current with
                    {
                        Lines = lines,
                        Remaining = current.Remaining - 1,
                        Revision = current.Revision + 1,
                        Status = $"Running {completed + 1:N0}/{current.TotalIterations:N0}"
                    };
                });
                return null;
            }

            var elapsed = Stopwatch.GetElapsedTime(state.StartedTimestamp).TotalMilliseconds;
            var allocatedBytes = GC.GetTotalAllocatedBytes(false) - state.StartedAllocatedBytes;
            setState(current => current with
            {
                Running = false,
                LastElapsedMilliseconds = elapsed,
                LastAllocatedBytes = allocatedBytes,
                Status = $"Completed {current.TotalIterations:N0} committed edits"
            });
            return null;
        }, state.Running, state.Remaining);

        void Start(int iterations)
        {
            setState(current =>
            {
                if (current.Running)
                    return current;

                return current with
                {
                    Running = true,
                    Remaining = iterations,
                    TotalIterations = iterations,
                    StartedTimestamp = Stopwatch.GetTimestamp(),
                    StartedAllocatedBytes = GC.GetTotalAllocatedBytes(true),
                    LastElapsedMilliseconds = 0,
                    LastAllocatedBytes = 0,
                    Status = $"Starting {iterations:N0} committed edits"
                };
            });
        }

        void EditOnce()
        {
            setState(current =>
            {
                if (current.Running)
                    return current;

                var lines = (EditorLine[])current.Lines.Clone();
                var index = lines.Length / 2;
                lines[index] = lines[index] with
                {
                    Text = $"var value_{index} = compute({index}); // manual edit {current.Revision + 1}"
                };
                return current with
                {
                    Lines = lines,
                    Revision = current.Revision + 1,
                    Status = $"Edited line {index + 1:N0}"
                };
            });
        }

        var target = GetType().Assembly.GetName().Name ?? "WPF comparison";
        return Grid(
                Header(target, state, EditOnce, () => Start(100), () => Start(1_000)).Row(0),
                Metrics(state).Row(1),
                Editor(state.Lines).Row(2),
                Text(state.Status)
                    .FontSize(12)
                    .FontColor("#e2e8f0")
                    .Padding(10, 7, 10, 7)
                    .Background(state.Running ? "#b45309" : "#1d4ed8")
                    .Row(3))
            .Rows("Auto,Auto,*,Auto")
            .Padding(16)
            .Background("#111827");
    }

    private static IElement Header(
        string target,
        EditorStressState state,
        Action editOnce,
        Action run100,
        Action run1000)
    {
        return Grid(
                Div(
                        Text("Nuri WPF Editor Diff Comparison")
                            .FontSize(22)
                            .FontWeight(FontWeightValue.Bold)
                            .FontColor("#f8fafc"),
                        Text($"{target} | {LineCount:N0} eager keyed lines | revision {state.Revision:N0}")
                            .FontSize(12)
                            .FontColor("#94a3b8")
                            .Margin(top: 4))
                    .Column(0),
                Grid(
                        Button("Edit once", editOnce).Height(34).Column(0),
                        Button("Run 100", run100).Height(34).Margin(left: 8).Column(1),
                        Button("Run 1,000", run1000).Height(34).Margin(left: 8).Column(2))
                    .Columns(100, 100, 110)
                    .Column(1))
            .Columns(Star, Auto)
            .Margin(bottom: 12);
    }

    private static IElement Metrics(EditorStressState state)
    {
        var millisecondsPerEdit = state.TotalIterations > 0 && state.LastElapsedMilliseconds > 0
            ? state.LastElapsedMilliseconds / state.TotalIterations
            : 0;
        var allocationPerEdit = state.TotalIterations > 0 && state.LastAllocatedBytes > 0
            ? state.LastAllocatedBytes / 1024d / state.TotalIterations
            : 0;

        return Grid(
                Metric("Elapsed", $"{state.LastElapsedMilliseconds:N1} ms", $"{millisecondsPerEdit:N3} ms/edit").Column(0),
                Metric("Allocated", $"{state.LastAllocatedBytes / 1024d / 1024d:N1} MB", $"{allocationPerEdit:N1} KB/edit").Column(1),
                Metric("Progress", state.Running ? $"{state.TotalIterations - state.Remaining:N0}/{state.TotalIterations:N0}" : "Idle", "one state update per commit").Column(2))
            .Columns(Star, Star, Star)
            .Margin(bottom: 12);
    }

    private static IElement Metric(string title, string value, string detail)
    {
        return Div(
                Text(title).FontSize(11).FontColor("#94a3b8"),
                Text(value).FontSize(20).FontWeight(FontWeightValue.Bold).FontColor("#f8fafc").Margin(top: 3),
                Text(detail).FontSize(11).FontColor("#cbd5e1").Margin(top: 3))
            .Padding(12)
            .Margin(right: 8)
            .Background("#1f2937")
            .CornerRadius(10);
    }

    private static IElement Editor(EditorLine[] lines)
    {
        var rows = lines.Select(line => (IElement)Grid(
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
            .Height(24)
            .Padding(8, 3, 8, 3)
            .Key(line.Id))
            .ToArray();

        return Grid(
                Text("VirtualTreeDiff.cs")
                    .FontSize(13)
                    .FontColor("#e5e7eb")
                    .Padding(14, 9, 14, 9)
                    .Background("#1f2937")
                    .Row(0),
                Div(DivTypes.Scroll, Div(rows).Background("#0f172a"))
                    .Row(1))
            .Rows("Auto,*");
    }

    private static EditorLine[] CreateSeed()
    {
        var lines = new EditorLine[LineCount];
        for (var index = 0; index < lines.Length; index++)
            lines[index] = new EditorLine($"line-{index}", index, $"var value_{index} = compute({index});");
        return lines;
    }
}

internal sealed record EditorLine(string Id, int Number, string Text);

internal sealed record EditorStressState(
    EditorLine[] Lines,
    bool Running,
    int Remaining,
    int TotalIterations,
    int Revision,
    long StartedTimestamp,
    long StartedAllocatedBytes,
    double LastElapsedMilliseconds,
    long LastAllocatedBytes,
    string Status)
{
    public static EditorStressState Create(EditorLine[] lines)
        => new(lines, false, 0, 0, 0, 0, 0, 0, 0, "Ready");
}
