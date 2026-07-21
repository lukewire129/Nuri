using System.Diagnostics;
using Duxel.Core;
using Nuri.Duxel;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.VirtualDom;

internal static class Program
{
    private static readonly UiFontAtlas FontAtlas = CreateFontAtlas();
    private static readonly UiFrameInfo FrameInfo = new(
        1f / 60f,
        new UiVector2(1280, 720),
        new UiVector2(1, 1));

    private static int Main(string[] args)
    {
        var label = GetOption(args, "--label") ?? "current";
        var iterations = GetIntOption(args, "--iterations", 100);
        var size = GetIntOption(args, "--size", 1_000);
        var warmup = GetIntOption(args, "--warmup", 10);

        var scenarios = new[]
        {
            new Scenario("Initial frame projection", () => PerfTreeFactory.CreateReorderedTree(size), false),
            new Scenario("Keyed reorder next frame", () => PerfTreeFactory.CreateReorderedTree(size), true),
            new Scenario("Todo screen initial frame", () => PerfTreeFactory.CreateTodoTree(size, false), false),
            new Scenario("Todo screen keyed reorder next frame", () => PerfTreeFactory.CreateTodoTree(size, true), true),
            new Scenario("Editor viewport initial frame", () => PerfTreeFactory.CreateEditorTree(size, false), false),
            new Scenario("Editor viewport single-line edit", () => PerfTreeFactory.CreateEditorTree(size, true), true),
        };

        var results = new List<Result>();
        foreach (var scenario in scenarios)
        {
            var trees = scenario.CreateTrees();
            var operations = scenario.RenderUpdate
                ? VirtualTreeDiff.Diff(trees.Old, trees.New)
                : Array.Empty<PatchOperation>();

            for (var i = 0; i < warmup; i++)
            {
                RunOnce(trees.Old, trees.New, scenario.RenderUpdate);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                RunOnce(trees.Old, trees.New, scenario.RenderUpdate);
            }

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
        PrintFirstFrameComparison(size, iterations, warmup);
        return 0;
    }

    private static void RunOnce(VirtualEntry oldTree, VirtualEntry newTree, bool renderUpdate)
    {
        using var context = CreateContext();
        var screen = new ProjectionScreen(oldTree);

        RenderFrame(context, screen);
        if (renderUpdate)
        {
            screen.Entry = newTree;
            RenderFrame(context, screen);
        }
    }

    private static UiContext CreateContext()
    {
        var context = new UiContext(
            FontAtlas,
            new UiTextureId((nuint)1),
            new UiTextureId((nuint)2));
        context.SetInput(new UiInputState(
            new UiVector2(-1, -1),
            false,
            false,
            false,
            false,
            false,
            false,
            0,
            0,
            Array.Empty<UiKeyEvent>(),
            Array.Empty<UiCharEvent>(),
            new UiKeyRepeatSettings(0.5, 0.05),
            default));
        context.SetClipRect(new UiRect(0, 0, 1280, 720));
        context.SetTextSettings(UiTextSettings.Default);
        return context;
    }

    private static UiFontAtlas CreateFontAtlas()
    {
        // Keep platform rasterization out of the measurement while still emitting glyph quads.
        var glyphs = new Dictionary<int, UiGlyphInfo>();
        for (var codepoint = 32; codepoint <= 126; codepoint++)
        {
            glyphs[codepoint] = new UiGlyphInfo(
                8,
                0,
                0,
                8,
                16,
                new UiRect(0, 0, 1, 1));
        }

        return new UiFontAtlas(
            1,
            1,
            default,
            new byte[] { 255 },
            glyphs,
            new Dictionary<uint, float>(),
            12,
            4,
            2,
            '?');
    }

    private static void RenderFrame(UiContext context, UiScreen screen)
    {
        context.NewFrame(FrameInfo);
        context.Render(screen);
        _ = context.GetDrawData();
    }

    private static void PrintResults(
        string label,
        int size,
        int iterations,
        int warmup,
        IReadOnlyList<Result> results)
    {
        Console.WriteLine($"# Nuri Duxel Performance ({label})");
        Console.WriteLine();
        Console.WriteLine($"Size: {size}, Iterations: {iterations}, Warmup: {warmup}");
        Console.WriteLine("Mode: headless immediate-mode frame projection (no Vulkan submission)");
        Console.WriteLine("Font atlas: deterministic synthetic ASCII monospace");
        Console.WriteLine();
        Console.WriteLine("| Label | Scenario | Size | Iterations | Mean ms | Alloc KB | Patch count |");
        Console.WriteLine("|---|---:|---:|---:|---:|---:|---:|");

        foreach (var result in results)
        {
            Console.WriteLine($"| {result.Label} | {result.Scenario} | {result.Size} | {result.Iterations} | {result.MeanMilliseconds:F4} | {result.AllocatedBytes / 1024.0:F2} | {result.PatchCount:F1} |");
        }

        Console.WriteLine();
        Console.WriteLine("```tsv");
        Console.WriteLine("label\tscenario\tsize\titerations\tmean_ms\talloc_kb\tpatch_count");

        foreach (var result in results)
        {
            Console.WriteLine($"{result.Label}\t{result.Scenario}\t{result.Size}\t{result.Iterations}\t{result.MeanMilliseconds:F4}\t{result.AllocatedBytes / 1024.0:F2}\t{result.PatchCount:F1}");
        }

        Console.WriteLine("```");
    }

    private static void PrintFirstFrameComparison(int size, int iterations, int warmup)
    {
        var projectionEntry = PerfTreeFactory.CreateReorderedTree(size).Old;
        var scenarios = new (string Name, Func<FirstFrameSample> Run)[]
        {
            ("Raw Duxel widgets", () => RunRawDuxelFirstFrame(size)),
            ("Prebuilt VirtualEntry projection", () => RunProjectionFirstFrame(projectionEntry)),
            ("Full Nuri component frame", () => RunNuriFirstFrame(size))
        };

        Console.WriteLine();
        Console.WriteLine("## First-frame CPU comparison");
        Console.WriteLine();
        Console.WriteLine("The timed region is UiContext.NewFrame through GetDrawData; Vulkan submission and present are excluded.");
        Console.WriteLine();
        Console.WriteLine("| Scenario | P50 ms | P95 ms | P99 ms | Mean alloc KB |");
        Console.WriteLine("|---|---:|---:|---:|---:|");

        foreach (var scenario in scenarios)
        {
            for (var index = 0; index < warmup; index++)
            {
                _ = scenario.Run();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var samples = new FirstFrameSample[iterations];
            for (var index = 0; index < iterations; index++)
            {
                samples[index] = scenario.Run();
            }

            var orderedMilliseconds = samples
                .Select(sample => sample.Elapsed.TotalMilliseconds)
                .Order()
                .ToArray();
            Console.WriteLine(
                $"| {scenario.Name} | {Percentile(orderedMilliseconds, 0.50):F4} | {Percentile(orderedMilliseconds, 0.95):F4} | {Percentile(orderedMilliseconds, 0.99):F4} | {samples.Average(sample => sample.AllocatedBytes) / 1024.0:F2} |");
        }
    }

    private static FirstFrameSample RunRawDuxelFirstFrame(int size)
    {
        using var context = CreateContext();
        var screen = new RawDuxelScreen(size);
        return MeasureFirstFrame(() => RenderFrame(context, screen));
    }

    private static FirstFrameSample RunProjectionFirstFrame(VirtualEntry entry)
    {
        using var context = CreateContext();
        var screen = new ProjectionScreen(entry);
        return MeasureFirstFrame(() => RenderFrame(context, screen));
    }

    private static FirstFrameSample RunNuriFirstFrame(int size)
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new FirstFrameComponent(size),
            () => { },
            "first-frame-performance",
            includeInDiagnostics: false);
        return MeasureFirstFrame(() => RenderFrame(context, screen));
    }

    private static FirstFrameSample MeasureFirstFrame(Action render)
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        render();
        stopwatch.Stop();
        return new FirstFrameSample(
            stopwatch.Elapsed,
            GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
    }

    private static double Percentile(double[] orderedValues, double percentile)
    {
        if (orderedValues.Length == 0)
        {
            return 0d;
        }

        var index = (int)Math.Ceiling(percentile * orderedValues.Length) - 1;
        return orderedValues[Math.Clamp(index, 0, orderedValues.Length - 1)];
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static int GetIntOption(string[] args, string name, int fallback)
    {
        var value = GetOption(args, name);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private sealed class ProjectionScreen : UiScreen
    {
        private readonly DuxelVirtualEntryRenderer _renderer = new();

        public ProjectionScreen(VirtualEntry entry)
        {
            Entry = entry;
        }

        public VirtualEntry Entry { get; set; }

        public override void Render(UiImmediateContext ui)
        {
            ui.BeginWindow("Nuri Duxel Performance");
            try
            {
                _renderer.Render(ui, Entry);
            }
            finally
            {
                ui.EndWindow();
            }
        }
    }

    private sealed class RawDuxelScreen(int size) : UiScreen
    {
        public override void Render(UiImmediateContext ui)
        {
            ui.BeginWindow("Raw Duxel Performance");
            try
            {
                for (var index = 0; index < size; index++)
                {
                    ui.Text("value");
                }
            }
            finally
            {
                ui.EndWindow();
            }
        }
    }

    private sealed class FirstFrameComponent(int size) : Component
    {
        public override IElement Render()
        {
            var children = new IElement[size];
            for (var index = 0; index < children.Length; index++)
            {
                children[index] = Text("value").Key($"item-{index}");
            }

            return Div(children);
        }
    }

    private sealed record Scenario(
        string Name,
        Func<(VirtualEntry Old, VirtualEntry New)> CreateTrees,
        bool RenderUpdate);

    private sealed record Result(
        string Label,
        string Scenario,
        int Size,
        int Iterations,
        double MeanMilliseconds,
        double AllocatedBytes,
        double PatchCount);

    private readonly record struct FirstFrameSample(
        TimeSpan Elapsed,
        long AllocatedBytes);
}
