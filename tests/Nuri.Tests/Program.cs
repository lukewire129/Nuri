using Nuri.VirtualDom;
using Nuri.Runtime;
using Nuri.Runtime.Invalidation;
using Nuri.UI;
using Nuri.UI.Dsl;
using Nuri.UI.Navigation;
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
        KeyedComponentsKeepDistinctHookLifetimesAtTheSamePosition();
        RuntimeAncestryCleansAndCoalescesKeyedSubtrees();
        RemovingHooksFromARenderCleansUpTheirState();
        StoreSetInvalidatesOnlySubscribedComponents();
        StoreCleanupPreventsUnmountedComponentInvalidation();
        StoreSelectorInvalidatesOnlyWhenSelectedValueChanges();
        StoreSelectorInvalidatesComponentOnlyOnceWhenMultipleSelectionsChange();
        NavigationUsesLatestStateForConsecutiveUpdates();
        MultipleNavigationHooksKeepIndependentState();
        NestedNavigationComponentsKeepIndependentState();
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

    private static void StoreSetInvalidatesOnlySubscribedComponents()
    {
        var store = new Store<string>("Guest");
        var subscribed = new StoreProbe { Id = "store-subscribed" };
        var notSubscribed = new StoreProbe { Id = "store-not-subscribed" };
        var invalidated = new List<string>();

        subscribed.UseSharedStore(store);
        subscribed.CompleteHooks();
        notSubscribed.RenderWithoutStore();
        notSubscribed.CompleteHooks();

        void OnAnyStateChanged(object? _, Component component)
        {
            invalidated.Add(component.Id);
        }

        Component.AnyStateChanged += OnAnyStateChanged;
        try
        {
            store.Set("Dana");
        }
        finally
        {
            Component.AnyStateChanged -= OnAnyStateChanged;
            Component.DisposeHookState(subscribed.Id);
            Component.DisposeHookState(notSubscribed.Id);
        }

        AssertEqual(1, invalidated.Count, "Store.Set should invalidate only components that called useStore.");
        AssertEqual(subscribed.Id, invalidated[0], "Store.Set should invalidate the subscribed component.");
    }

    private static void StoreCleanupPreventsUnmountedComponentInvalidation()
    {
        var store = new Store<string>("Guest");
        var component = new StoreProbe { Id = "store-unmounted" };
        var invalidated = new List<string>();

        component.UseSharedStore(store);
        component.CompleteHooks();
        Component.DisposeHookState(component.Id);

        void OnAnyStateChanged(object? _, Component dirtyComponent)
        {
            invalidated.Add(dirtyComponent.Id);
        }

        Component.AnyStateChanged += OnAnyStateChanged;
        try
        {
            store.Set("Dana");
        }
        finally
        {
            Component.AnyStateChanged -= OnAnyStateChanged;
        }

        AssertEqual(0, invalidated.Count, "Unmounted store subscribers should not be invalidated.");
    }

    private static void StoreSelectorInvalidatesOnlyWhenSelectedValueChanges()
    {
        var store = new Store<StoreTestState>(new StoreTestState("Guest", "User", 0));
        var nameComponent = new StoreProbe { Id = "store-selector-name" };
        var roleComponent = new StoreProbe { Id = "store-selector-role" };
        var countComponent = new StoreProbe { Id = "store-selector-count" };
        var invalidated = new List<string>();

        nameComponent.UseSharedStore(store, state => state.Name);
        nameComponent.CompleteHooks();
        roleComponent.UseSharedStore(store, state => state.Role);
        roleComponent.CompleteHooks();
        countComponent.UseSharedStore(store, state => state.LoginCount);
        countComponent.CompleteHooks();

        void OnAnyStateChanged(object? _, Component component)
        {
            invalidated.Add(component.Id);
        }

        Component.AnyStateChanged += OnAnyStateChanged;
        try
        {
            store.Set(new StoreTestState("Dana", "User", 0));
        }
        finally
        {
            Component.AnyStateChanged -= OnAnyStateChanged;
            Component.DisposeHookState(nameComponent.Id);
            Component.DisposeHookState(roleComponent.Id);
            Component.DisposeHookState(countComponent.Id);
        }

        AssertEqual(1, invalidated.Count, "Store selector should invalidate only changed selected values.");
        AssertEqual(nameComponent.Id, invalidated[0], "Changing Name should invalidate only the Name subscriber.");
    }

    private static void StoreSelectorInvalidatesComponentOnlyOnceWhenMultipleSelectionsChange()
    {
        var store = new Store<StoreTestState>(new StoreTestState("Guest", "User", 0));
        var component = new StoreProbe { Id = "store-selector-multi" };
        var invalidated = new List<string>();

        component.UseSharedStore(store, state => state.Name);
        component.UseSharedStore(store, state => state.Role);
        component.CompleteHooks();

        void OnAnyStateChanged(object? _, Component dirtyComponent)
        {
            invalidated.Add(dirtyComponent.Id);
        }

        Component.AnyStateChanged += OnAnyStateChanged;
        try
        {
            store.Set(new StoreTestState("Dana", "Admin", 0));
        }
        finally
        {
            Component.AnyStateChanged -= OnAnyStateChanged;
            Component.DisposeHookState(component.Id);
        }

        AssertEqual(1, invalidated.Count, "A component with multiple dirty store selectors should be invalidated once.");
        AssertEqual(component.Id, invalidated[0], "The component with changed selectors should be invalidated.");
    }

    private static void RouterAssignsSelectedRouteKey()
    {
        var router = Component.Router("form",
            Component.Route("counter", () => Component.Text("Counter")),
            Component.Route("form", () => new HookProbe()));

        var rendered = router.Render();
        AssertEqual("form", rendered.Key, "Router should key selected route content by route key.");
    }

    private static void KeyedComponentsKeepDistinctHookLifetimesAtTheSamePosition()
    {
        var log = new List<string>();
        var profile = new LifecycleProbe("profile", log).Key("profile");
        var billing = new LifecycleProbe("billing", log).Key("billing");

        profile.LoadNodeNumber("tabs", 1);
        billing.LoadNodeNumber("tabs", 1);

        AssertNotEqual(profile.Id, billing.Id, "Different component keys at the same position must produce different hook identities.");

        profile.RegisterMountEffect();
        profile.FlushEffects();
        billing.RegisterMountEffect();
        Component.DisposeHookState(profile.Id);
        billing.FlushEffects();

        AssertEqual("mount:profile", log[0], "The original keyed component should mount once.");
        AssertEqual("cleanup:profile", log[1], "Replacing a keyed component should clean up the previous component.");
        AssertEqual("mount:billing", log[2], "The replacement keyed component should mount with a distinct hook identity.");
        Component.DisposeHookState(billing.Id);
    }

    private static void RuntimeAncestryCleansAndCoalescesKeyedSubtrees()
    {
        var log = new List<string>();
        var parent = new LifecycleProbe("parent", log);
        parent.LoadNodeNumber("runtime-tree", 1);
        var child = new LifecycleProbe("child", log).Key("value#key:with_separator");
        child.LoadNodeNumber(parent.Id, 1);
        child.RegisterMountEffect();
        child.FlushEffects();

        var queue = new ComponentInvalidationQueue();
        queue.Enqueue(child);
        queue.Enqueue(parent);
        var invalidations = queue.DrainCoveredByParents();

        AssertEqual(1, invalidations.Count, "A parent invalidation should cover keyed descendants using runtime ancestry.");
        AssertEqual(parent.Id, invalidations[0].ComponentId, "The parent should be the retained subtree invalidation.");

        Component.DisposeHookState(parent.Id);
        AssertEqual("cleanup:child", log.Last(), "Disposing a parent subtree should clean up keyed descendants without parsing their ids.");
    }

    private static void NavigationUsesLatestStateForConsecutiveUpdates()
    {
        var component = new NavigationProbe { Id = "navigation-consecutive" };
        var (_, navigator) = component.UseNavigation("home");

        navigator.Navigate("details");
        navigator.Navigate("summary");
        navigator.Replace("confirmation");
        navigator.GoBack();

        component.ResetStateIndexForRender();
        var (state, _) = component.UseNavigation("home");

        AssertEqual("details", state.CurrentRoute, "Consecutive navigation updates should use the latest state.");
        AssertEqual(1, state.BackStack.Count, "GoBack should remove only the latest history entry.");
        AssertEqual("home", state.BackStack[0], "Navigation history should retain the initial route.");
        Component.DisposeHookState(component.Id);
    }

    private static void MultipleNavigationHooksKeepIndependentState()
    {
        var component = new NavigationProbe { Id = "navigation-multiple" };
        var (_, primaryNavigator) = component.UseNavigation("primary-home");
        var (_, secondaryNavigator) = component.UseNavigation("secondary-home");

        primaryNavigator.Navigate("primary-details");
        secondaryNavigator.Navigate("secondary-settings");

        component.ResetStateIndexForRender();
        var (primaryState, _) = component.UseNavigation("primary-home");
        var (secondaryState, _) = component.UseNavigation("secondary-home");

        AssertEqual("primary-details", primaryState.CurrentRoute, "The first navigation hook should retain its own route.");
        AssertEqual("secondary-settings", secondaryState.CurrentRoute, "The second navigation hook should retain its own route.");
        AssertEqual("primary-home", primaryState.BackStack[0], "The first navigation hook should retain its own history.");
        AssertEqual("secondary-home", secondaryState.BackStack[0], "The second navigation hook should retain its own history.");
        Component.DisposeHookState(component.Id);
    }

    private static void NestedNavigationComponentsKeepIndependentState()
    {
        var parent = new NavigationProbe { Id = "navigation-parent" };
        var child = new NavigationProbe { Id = "navigation-parent_0" };
        var (_, parentNavigator) = parent.UseNavigation("parent-home");
        var (_, childNavigator) = child.UseNavigation("child-home");

        parentNavigator.Navigate("parent-details");
        childNavigator.Navigate("child-settings");

        parent.ResetStateIndexForRender();
        child.ResetStateIndexForRender();
        var (parentState, _) = parent.UseNavigation("parent-home");
        var (childState, _) = child.UseNavigation("child-home");

        AssertEqual("parent-details", parentState.CurrentRoute, "Parent navigation should not be changed by its child router.");
        AssertEqual("child-settings", childState.CurrentRoute, "Child navigation should not be changed by its parent router.");
        Component.DisposeHookState(parent.Id);
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

    private sealed class StoreProbe : Component
    {
        public string UseSharedStore(Store<string> store)
        {
            return useStore(store);
        }

        public TResult UseSharedStore<TState, TResult>(Store<TState> store, Func<TState, TResult> selector)
        {
            return useStore(store, selector);
        }

        public void RenderWithoutStore()
        {
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

    private sealed class NavigationProbe : Component
    {
        public (NavigationState state, Navigator navigator) UseNavigation(string initialRoute)
        {
            return useNavigation(initialRoute);
        }

        public override IElement Render()
        {
            return Div();
        }
    }

    private sealed class LifecycleProbe : Component
    {
        private readonly string _name;
        private readonly List<string> _log;

        public LifecycleProbe(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public void RegisterMountEffect()
        {
            useEffect(() =>
            {
                _log.Add("mount:" + _name);
                return () => _log.Add("cleanup:" + _name);
            }, []);
        }

        public void FlushEffects()
        {
            FlushPendingEffectsForRender();
        }

        public override IElement Render()
        {
            return Div();
        }
    }

    private sealed record StoreTestState(string Name, string Role, int LoginCount);
}
