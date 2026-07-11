using System.Diagnostics;
using Nuri.Runtime.Invalidation;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.VirtualDom;

var label = GetOption(args, "--label") ?? "current";
var iterations = GetIntOption(args, "--iterations", 100);
var size = GetIntOption(args, "--size", 1_000);
var warmup = GetIntOption(args, "--warmup", 10);

var scenarios = new[]
{
    CreateKeyedReorderScenario(size),
    CreateHookRenderScenario(1),
    CreateHookRenderScenario(10),
    CreateHookRenderScenario(50),
    CreateKeyedStateScenario(size),
    CreateInvalidationScenario(size),
    CreateEffectChurnScenario(size),
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

    public static void FlushEffects() => Component.FlushPendingEffects();
    public override IElement Render() => Div();
}

sealed record Scenario(string Name, int Size, Func<int> Run, Action Cleanup);
sealed record Result(string Label, string Scenario, int Size, int Iterations, double MeanMilliseconds, double AllocatedBytes, double OperationCount);
