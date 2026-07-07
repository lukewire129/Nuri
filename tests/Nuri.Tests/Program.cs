using Nuri.VirtualDom;
using Nuri.UI;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.Tests;

internal static class Program
{
    private static void Main()
    {
        KeyedReorderPreservesPatchTargetIdentity();
        KeyedSlidingWindowPreservesRetainedChildIdentities();
        UseReducerDispatchesFromCurrentState();
        UseRefPreservesReferenceAcrossRenders();
        UseLatestTracksTheCurrentValue();
        UseMemoCachesUntilDependenciesChange();
        UseEffectRunsAfterRenderAndCleansUpOnDependencyChange();
        RemovingHooksFromARenderCleansUpTheirState();
        RouterAssignsSelectedRouteKey();
        TransitionAppliesToAllConfiguredProperties();
        Console.WriteLine("Nuri.Tests passed.");
    }

    private static void KeyedReorderPreservesPatchTargetIdentity()
    {
        var oldTree = Parent(
                Row("a", selected: false),
                Row("b", selected: false))
            .WithIdentity("root", null);

        var oldAId = oldTree.Children[0].Id;
        var oldBId = oldTree.Children[1].Id;

        var newTree = Parent(
                Row("b", selected: true),
                Row("a", selected: false))
            .WithIdentity("root", null);

        var operations = VirtualTreeDiff.Diff(oldTree, newTree);
        var selectedPatch = operations
            .OfType<UpdatePropertyPatch>()
            .Single(patch => patch.PropertyName == "Selected" && Equals(patch.Value, true));

        AssertEqual(oldBId, selectedPatch.Target.Id, "Keyed reorder should update B by B's retained id.");
        AssertNotEqual(oldAId, selectedPatch.Target.Id, "Keyed reorder must not update the row currently at B's new position.");
        AssertEqual(oldBId, newTree.Children[0].Id, "New B entry should inherit old B id.");
        AssertEqual(oldAId, newTree.Children[1].Id, "New A entry should inherit old A id.");
    }

    private static void KeyedSlidingWindowPreservesRetainedChildIdentities()
    {
        var oldTree = Parent(
                Row("a", selected: false),
                Row("b", selected: false),
                Row("c", selected: false),
                Row("d", selected: false))
            .WithIdentity("root", null);

        var oldAId = oldTree.Children[0].Id;
        var oldBId = oldTree.Children[1].Id;
        var oldCId = oldTree.Children[2].Id;
        var oldDId = oldTree.Children[3].Id;

        var newTree = Parent(
                Row("b", selected: false),
                Row("c", selected: false),
                Row("d", selected: true),
                Row("e", selected: false))
            .WithIdentity("root", null);

        var operations = VirtualTreeDiff.Diff(oldTree, newTree);
        var selectedPatch = operations
            .OfType<UpdatePropertyPatch>()
            .Single(patch => patch.PropertyName == "Selected" && Equals(patch.Value, true));
        var addedChild = operations.OfType<AddChildPatch>().Single().Child;

        AssertEqual(oldDId, selectedPatch.Target.Id, "Sliding window should update retained D by D's retained id.");
        AssertEqual(oldBId, newTree.Children[0].Id, "Retained B should inherit old B id.");
        AssertEqual(oldCId, newTree.Children[1].Id, "Retained C should inherit old C id.");
        AssertEqual(oldDId, newTree.Children[2].Id, "Retained D should inherit old D id.");
        AssertEqual(oldAId, addedChild.Id, "New E may safely reuse removed A's id after A is removed.");
    }

    private static VirtualEntry Parent(params VirtualEntry[] children)
    {
        return new VirtualEntry("Div", key: "parent", children: children);
    }

    private static void UseReducerDispatchesFromCurrentState()
    {
        var component = new HookProbe { Id = "hook-reducer" };

        var (state, dispatch) = component.UseCounter();
        AssertEqual(0, state, "Reducer should return initial state on first render.");

        dispatch(2);
        dispatch(3);

        component.ResetStateIndexForRender();
        var (updatedState, _) = component.UseCounter();
        AssertEqual(5, updatedState, "Reducer dispatch should apply actions to current stored state.");
    }

    private static void UseRefPreservesReferenceAcrossRenders()
    {
        var component = new HookProbe { Id = "hook-ref" };

        var firstRef = component.UseNumberRef();
        firstRef.Current = 42;

        component.ResetStateIndexForRender();
        var secondRef = component.UseNumberRef();

        AssertEqual(true, ReferenceEquals(firstRef, secondRef), "useRef should preserve reference identity across renders.");
        AssertEqual(42, secondRef.Current, "useRef should preserve mutable current value across renders.");
    }

    private static void UseLatestTracksTheCurrentValue()
    {
        var component = new HookProbe { Id = "hook-latest" };

        var firstRef = component.UseLatestNumber(1);
        component.ResetStateIndexForRender();
        var secondRef = component.UseLatestNumber(42);

        AssertEqual(true, ReferenceEquals(firstRef, secondRef), "useLatest should preserve reference identity across renders.");
        AssertEqual(42, secondRef.Current, "useLatest should update the current value each render.");
    }

    private static void UseMemoCachesUntilDependenciesChange()
    {
        var component = new HookProbe { Id = "hook-memo" };

        var firstValue = component.UseMemoValue("alpha");
        component.ResetStateIndexForRender();
        var secondValue = component.UseMemoValue("alpha");
        AssertEqual(1, component.MemoFactoryCallCount, "useMemo should cache within the same dependency set.");
        AssertEqual(firstValue, secondValue, "useMemo should return the cached value when dependencies do not change.");

        component.ResetStateIndexForRender();
        var thirdValue = component.UseMemoValue("beta");
        AssertEqual(2, component.MemoFactoryCallCount, "useMemo should recompute when dependencies change.");
        AssertNotEqual(secondValue, thirdValue, "useMemo should return a new value after dependency changes.");
    }

    private static void UseEffectRunsAfterRenderAndCleansUpOnDependencyChange()
    {
        var component = new HookProbe { Id = "hook-effect" };

        component.RegisterEffect("alpha");
        component.FlushEffects();
        AssertEqual("run:alpha", component.EffectLog[0], "useEffect should run after render flush.");

        component.ResetStateIndexForRender();
        component.RegisterEffect("alpha");
        component.FlushEffects();
        AssertEqual(1, component.EffectLog.Count, "useEffect should skip rerun when dependencies do not change.");

        component.ResetStateIndexForRender();
        component.RegisterEffect("beta");
        component.FlushEffects();
        AssertEqual("cleanup:alpha", component.EffectLog[1], "useEffect should clean up before rerunning on dependency changes.");
        AssertEqual("run:beta", component.EffectLog[2], "useEffect should rerun after dependency changes.");
    }

    private static void RemovingHooksFromARenderCleansUpTheirState()
    {
        var component = new HookProbe { Id = "hook-trim" };

        component.RegisterConditionalEffects(includeSecond: true);
        component.CompleteHooks();
        component.FlushEffects();

        component.ResetStateIndexForRender();
        component.RegisterConditionalEffects(includeSecond: false);
        component.CompleteHooks();

        AssertEqual("cleanup:second", component.EffectLog.Last(), "Removing a hook slot should dispose its cleanup immediately.");
    }

    private static void RouterAssignsSelectedRouteKey()
    {
        var router = Component.Router("form",
            Component.Route("counter", () => Component.Text("Counter")),
            Component.Route("form", () => new HookProbe()));

        var rendered = router.Render();
        AssertEqual("form", rendered.Key, "Router should key selected route content by route key.");
    }

    private static void TransitionAppliesToAllConfiguredProperties()
    {
        var element = Component.Grid()
            .Columns(Component.Pixels(42), Component.Star)
            .Background("#111827")
            .FontColor("#ffffff")
            .FontSize(20)
            .Transition(200);

        AssertEqual(true, element.Animations.ContainsKey("Background"), "Transition should apply to Background.");
        AssertEqual(true, element.Animations.ContainsKey("Foreground"), "Transition should apply to Foreground.");
        AssertEqual(false, element.Animations.ContainsKey("FontSize"), "Transition should skip unsupported FontSize animations.");
        AssertEqual(false, element.Animations.ContainsKey("ColumnDefinitions"), "Transition should skip grid column definitions.");

        var fontColor = element.Animations["Foreground"];
        var background = element.Animations["Background"];
        AssertEqual("Foreground", fontColor.PropertyName, "Foreground animation should target Foreground.");
        AssertEqual(element.Properties["Foreground"], fontColor.To, "Foreground animation should use the configured value.");
        AssertEqual(TimeSpan.FromMilliseconds(200), background.Duration, "Transition should use the configured duration.");
    }

    private static VirtualEntry Row(string key, bool selected)
    {
        return new VirtualEntry(
            "Div",
            key: key,
            properties: new[]
            {
                new KeyValuePair<string, object?>("Selected", selected),
                new KeyValuePair<string, object?>("Title", key.ToUpperInvariant())
            },
            children: new[]
            {
                new VirtualEntry("Text", key: $"{key}-title", properties: new[]
                {
                    new KeyValuePair<string, object?>("Text", key.ToUpperInvariant())
                })
            });
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message} Expected: {expected}; Actual: {actual}");
    }

    private static void AssertNotEqual<T>(T notExpected, T actual, string message)
    {
        if (EqualityComparer<T>.Default.Equals(notExpected, actual))
            throw new InvalidOperationException($"{message} Not expected: {notExpected}; Actual: {actual}");
    }

    private sealed class HookProbe : Component
    {
        public int MemoFactoryCallCount { get; private set; }

        public List<string> EffectLog { get; } = new List<string>();

        public (int state, Action<int> dispatch) UseCounter()
        {
            return useReducer<int, int>((state, action) => state + action, 0);
        }

        public Ref<int> UseNumberRef()
        {
            return useRef(1);
        }

        public Ref<int> UseLatestNumber(int value)
        {
            return useLatest(value);
        }

        public string UseMemoValue(string value)
        {
            return useMemo(() =>
            {
                MemoFactoryCallCount++;
                return value + ":" + MemoFactoryCallCount;
            }, value);
        }

        public void RegisterEffect(string value)
        {
            useEffect(() =>
            {
                EffectLog.Add("run:" + value);
                return () => EffectLog.Add("cleanup:" + value);
            }, value);
        }

        public void FlushEffects()
        {
            FlushPendingEffectsForRender();
        }

        public void RegisterConditionalEffects(bool includeSecond)
        {
            useEffect(() => () => EffectLog.Add("cleanup:first"), "first");

            if (includeSecond)
                useEffect(() => () => EffectLog.Add("cleanup:second"), "second");
        }

        public void CompleteHooks()
        {
            CompleteHookRender();
        }

        public override IElement Render()
        {
            return Div();
        }
    }
}
