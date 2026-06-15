using System.Diagnostics;
using System.Windows;
using Nuri.VirtualDom;
using Nuri.UI.Controls;
using Nuri.WPF;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var label = GetOption(args, "--label") ?? "current";
        var iterations = GetIntOption(args, "--iterations", 100);
        var size = GetIntOption(args, "--size", 1_000);
        var warmup = GetIntOption(args, "--warmup", 10);

        var scenarios = new[]
        {
            new Scenario("Initial build", () => CreateReorderedTree(size), false),
            new Scenario("Keyed reorder", () => CreateReorderedTree(size), true),
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
                size,
                iterations,
                stopwatch.Elapsed.TotalMilliseconds / iterations,
                allocatedBytes / (double)iterations,
                operations.Count));
        }

        PrintResults(label, size, iterations, warmup, results);
        return 0;
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

    private static (VirtualEntry Old, VirtualEntry New) CreateReorderedTree(int size)
    {
        var oldChildren = new List<VirtualEntry>(size);
        var newChildren = new List<VirtualEntry>(size);

        for (var i = 0; i < size; i++)
            oldChildren.Add(CreateItem(i, keyed: true, "value"));

        for (var i = 1; i < size; i++)
            newChildren.Add(CreateItem(i, keyed: true, "value"));
        newChildren.Add(CreateItem(0, keyed: true, "value"));

        return (CreateRoot(oldChildren), CreateRoot(newChildren));
    }

    private static VirtualEntry CreateRoot(IEnumerable<VirtualEntry> children)
    {
            return new VirtualEntry(VirtualControlTypes.Div, kind: Nuri.UI.Controls.DivTypes.Column, children: children).WithIdentity("0", null);
    }

    private static VirtualEntry CreateItem(int index, bool keyed, string value)
    {
        return new VirtualEntry(
                VirtualControlTypes.Text,
                key: keyed ? $"item-{index}" : null,
                properties: new[]
            {
                KeyValuePair.Create<string, object?>("Text", value),
                KeyValuePair.Create<string, object?>("Tag", index),
            });
    }

    private sealed record Scenario(string Name, Func<(VirtualEntry Old, VirtualEntry New)> CreateTrees, bool ApplyDiff);

    private sealed record Result(
        string Label,
        string Scenario,
        int Size,
        int Iterations,
        double MeanMilliseconds,
        double AllocatedBytes,
        double PatchCount);
}
