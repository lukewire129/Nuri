using System.Diagnostics;
using System.Windows;
using Nuri.VirtualDom;
using Nuri.UI.Controls;
using Nuri.WPF;
using Nuri.UI.Dsl;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var label = GetOption(args, "--label") ?? "current";
        var iterations = GetIntOption(args, "--iterations", 100);
        var size = GetIntOption(args, "--size", 1_000);
        var warmup = GetIntOption(args, "--warmup", 10);

        if (args.Contains("--explorer-comparison", StringComparer.Ordinal))
            return RunExplorerComparison(label);

        var scenarios = new[]
        {
            new Scenario("Initial build", () => PerfTreeFactory.CreateReorderedTree(size), false),
            new Scenario("Keyed reorder", () => PerfTreeFactory.CreateReorderedTree(size), true),
            new Scenario("Todo screen initial build", () => PerfTreeFactory.CreateTodoTree(size, false), false),
            new Scenario("Todo screen keyed reorder", () => PerfTreeFactory.CreateTodoTree(size, true), true),
            new Scenario("Virtualized 10k initial source", () => CreateVirtualizedTree(10_000), false, 10_000),
            new Scenario("Virtualized 10k selection update", () => CreateVirtualizedTree(10_000), true, 10_000),
        };

        var results = new List<Result>();

        foreach (var scenario in scenarios)
        {
            var trees = scenario.CreateTrees();
            var operations = scenario.ApplyDiff ? VirtualTreeDiff.Diff(trees.Old, trees.New) : Array.Empty<PatchOperation>();

            for (var i = 0; i < warmup; i++)
                RunOnce(trees.Old, operations, scenario.ApplyDiff);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
                RunOnce(trees.Old, operations, scenario.ApplyDiff);

            stopwatch.Stop();

            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            results.Add(new Result(
                label,
                scenario.Name,
                scenario.ReportedSize ?? size,
                iterations,
                stopwatch.Elapsed.TotalMilliseconds / iterations,
                allocatedBytes / (double)iterations,
                operations.Count));
        }

        PrintResults(label, size, iterations, warmup, results);
        return 0;
    }

    private static int RunExplorerComparison(string label)
    {
        const int folderCount = 100;
        const int filesPerFolder = 100;
        var rows = new List<ExplorerPerfRow>(1 + folderCount + folderCount * filesPerFolder)
        {
            new ExplorerPerfRow("workspace", "Nuri.Generated", 0, true)
        };
        for (var folderIndex = 0; folderIndex < folderCount; folderIndex++)
        {
            rows.Add(new ExplorerPerfRow($"folder-{folderIndex}", $"Generated Folder {folderIndex:D3}", 1, true));
            for (var fileIndex = 0; fileIndex < filesPerFolder; fileIndex++)
            {
                rows.Add(new ExplorerPerfRow(
                    $"file-{folderIndex}-{fileIndex}",
                    $"document-{folderIndex:D3}-{fileIndex:D3}.txt",
                    2,
                    false));
            }
        }

        var warmupRows = rows.Take(20).ToArray();
        MeasureExplorerBuild(warmupRows, virtualized: false);
        MeasureExplorerBuild(warmupRows, virtualized: true);
        var eager = MeasureExplorerBuild(rows, virtualized: false);
        var virtualized = MeasureExplorerBuild(rows, virtualized: true);

        Console.WriteLine($"# Explorer materialization comparison ({label})");
        Console.WriteLine();
        Console.WriteLine($"Rows: {rows.Count:N0}, Viewport: 700px, Item extent: 36px");
        Console.WriteLine();
        Console.WriteLine("| Mode | Total ms | Alloc MB | Item templates | Native row containers |");
        Console.WriteLine("|---|---:|---:|---:|---:|");
        Console.WriteLine($"| Eager | {eager.Milliseconds:F2} | {eager.AllocatedBytes / 1024.0 / 1024.0:F2} | {eager.TemplateCalls:N0} | {eager.RealizedRows:N0} |");
        Console.WriteLine($"| Virtualized | {virtualized.Milliseconds:F2} | {virtualized.AllocatedBytes / 1024.0 / 1024.0:F2} | {virtualized.TemplateCalls:N0} | {virtualized.RealizedRows:N0} |");
        Console.WriteLine();
        Console.WriteLine($"Materialization time ratio: {eager.Milliseconds / virtualized.Milliseconds:F1}x");
        Console.WriteLine($"Allocation ratio: {eager.AllocatedBytes / (double)virtualized.AllocatedBytes:F1}x");
        return 0;
    }

    private static ExplorerBuildResult MeasureExplorerBuild(IReadOnlyList<ExplorerPerfRow> rows, bool virtualized)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var templateCalls = 0;
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        FrameworkElement native;
        if (virtualized)
        {
            var element = Component.VirtualizedItems(
                rows,
                row => row.Id,
                36,
                row =>
                {
                    templateCalls++;
                    return CreateExplorerRow(row);
                });
            native = WpfVirtualEntryRenderer.Build(element.ToVirtualEntry().WithIdentity("explorer-virtual", null));
        }
        else
        {
            var elements = rows.Select(row =>
            {
                templateCalls++;
                return CreateExplorerRow(row);
            }).ToArray();
            var element = Component.Div(DivTypes.Column, elements);
            native = WpfVirtualEntryRenderer.Build(element.ToVirtualEntry().WithIdentity("explorer-eager", null));
        }

        var realizedRows = rows.Count;
        var window = new Window
        {
            Width = 700,
            Height = 700,
            Content = native,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Opacity = 0
        };
        window.Show();
        native.UpdateLayout();
        if (virtualized)
        {
            var listBox = (System.Windows.Controls.ListBox)native;
            realizedRows = 0;
            for (var index = 0; index < listBox.Items.Count; index++)
            {
                if (listBox.ItemContainerGenerator.ContainerFromIndex(index) != null)
                    realizedRows++;
            }
        }

        stopwatch.Stop();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        window.Close();
        return new ExplorerBuildResult(stopwatch.Elapsed.TotalMilliseconds, allocatedBytes, templateCalls, realizedRows);
    }

    private static IElement CreateExplorerRow(ExplorerPerfRow row)
    {
        return Component.Grid(
                Component.Button(row.IsFolder ? "-" : " ")
                    .Height(31)
                    .Column(0),
                Component.Button($"{(row.IsFolder ? "[D]" : "[F]")}  {row.Name}")
                    .Height(31)
                    .TextStart()
                    .Padding(10, 4, 10, 4)
                    .Margin(left: 5)
                    .Column(1))
            .Columns(34, Component.Star)
            .Margin(left: row.Depth * 18, bottom: 5);
    }

    private static void RunOnce(VirtualEntry oldTree, IReadOnlyList<PatchOperation> operations, bool applyDiff)
    {
        var root = WpfVirtualEntryRenderer.Build(oldTree);
        if (applyDiff)
            WpfVirtualEntryRenderer.ApplyDiff(root, operations);

        if (root is IDisposable disposable)
            disposable.Dispose();
    }

    private static void PrintResults(string label, int size, int iterations, int warmup, IReadOnlyList<Result> results)
    {
        Console.WriteLine($"# Nuri WPF Performance ({label})");
        Console.WriteLine();
        Console.WriteLine($"Size: {size}, Iterations: {iterations}, Warmup: {warmup}");
        Console.WriteLine();
        Console.WriteLine("| Label | Scenario | Size | Iterations | Mean ms | Alloc KB | Patch count |");
        Console.WriteLine("|---|---:|---:|---:|---:|---:|---:|");

        foreach (var result in results)
            Console.WriteLine($"| {result.Label} | {result.Scenario} | {result.Size} | {result.Iterations} | {result.MeanMilliseconds:F4} | {result.AllocatedBytes / 1024.0:F2} | {result.PatchCount:F1} |");

        Console.WriteLine();
        Console.WriteLine("```tsv");
        Console.WriteLine("label\tscenario\tsize\titerations\tmean_ms\talloc_kb\tpatch_count");

        foreach (var result in results)
            Console.WriteLine($"{result.Label}\t{result.Scenario}\t{result.Size}\t{result.Iterations}\t{result.MeanMilliseconds:F4}\t{result.AllocatedBytes / 1024.0:F2}\t{result.PatchCount:F1}");

        Console.WriteLine("```");
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
                return args[i + 1];
        }

        return null;
    }

    private static int GetIntOption(string[] args, string name, int fallback)
    {
        var value = GetOption(args, name);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static (VirtualEntry Old, VirtualEntry New) CreateVirtualizedTree(int size)
    {
        var oldItems = Enumerable.Range(0, size).Select(index => new VirtualizedPerfRow(index, false)).ToArray();
        var newItems = (VirtualizedPerfRow[])oldItems.Clone();
        newItems[size / 2] = newItems[size / 2] with { Selected = true };

        return (CreateVirtualizedEntry(oldItems), CreateVirtualizedEntry(newItems));
    }

    private static VirtualEntry CreateVirtualizedEntry(VirtualizedPerfRow[] items)
    {
        var element = Component.VirtualizedItems(
            items,
            item => item.Index.ToString(),
            32,
            item => Component.Text(item.Selected ? $"selected:{item.Index}" : item.Index.ToString()));
        return element.ToVirtualEntry().WithIdentity("virtualized-perf", null);
    }

    private sealed record Scenario(
        string Name,
        Func<(VirtualEntry Old, VirtualEntry New)> CreateTrees,
        bool ApplyDiff,
        int? ReportedSize = null);

    private sealed record VirtualizedPerfRow(int Index, bool Selected);

    private sealed record ExplorerPerfRow(string Id, string Name, int Depth, bool IsFolder);

    private sealed record ExplorerBuildResult(
        double Milliseconds,
        long AllocatedBytes,
        int TemplateCalls,
        int RealizedRows);

    private sealed record Result(
        string Label,
        string Scenario,
        int Size,
        int Iterations,
        double MeanMilliseconds,
        double AllocatedBytes,
        double PatchCount);
}
