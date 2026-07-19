using System.Diagnostics;
using System.Windows;
using Nuri.UI.Controls;
using Nuri.VirtualDom;
using Nuri.WPF;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var label = GetOption(args, "--label") ?? "current";
        var iterations = GetIntOption(args, "--iterations", 100);
        var warmup = GetIntOption(args, "--warmup", 10);
        var size = GetIntOption(args, "--size", 1_000);
        var lineTexts = Enumerable.Range(0, size)
            .Select(index => $"var value_{index} = compute({index});")
            .ToArray();

        var scenarios = new[]
        {
            CreateTreeScenario(size, lineTexts),
            CreateDiffScenario(size, lineTexts),
            CreateInitialBuildScenario(size, lineTexts),
            CreatePatchScenario(size, lineTexts),
            CreateFullUpdateScenario(size, lineTexts)
        };
        var results = new List<Result>(scenarios.Length);

        foreach (var scenario in scenarios)
        {
            scenario.Setup();
            for (var index = 0; index < warmup; index++)
                scenario.Run(index + 1);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var gen0Before = GC.CollectionCount(0);
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var resultCount = 0L;
            var stopwatch = Stopwatch.StartNew();
            for (var index = 0; index < iterations; index++)
                resultCount += scenario.Run(warmup + index + 1);
            stopwatch.Stop();
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            var gen0Collections = GC.CollectionCount(0) - gen0Before;
            scenario.Cleanup();

            results.Add(new Result(
                label,
                scenario.Name,
                size,
                iterations,
                stopwatch.Elapsed.TotalMilliseconds / iterations,
                allocatedBytes / (double)iterations,
                gen0Collections,
                resultCount / (double)iterations));
        }

        Console.WriteLine($"# Nuri WPF Phase Comparison ({label})");
        Console.WriteLine();
        Console.WriteLine($"Size: {size}, Iterations: {iterations}, Warmup: {warmup}");
        Console.WriteLine();
        Console.WriteLine("| Label | Phase | Mean ms | Alloc KB | Gen0 | Result count |");
        Console.WriteLine("|---|---|---:|---:|---:|---:|");
        foreach (var result in results)
            Console.WriteLine($"| {result.Label} | {result.Name} | {result.MeanMilliseconds:F4} | {result.AllocatedBytes / 1024d:F2} | {result.Gen0Collections} | {result.ResultCount:F1} |");

        Console.WriteLine();
        Console.WriteLine("label	phase	size	iterations	mean_ms	alloc_kb	gen0	result_count");
        foreach (var result in results)
            Console.WriteLine($"{result.Label}	{result.Name}	{result.Size}	{result.Iterations}	{result.MeanMilliseconds:F4}	{result.AllocatedBytes / 1024d:F2}	{result.Gen0Collections}	{result.ResultCount:F1}");
        return 0;
    }

    private static Scenario CreateTreeScenario(int size, string[] lineTexts)
    {
        return new Scenario(
            "Virtual tree creation",
            () => { },
            revision => CreateEditorTree(size, lineTexts, revision).Children.Count,
            () => { });
    }

    private static Scenario CreateDiffScenario(int size, string[] lineTexts)
    {
        VirtualEntry oldTree = null!;
        VirtualEntry newTree = null!;
        return new Scenario(
            "VirtualTreeDiff",
            () =>
            {
                oldTree = CreateEditorTree(size, lineTexts, 0);
                newTree = CreateEditorTree(size, lineTexts, 1);
            },
            _ => VirtualTreeDiff.Diff(oldTree, newTree).Count,
            () => { });
    }

    private static Scenario CreateInitialBuildScenario(int size, string[] lineTexts)
    {
        VirtualEntry tree = null!;
        return new Scenario(
            "WPF initial build",
            () => tree = CreateEditorTree(size, lineTexts, 0),
            _ =>
            {
                var root = WpfVirtualEntryRenderer.Build(tree);
                if (root is IDisposable disposable)
                    disposable.Dispose();
                return 1;
            },
            () => { });
    }

    private static Scenario CreatePatchScenario(int size, string[] lineTexts)
    {
        FrameworkElement root = null!;
        IReadOnlyList<PatchOperation> operations = null!;
        return new Scenario(
            "WPF property patch",
            () =>
            {
                var oldTree = CreateEditorTree(size, lineTexts, 0);
                var newTree = CreateEditorTree(size, lineTexts, 1);
                root = WpfVirtualEntryRenderer.Build(oldTree);
                operations = VirtualTreeDiff.Diff(oldTree, newTree);
            },
            _ =>
            {
                WpfVirtualEntryRenderer.ApplyDiff(root, operations);
                return operations.Count;
            },
            () =>
            {
                if (root is IDisposable disposable)
                    disposable.Dispose();
            });
    }

    private static Scenario CreateFullUpdateScenario(int size, string[] lineTexts)
    {
        FrameworkElement root = null!;
        VirtualEntry currentTree = null!;
        return new Scenario(
            "Full sequential update",
            () =>
            {
                currentTree = CreateEditorTree(size, lineTexts, 0);
                root = WpfVirtualEntryRenderer.Build(currentTree);
            },
            revision =>
            {
                var nextTree = CreateEditorTree(size, lineTexts, revision);
                var operations = VirtualTreeDiff.Diff(currentTree, nextTree);
                WpfVirtualEntryRenderer.ApplyDiff(root, operations);
                currentTree = nextTree;
                return operations.Count;
            },
            () =>
            {
                if (root is IDisposable disposable)
                    disposable.Dispose();
            });
    }

    private static VirtualEntry CreateEditorTree(int size, string[] lineTexts, int revision)
    {
        var editedIndex = size / 2;
        var children = new VirtualEntry[size];
        for (var index = 0; index < size; index++)
        {
            var text = index == editedIndex && revision > 0
                ? $"{lineTexts[index]} // edit {revision}"
                : lineTexts[index];
            children[index] = new VirtualEntry(
                VirtualControlTypes.Text,
                key: $"line-{index}",
                properties: new[]
                {
                    KeyValuePair.Create<string, object?>("Text", text)
                });
        }

        return new VirtualEntry(
            VirtualControlTypes.Div,
            kind: DivTypes.Column,
            children: children).WithIdentity("editor-root", null);
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.Ordinal))
                return args[index + 1];
        }

        return null;
    }

    private static int GetIntOption(string[] args, string name, int fallback)
        => int.TryParse(GetOption(args, name), out var value) ? value : fallback;

    private sealed record Scenario(
        string Name,
        Action Setup,
        Func<int, int> Run,
        Action Cleanup);

    private sealed record Result(
        string Label,
        string Name,
        int Size,
        int Iterations,
        double MeanMilliseconds,
        double AllocatedBytes,
        int Gen0Collections,
        double ResultCount);
}
