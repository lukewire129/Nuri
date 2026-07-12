using System.Diagnostics;
using Nuri.Runtime.Invalidation;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.VirtualDom;

var label = GetOption(args, "--label") ?? "current";
var iterations = GetIntOption(args, "--iterations", 100);
var size = GetIntOption(args, "--size", 1_000);
var warmup = GetIntOption(args, "--warmup", 10);
var sustainedIterations = GetIntOption(args, "--sustained-iterations", 100_000);
var sustainedWarmup = GetIntOption(args, "--sustained-warmup", 10_000);

var scenarios = new[]
{
    CreateKeyedReorderScenario(size),
    CreateHookRenderScenario(1),
    CreateHookRenderScenario(10),
    CreateHookRenderScenario(50),
    CreateHookRenderScenario(100),
    CreateKeyedStateScenario(size),
    CreateInvalidationEnqueueScenario(size),
    CreateInvalidationScenario(size),
    CreateEffectChurnScenario(size),
    CreateEffectUpdateScenario(100, changing: false),
    CreateEffectUpdateScenario(100, changing: true),
    CreateHookMountScenario(1),
    CreateHookMountScenario(10),
    CreateHookMountScenario(50),
    CreateHookMountScenario(100),
};

var results = new List<Result>();
foreach (var scenario in scenarios)
{
    for (var i = 0; i < warmup; i++)
        scenario.Run();

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var stopwatch = Stopwatch.StartNew();
    var operationCount = 0L;
    for (var i = 0; i < iterations; i++)
        operationCount += scenario.Run();

    stopwatch.Stop();
    var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    scenario.Cleanup();

    results.Add(new Result(
        label,
        scenario.Name,
        scenario.Size,
        iterations,
        stopwatch.Elapsed.TotalMilliseconds / iterations,
        allocatedBytes / (double)iterations,
        operationCount / (double)iterations));
}

Console.WriteLine($"# Nuri Performance ({label})");
Console.WriteLine();
Console.WriteLine($"Default size: {size}, Iterations: {iterations}, Warmup: {warmup}");
Console.WriteLine();
Console.WriteLine("| Label | Scenario | Size | Iterations | Mean ms | Alloc KB | Result count |");
Console.WriteLine("|---|---:|---:|---:|---:|---:|---:|");
foreach (var result in results)
    Console.WriteLine($"| {result.Label} | {result.Scenario} | {result.Size} | {result.Iterations} | {result.MeanMilliseconds:F4} | {result.AllocatedBytes / 1024.0:F2} | {result.OperationCount:F1} |");

Console.WriteLine();
Console.WriteLine("```tsv");
Console.WriteLine("label\tscenario\tsize\titerations\tmean_ms\talloc_kb\tresult_count");
foreach (var result in results)
    Console.WriteLine($"{result.Label}\t{result.Scenario}\t{result.Size}\t{result.Iterations}\t{result.MeanMilliseconds:F4}\t{result.AllocatedBytes / 1024.0:F2}\t{result.OperationCount:F1}");
Console.WriteLine("```");

var sustainedResults = new[] { 10_000, sustainedIterations }
    .Distinct()
    .Select(count => MeasureSustainedHookRender(label, 50, count, sustainedWarmup))
    .ToArray();

Console.WriteLine();
Console.WriteLine("## Sustained state-hook throughput");
Console.WriteLine();
Console.WriteLine("| Label | Hooks | Renders | Total ms | Renders/sec | Alloc/render KB | Total alloc MB | Gen0 | Gen1 | Gen2 | Result count |");
Console.WriteLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
foreach (var result in sustainedResults)
    Console.WriteLine($"| {result.Label} | {result.HookCount} | {result.Renders} | {result.TotalMilliseconds:F2} | {result.RendersPerSecond:F0} | {result.AllocatedBytesPerRender / 1024.0:F2} | {result.TotalAllocatedBytes / 1024.0 / 1024.0:F2} | {result.Gen0Collections} | {result.Gen1Collections} | {result.Gen2Collections} | {result.OperationCount:F1} |");

Console.WriteLine();
Console.WriteLine("```tsv");
Console.WriteLine("label\thooks\trenders\ttotal_ms\trenders_per_sec\talloc_per_render_kb\ttotal_alloc_mb\tgen0\tgen1\tgen2\tresult_count");
foreach (var result in sustainedResults)
    Console.WriteLine($"{result.Label}\t{result.HookCount}\t{result.Renders}\t{result.TotalMilliseconds:F2}\t{result.RendersPerSecond:F0}\t{result.AllocatedBytesPerRender / 1024.0:F2}\t{result.TotalAllocatedBytes / 1024.0 / 1024.0:F2}\t{result.Gen0Collections}\t{result.Gen1Collections}\t{result.Gen2Collections}\t{result.OperationCount:F1}");
Console.WriteLine("```");

static SustainedResult MeasureSustainedHookRender(string label, int hookCount, int renders, int warmup)
{
    var component = new PerfComponent { Id = $"perf-sustained-hooks-{hookCount}-{renders}" };
    for (var i = 0; i < warmup; i++)
        component.RenderStateHooks(hookCount);

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var gen0Before = GC.CollectionCount(0);
    var gen1Before = GC.CollectionCount(1);
    var gen2Before = GC.CollectionCount(2);
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var operationCount = 0L;
    var stopwatch = Stopwatch.StartNew();
    for (var i = 0; i < renders; i++)
        operationCount += component.RenderStateHooks(hookCount);
    stopwatch.Stop();
    var totalAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

    Component.DisposeHookState(component.Id);
    return new SustainedResult(
        label,
        hookCount,
        renders,
        stopwatch.Elapsed.TotalMilliseconds,
        renders / stopwatch.Elapsed.TotalSeconds,
        totalAllocatedBytes,
        totalAllocatedBytes / (double)renders,
        GC.CollectionCount(0) - gen0Before,
        GC.CollectionCount(1) - gen1Before,
        GC.CollectionCount(2) - gen2Before,
        operationCount / (double)renders);
}

static Scenario CreateKeyedReorderScenario(int size)
{
    var trees = CreateReorderedTree(size);
    return new Scenario("Keyed reorder patches", size, () => VirtualTreeDiff.Diff(trees.Old, trees.New).Count, () => { });
}

static Scenario CreateHookRenderScenario(int hookCount)
{
    var component = new PerfComponent { Id = $"perf-hooks-{hookCount}" };
    return new Scenario($"Stable render with {hookCount} state hooks", hookCount, () => component.RenderStateHooks(hookCount), () => Component.DisposeHookState(component.Id));
}

static Scenario CreateHookMountScenario(int hookCount)
{
    var sequence = 0;
    return new Scenario($"First mount with {hookCount} state hooks", hookCount, () =>
    {
        var component = new PerfComponent { Id = $"perf-hook-mount-{hookCount}-{sequence++}" };
        component.RenderStateHooks(hookCount);
        Component.DisposeHookState(component.Id);
        return hookCount;
    }, () => { });
}

static Scenario CreateKeyedStateScenario(int size)
{
    var sequence = 0;
    return new Scenario("Keyed components mount/state/dispose", size, () =>
    {
        var rootId = "perf-keyed-state-" + sequence++;
        for (var i = 0; i < size; i++)
        {
            var component = new PerfComponent().Key("item-" + i);
            component.LoadNodeNumber(rootId, i + 1);
            component.RenderStateHooks(1);
        }

        Component.DisposeHookState(rootId);
        return size;
    }, () => { });
}

static Scenario CreateInvalidationScenario(int size)
{
    var parent = new PerfComponent();
    parent.LoadNodeNumber("perf-invalidation", 1);
    var children = new PerfComponent[size];
    for (var i = 0; i < size; i++)
    {
        children[i] = new PerfComponent().Key("child-" + i);
        children[i].LoadNodeNumber(parent.Id, i + 1);
    }

    return new Scenario("Parent/child invalidation coalescing", size, () =>
    {
        var queue = new ComponentInvalidationQueue();
        foreach (var child in children)
            queue.Enqueue(child);
        queue.Enqueue(parent);
        return queue.DrainCoveredByParents().Count;
    }, () => Component.DisposeHookState(parent.Id));
}

static Scenario CreateInvalidationEnqueueScenario(int size)
{
    var parent = new PerfComponent();
    parent.LoadNodeNumber("perf-enqueue", 1);
    var children = new PerfComponent[size];
    for (var i = 0; i < size; i++)
    {
        children[i] = new PerfComponent().Key($"enqueue-child-{i}");
        children[i].LoadNodeNumber(parent.Id, i + 1);
    }

    return new Scenario("Invalidation enqueue only", size, () =>
    {
        var queue = new ComponentInvalidationQueue();
        foreach (var child in children)
            queue.Enqueue(child);
        queue.Enqueue(parent);
        return queue.HasPending ? 1 : 0;
    }, () => Component.DisposeHookState(parent.Id));
}

static Scenario CreateEffectChurnScenario(int size)
{
    var sequence = 0;
    var cleanupCount = 0;
    return new Scenario("Effect mount/unmount", size, () =>
    {
        var rootId = "perf-effects-" + sequence++;
        var before = cleanupCount;
        for (var i = 0; i < size; i++)
        {
            var component = new PerfComponent().Key("effect-" + i);
            component.LoadNodeNumber(rootId, i + 1);
            component.RegisterEffect(() => cleanupCount++);
        }

        PerfComponent.FlushEffects();
        Component.DisposeHookState(rootId);
        return cleanupCount - before;
    }, () => { });
}

static Scenario CreateEffectUpdateScenario(int hookCount, bool changing)
{
    var component = new PerfComponent { Id = $"perf-effect-update-{(changing ? "changing" : "stable")}" };
    var sequence = 0;
    component.RegisterEffects(hookCount, 0, changing);
    PerfComponent.FlushEffects();

    return new Scenario(
        $"Effect {(changing ? "dependency update" : "stable update")} ({hookCount})",
        hookCount,
        () =>
        {
            component.RegisterEffects(hookCount, changing ? ++sequence : 0, changing);
            PerfComponent.FlushEffects();
            return hookCount;
        },
        () => Component.DisposeHookState(component.Id));
}

static string? GetOption(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (string.Equals(args[i], name, StringComparison.Ordinal))
            return args[i + 1];
    return null;
}

static int GetIntOption(string[] args, string name, int fallback)
    => int.TryParse(GetOption(args, name), out var parsed) ? parsed : fallback;

static (VirtualEntry Old, VirtualEntry New) CreateReorderedTree(int size)
{
    var oldChildren = Enumerable.Range(0, size).Select(CreateItem).ToArray();
    var newChildren = Enumerable.Range(1, Math.Max(0, size - 1)).Append(0).Select(CreateItem).ToArray();
    return (CreateRoot(oldChildren), CreateRoot(newChildren));
}

static VirtualEntry CreateRoot(IEnumerable<VirtualEntry> children)
    => new VirtualEntry(VirtualControlTypes.Div, kind: DivTypes.Column, children: children).WithIdentity("0", null);

static VirtualEntry CreateItem(int index)
    => new VirtualEntry(VirtualControlTypes.Text, key: $"item-{index}", properties: new[] { KeyValuePair.Create<string, object?>("Index", index) });

sealed class PerfComponent : Component
{
    public int RenderStateHooks(int count)
    {
        ResetStateIndexForRender();
        for (var i = 0; i < count; i++)
            useState(i);
        CompleteRenderHooks();
        return count;
    }

    public void RegisterEffect(Action cleanup)
    {
        ResetStateIndexForRender();
        useEffect(() => cleanup, []);
        CompleteRenderHooks();
    }

    public void RegisterEffects(int count, int dependency, bool changing)
    {
        ResetStateIndexForRender();
        for (var i = 0; i < count; i++)
        {
            var index = i;
            useEffect(() => () => { _ = index; }, changing ? dependency : 0);
        }

        CompleteRenderHooks();
    }

    public static void FlushEffects() => Component.FlushPendingEffects();
    public override IElement Render() => Div();
}

sealed record Scenario(string Name, int Size, Func<int> Run, Action Cleanup);
sealed record Result(string Label, string Scenario, int Size, int Iterations, double MeanMilliseconds, double AllocatedBytes, double OperationCount);
sealed record SustainedResult(
    string Label,
    int HookCount,
    int Renders,
    double TotalMilliseconds,
    double RendersPerSecond,
    long TotalAllocatedBytes,
    double AllocatedBytesPerRender,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    double OperationCount);
