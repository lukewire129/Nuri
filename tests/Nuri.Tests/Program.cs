using Nuri.VirtualDom;
using Nuri.Runtime;
using Nuri.Runtime.Invalidation;
using Nuri.Runtime.Diagnostics;
using Nuri.Runtime.Lifecycle;
using Nuri.UI;
using Nuri.UI.Dsl;
using Nuri.UI.Navigation;
using Nuri.UI.Values;
using Nuri.UI.Virtualization;
using Nuri.Constants;
using Nuri.Platform.Abstractions;
using Nuri.UI.Controls;

namespace Nuri.Tests;

internal static class Program
{
    private static void Main()
    {
        KeyedReorderPreservesPatchTargetIdentity();
        KeyedSlidingWindowPreservesRetainedChildIdentities();
        SubtreeRebuildPreservesRenderedDescendantIdentities();
        UseReducerDispatchesFromCurrentState();
        UseStateReusesSetterForStableLogicalComponent();
        UseStateSetterTracksLatestComponentOwner();
        DisposedStateSettersCannotInvalidateReplacementComponents();
        DisposedReducerDispatchCannotRecreateStateOrInvalidate();
        UseStateSettersRemainIsolatedAndTrimmed();
        UseRefPreservesReferenceAcrossRenders();
        UseLatestTracksTheCurrentValue();
        UseMemoCachesUntilDependenciesChange();
        UseEffectRunsAfterRenderAndCleansUpOnDependencyChange();
        KeyedComponentsKeepDistinctHookLifetimesAtTheSamePosition();
        RuntimeAncestryCleansAndCoalescesKeyedSubtrees();
        InvalidationQueuePreservesOrderingIndependentCoverage();
        RuntimeAncestryRegistryReleasesDisposedSubtrees();
        ComponentCachesAndRefreshesItsRuntimeNode();
        DiagnosticsPreserveHookSummaryWhenEnabled();
        DiagnosticsTrackPatchBatchesAndVirtualizedRows();
        DiagnosticsLogOnceDeduplicatesUntilLogsAreCleared();
        DuplicateComponentKeysUseIndependentHookIdentity();
        RemovingHooksFromARenderCleansUpTheirState();
        StoreSetInvalidatesOnlySubscribedComponents();
        StoreCleanupPreventsUnmountedComponentInvalidation();
        StoreSelectorInvalidatesOnlyWhenSelectedValueChanges();
        StoreSelectorInvalidatesComponentOnlyOnceWhenMultipleSelectionsChange();
        NavigationUsesLatestStateForConsecutiveUpdates();
        MultipleNavigationHooksKeepIndependentState();
        NestedNavigationComponentsKeepIndependentState();
        RouterAssignsSelectedRouteKey();
        LayoutDistributionDslUsesNeutralProperties();
        GridLengthDslTreatsNumbersAsPixels();
        GridLengthDslParsesStringDefinitions();
        GridLengthDslRejectsInvalidStringDefinitions();
        TransitionAppliesToAllConfiguredProperties();
        VirtualizedItemsStayLazyAndProduceKeyedChanges();
        VirtualizedItemsCaptureAnImmutableSnapshot();
        VirtualizedItemsUseSafeDuplicateIdentities();
        VirtualizedItemsRejectComponentTemplatesLazily();
        Console.WriteLine("Nuri.Tests passed.");
    }

    private static void VirtualizedItemsStayLazyAndProduceKeyedChanges()
    {
        var templateCalls = 0;
        var oldItems = new[] { new VirtualizedProbe("a", 1), new VirtualizedProbe("b", 1) };
        var newItems = new[] { new VirtualizedProbe("b", 2), new VirtualizedProbe("a", 1), new VirtualizedProbe("c", 1) };
        var oldSource = GetVirtualizedSource(Component.VirtualizedItems(
            oldItems,
            item => item.Id,
            32,
            item => { templateCalls++; return Component.Text(item.Id); }));
        var newSource = GetVirtualizedSource(Component.VirtualizedItems(
            newItems,
            item => item.Id,
            32,
            item => { templateCalls++; return Component.Text(item.Id); }));

        var oldTree = VirtualizedEntry(oldSource);
        var newTree = VirtualizedEntry(newSource);
        var patch = VirtualTreeDiff.Diff(oldTree, newTree).OfType<UpdateVirtualizedItemsPatch>().Single();

        AssertEqual(0, templateCalls, "Virtualized source creation and diffing must not invoke item templates.");
        AssertEqual(1, patch.Changes.Count(change => change.Type == VirtualizedItemChangeType.Add), "A new key should produce one virtualized add change.");
        AssertEqual(2, patch.Changes.Count(change => change.Type == VirtualizedItemChangeType.Move), "Retained keys should report their new positions.");
        AssertEqual(1, patch.Changes.Count(change => change.Type == VirtualizedItemChangeType.Update), "A changed snapshot with the same key should produce one update.");
    }

    private static void SubtreeRebuildPreservesRenderedDescendantIdentities()
    {
        var oldTitle = new VirtualEntry(
                "Text",
                properties: new[] { new KeyValuePair<string, object?>("Text", "Title") })
            .WithIdentity("component_1", "component", rewriteChildren: false);
        var oldStatus = new VirtualEntry(
                "Text",
                key: "status",
                properties: new[] { new KeyValuePair<string, object?>("Text", "Before") })
            .WithIdentity("component_2", "component", rewriteChildren: false);
        var oldComponent = new VirtualEntry("Div", children: new[] { oldTitle, oldStatus })
            .WithIdentity("component", "root", rewriteChildren: false)
            .WithComponentId("component");
        var oldRoot = new VirtualEntry("Window", children: new[] { oldComponent })
            .WithIdentity("root", null, rewriteChildren: false);

        var newTitle = new VirtualEntry(
                "Text",
                properties: new[] { new KeyValuePair<string, object?>("Text", "Title") })
            .WithIdentity("component_1", "component", rewriteChildren: false);
        var newStatus = new VirtualEntry(
                "Text",
                key: "status",
                properties: new[] { new KeyValuePair<string, object?>("Text", "After") })
            .WithIdentity("component_2", "component", rewriteChildren: false);
        var newComponent = new VirtualEntry("Div", children: new[] { newTitle, newStatus })
            .WithIdentity("component", "root", rewriteChildren: false);

        var runtime = new ApplicationRuntime<VirtualEntry>(() => oldRoot, entry => entry);
        var renderer = new CapturingRenderer();
        object? nativeRoot = null;
        var coordinator = new RenderCoordinator<VirtualEntry, object>(
            runtime,
            renderer,
            new CapturingHost(),
            () => nativeRoot,
            root => nativeRoot = root,
            _ => { });

        coordinator.Initialize();
        AssertEqual(true, coordinator.RebuildSubtree(oldComponent, newComponent, "component"), "The component subtree should remain replaceable by component identity.");

        var update = renderer.Operations.OfType<UpdatePropertyPatch>().Single();
        AssertEqual("component_2", update.Target.Id, "Subtree rebuilds must retain the rendered descendant identity used by the native control index.");
        AssertEqual(0, renderer.Operations.OfType<ReplaceEntryPatch>().Count(), "Stable mixed keyed and unkeyed descendants should not be replaced because of identity rewriting.");
    }

    private static void VirtualizedItemsCaptureAnImmutableSnapshot()
    {
        var items = new List<VirtualizedProbe>
        {
            new VirtualizedProbe("a", 1)
        };
        var source = GetVirtualizedSource(Component.VirtualizedItems(
            items,
            item => item.Id,
            32,
            item => Component.Text($"{item.Id}:{item.Version}")));

        items[0] = new VirtualizedProbe("changed", 2);
        items.Add(new VirtualizedProbe("b", 1));

        AssertEqual(1, source.Count, "A virtualized source must retain the item count captured at render time.");
        AssertEqual("a", source.GetKey(0), "A virtualized source must retain keys from the captured render snapshot.");
        AssertEqual("a:1", source.RenderItem(0).Properties["Text"], "A virtualized source must render values from the captured snapshot.");
    }

    private static void VirtualizedItemsUseSafeDuplicateIdentities()
    {
        var source = GetVirtualizedSource(Component.VirtualizedItems(
            new[] { "same", "same" },
            item => item,
            24,
            Component.Text));
        var identities = source.GetIdentities();

        AssertNotEqual(identities[0], identities[1], "Duplicate virtualized keys must receive distinct fallback identities.");
    }

    private static void VirtualizedItemsRejectComponentTemplatesLazily()
    {
        var source = GetVirtualizedSource(Component.VirtualizedItems(
            new[] { "row" },
            item => item,
            24,
            _ => new VirtualizedComponentProbe()));

        var exception = AssertThrows<InvalidOperationException>(
            () => source.RenderItem(0),
            "Virtualized templates containing components should be rejected when the row is realized.");
        AssertEqual(true, exception.Message.Contains("row") && exception.Message.Contains(nameof(VirtualizedComponentProbe)), "The template error should identify the item key and component type.");
    }

    private static IVirtualizedItemsSource GetVirtualizedSource(ItemsView view)
    {
        return (IVirtualizedItemsSource)view.Properties[PropertyKeys.VirtualizedItemsSource];
    }

    private static VirtualEntry VirtualizedEntry(IVirtualizedItemsSource source)
    {
        return new VirtualEntry(
            VirtualControlTypes.Items,
            kind: ItemsTypes.Virtualized,
            properties: new[] { KeyValuePair.Create<string, object?>(PropertyKeys.VirtualizedItemsSource, source) })
            .WithIdentity("virtualized-root", null);
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
        AssertNotEqual(oldAId, addedChild.Id, "A new key must not reuse a removed key's virtual identity.");
        AssertEqual("root#key:e", addedChild.Id, "A new key should receive its own key-derived virtual identity.");
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

    private static void UseStateReusesSetterForStableLogicalComponent()
    {
        var component = new HookProbe { Id = "hook-state-stable-setter" };
        var (_, firstSetter) = component.UseNumberState(0);

        component.ResetStateIndexForRender();
        var (_, secondSetter) = component.UseNumberState(0);

        AssertEqual(true, ReferenceEquals(firstSetter, secondSetter), "useState should reuse its setter for a stable logical hook slot.");
        Component.DisposeHookState(component.Id);
    }

    private static void UseStateSetterTracksLatestComponentOwner()
    {
        const string componentId = "hook-state-latest-owner";
        var first = new HookProbe { Id = componentId };
        var (_, firstSetter) = first.UseNumberState(0);
        var second = new HookProbe { Id = componentId };
        var (_, secondSetter) = second.UseNumberState(0);

        AssertEqual(true, ReferenceEquals(firstSetter, secondSetter), "A replacement CLR object for the same logical component should receive the same setter.");
        secondSetter(current => current + 1);
        AssertEqual(0, first.StateChangedCount, "A reused setter must not invalidate the stale CLR component object.");
        AssertEqual(1, second.StateChangedCount, "A reused setter should invalidate the latest CLR component object.");

        second.ResetStateIndexForRender();
        var (state, _) = second.UseNumberState(0);
        AssertEqual(1, state, "A reused setter should update the current logical component state.");
        Component.DisposeHookState(componentId);
    }

    private static void DisposedStateSettersCannotInvalidateReplacementComponents()
    {
        const string componentId = "hook-state-disposed-setter";
        var removed = new HookProbe { Id = componentId };
        var (_, staleSetter) = removed.UseNumberState(0);
        Component.DisposeHookState(componentId);

        var replacement = new HookProbe { Id = componentId };
        var (_, currentSetter) = replacement.UseNumberState(10);

        staleSetter(current => current + 1);
        AssertEqual(0, removed.StateChangedCount, "A disposed state setter must not invalidate its removed component.");
        AssertEqual(0, replacement.StateChangedCount, "A disposed state setter must not invalidate a replacement that reused the same id.");

        replacement.ResetStateIndexForRender();
        var (unchangedState, _) = replacement.UseNumberState(10);
        AssertEqual(10, unchangedState, "A disposed state setter must not change replacement state.");

        currentSetter(current => current + 1);
        AssertEqual(1, replacement.StateChangedCount, "The current replacement setter should remain active.");
        Component.DisposeHookState(componentId);
    }

    private static void DisposedReducerDispatchCannotRecreateStateOrInvalidate()
    {
        const string componentId = "hook-reducer-disposed-dispatch";
        var removed = new HookProbe { Id = componentId };
        var (_, staleDispatch) = removed.UseCounter();
        Component.DisposeHookState(componentId);

        var replacement = new HookProbe { Id = componentId };
        var (initialState, currentDispatch) = replacement.UseCounter();
        AssertEqual(0, initialState, "A replacement reducer should mount with fresh state.");

        staleDispatch(5);
        AssertEqual(0, removed.StateChangedCount, "A disposed reducer dispatch must not invalidate its removed component.");
        AssertEqual(0, replacement.StateChangedCount, "A disposed reducer dispatch must not invalidate a replacement that reused the same id.");

        replacement.ResetStateIndexForRender();
        var (unchangedState, _) = replacement.UseCounter();
        AssertEqual(0, unchangedState, "A disposed reducer dispatch must not recreate or change replacement state.");

        currentDispatch(2);
        AssertEqual(1, replacement.StateChangedCount, "The current replacement reducer dispatch should remain active.");
        Component.DisposeHookState(componentId);
    }

    private static void UseStateSettersRemainIsolatedAndTrimmed()
    {
        var component = new HookProbe { Id = "hook-state-trim" };
        var setters = component.UseNumberStates(2);
        component.CompleteHooks();
        AssertEqual(false, ReferenceEquals(setters[0], setters[1]), "Different state hook slots must keep distinct setters.");

        component.ResetStateIndexForRender();
        component.UseNumberStates(1);
        component.CompleteHooks();
        component.ResetStateIndexForRender();
        var remountedSetters = component.UseNumberStates(2);
        AssertEqual(true, ReferenceEquals(setters[0], remountedSetters[0]), "A retained hook slot should preserve its setter.");
        AssertEqual(false, ReferenceEquals(setters[1], remountedSetters[1]), "A trimmed hook slot should create a new setter when remounted.");
        Component.DisposeHookState(component.Id);
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
        AssertEqual(true, ComponentLifecycle.IsInSubtree(child.Id, parent.Id), "Renderer root membership should follow runtime ancestry.");
        AssertEqual(false, ComponentLifecycle.IsInSubtree(parent.Id, child.Id), "Renderer root membership should not treat an ancestor as its child's subtree.");

        Component.DisposeHookState(parent.Id);
        AssertEqual("cleanup:child", log.Last(), "Disposing a parent subtree should clean up keyed descendants without parsing their ids.");
    }

    private static void InvalidationQueuePreservesOrderingIndependentCoverage()
    {
        var firstRoot = new HookProbe();
        firstRoot.LoadNodeNumber("invalidation-roots", 1);
        var firstChild = new HookProbe().Key("first-child");
        firstChild.LoadNodeNumber(firstRoot.Id, 1);
        var sibling = new HookProbe().Key("sibling");
        sibling.LoadNodeNumber(firstRoot.Id, 2);
        var secondRoot = new HookProbe();
        secondRoot.LoadNodeNumber("invalidation-roots", 2);

        var parentFirst = new ComponentInvalidationQueue();
        parentFirst.Enqueue(firstRoot);
        parentFirst.Enqueue(firstChild);
        parentFirst.Enqueue(firstRoot);
        var parentFirstResult = parentFirst.DrainCoveredByParents();
        AssertEqual(1, parentFirstResult.Count, "A parent enqueued first should cover its child and ignore duplicate enqueue.");
        AssertEqual(firstRoot.Id, parentFirstResult[0].ComponentId, "The parent should be retained regardless of enqueue order.");
        AssertEqual(false, parentFirst.HasPending, "Draining should clear queue membership state.");

        var independent = new ComponentInvalidationQueue();
        independent.Enqueue(firstChild);
        independent.Enqueue(sibling);
        independent.Enqueue(secondRoot);
        var independentResult = independent.DrainCoveredByParents();
        AssertEqual(3, independentResult.Count, "Siblings and separate roots must remain independent invalidations.");

        Component.DisposeHookState(firstRoot.Id);
        Component.DisposeHookState(secondRoot.Id);
    }

    private static void RuntimeAncestryRegistryReleasesDisposedSubtrees()
    {
        var baseline = RuntimeTreeIdentity.RegisteredNodeCount;
        var root = Component.Div(new HookProbe().Key("registry-child"));
        root.LoadNodeNumber("registry-test", 1);
        ElementTree<IElement, AnimationValue>.AssignDescendantIds(root.Id, root);

        AssertEqual(true, RuntimeTreeIdentity.RegisteredNodeCount > baseline, "Building a subtree should register runtime ancestry nodes.");
        Component.DisposeHookState(root.Id);
        AssertEqual(baseline, RuntimeTreeIdentity.RegisteredNodeCount, "Disposing a subtree should release all runtime ancestry nodes.");
    }

    private static void ComponentCachesAndRefreshesItsRuntimeNode()
    {
        var component = new HookProbe().Key("runtime-node-cache");
        component.LoadNodeNumber("runtime-node-cache-root", 1);
        var initialNode = component.RuntimeNode;

        component.LoadNodeNumber("runtime-node-cache-root", 1);
        AssertEqual(true, ReferenceEquals(initialNode, component.RuntimeNode), "A stable logical component should retain its cached runtime node.");

        var registeredBeforeDispose = RuntimeTreeIdentity.RegisteredNodeCount;
        Component.DisposeHookState(component.Id);
        AssertEqual(registeredBeforeDispose - 1, RuntimeTreeIdentity.RegisteredNodeCount, "Disposing a component should detach its cached runtime node.");

        component.ResetStateIndexForRender();
        var refreshedNode = component.RuntimeNode;
        AssertEqual(false, ReferenceEquals(initialNode, refreshedNode), "A disposed component object must not reuse its detached runtime node.");
        Component.DisposeHookState(component.Id);
    }

    private static void DiagnosticsPreserveHookSummaryWhenEnabled()
    {
        const string rootId = "diagnostics-hook-root";
        var component = new HookProbe();
        component.LoadNodeNumber(rootId, 1);
        var entry = Parent().WithIdentity(rootId, null).WithComponentId(component.Id);

        NuriDiagnostics.Enable();
        NuriDiagnostics.RegisterRoot(rootId, "Test", () => entry);
        try
        {
            component.UseNumberRef();
            component.CompleteHooks();

            var hook = NuriDiagnostics.GetSnapshot()
                .Roots.Single(root => root.RootId == rootId)
                .RootComponent!.Hooks.Single();
            AssertEqual(HookKind.Ref, hook.Kind, "Diagnostics should preserve the hook kind.");
            AssertEqual(nameof(Int32), hook.DisplayType, "Diagnostics should preserve the hook display type.");
            AssertEqual("1", hook.Summary, "Diagnostics should preserve the hook value summary.");
        }
        finally
        {
            Component.DisposeHookState(component.Id);
            NuriDiagnostics.UnregisterRoot(rootId);
            NuriDiagnostics.Disable();
        }
    }

    private static void DuplicateComponentKeysUseIndependentHookIdentity()
    {
        var log = new List<string>();
        var first = new LifecycleProbe("first", log).Key("duplicate");
        var second = new LifecycleProbe("second", log).Key("duplicate");
        var parent = Component.Div(first, second);

        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        ElementTree<IElement, AnimationValue>.AssignDescendantIds("duplicate-parent", parent);

        AssertNotEqual(first.Id, second.Id, "Duplicate component keys must not share hook identity.");
        AssertEqual("duplicate-parent_1", first.Id, "The first duplicate key should fall back to its position.");
        AssertEqual("duplicate-parent_2", second.Id, "The second duplicate key should fall back to its position.");
        AssertEqual(true, NuriDiagnostics.GetSnapshot().RecentLogs.Any(logEntry => logEntry.Kind == RuntimeLogKind.DuplicateKey), "Duplicate keys should emit a diagnostics entry.");

        first.RegisterMountEffect();
        second.RegisterMountEffect();
        first.FlushEffects();
        Component.DisposeHookState("duplicate-parent");

        AssertEqual(true, log.Contains("mount:first"), "The first duplicate component should mount independently.");
        AssertEqual(true, log.Contains("mount:second"), "The second duplicate component should mount independently.");
        AssertEqual(true, log.Contains("cleanup:first"), "The first duplicate component should clean up independently.");
        AssertEqual(true, log.Contains("cleanup:second"), "The second duplicate component should clean up independently.");
        NuriDiagnostics.Disable();
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
            .Opacity(0.5)
            .Rotate(5)
            .Translate(12, -4)
            .Scale(1.1, 0.9)
            .Transition(200);

        AssertEqual(true, element.Animations.ContainsKey("Background"), "Transition should apply to Background.");
        AssertEqual(true, element.Animations.ContainsKey("Foreground"), "Transition should apply to Foreground.");
        AssertEqual(true, element.Animations.ContainsKey("Opacity"), "Transition should apply to Opacity.");
        AssertEqual(true, element.Animations.ContainsKey("Rotate"), "Transition should apply to Rotate.");
        AssertEqual(true, element.Animations.ContainsKey("TranslateX"), "Transition should apply to TranslateX.");
        AssertEqual(true, element.Animations.ContainsKey("TranslateY"), "Transition should apply to TranslateY.");
        AssertEqual(true, element.Animations.ContainsKey("ScaleX"), "Transition should apply to ScaleX.");
        AssertEqual(true, element.Animations.ContainsKey("ScaleY"), "Transition should apply to ScaleY.");
        AssertEqual(false, element.Animations.ContainsKey("FontSize"), "Transition should skip unsupported FontSize animations.");
        AssertEqual(false, element.Animations.ContainsKey("ColumnDefinitions"), "Transition should skip grid column definitions.");

        var fontColor = element.Animations["Foreground"];
        var background = element.Animations["Background"];
        var opacity = element.Animations["Opacity"];
        var rotate = element.Animations["Rotate"];
        AssertEqual("Foreground", fontColor.PropertyName, "Foreground animation should target Foreground.");
        AssertEqual(element.Properties["Foreground"], fontColor.To, "Foreground animation should use the configured value.");
        AssertEqual(0.5, opacity.To, "Opacity animation should use the configured value.");
        AssertEqual(5d, rotate.To, "Rotate animation should use the configured value.");
        AssertEqual(12d, element.Animations["TranslateX"].To, "TranslateX animation should use the configured value.");
        AssertEqual(-4d, element.Animations["TranslateY"].To, "TranslateY animation should use the configured value.");
        AssertEqual(1.1d, element.Animations["ScaleX"].To, "ScaleX animation should use the configured value.");
        AssertEqual(0.9d, element.Animations["ScaleY"].To, "ScaleY animation should use the configured value.");
        AssertEqual(TimeSpan.FromMilliseconds(200), background.Duration, "Transition should use the configured duration.");
    }

    private static void LayoutDistributionDslUsesNeutralProperties()
    {
        var element = Component.Div(DivTypes.Row)
            .Spacing(12)
            .JustifyContent(ContentDistribution.SpaceEvenly);
        var growingChild = Component.Div().Grow(2);

        AssertEqual(12d, element.Properties[PropertyKeys.Spacing], "Spacing should retain its neutral scalar value.");
        AssertEqual(ContentDistribution.SpaceEvenly, element.Properties[PropertyKeys.JustifyContent], "JustifyContent should retain its neutral distribution value.");
        AssertEqual(2d, growingChild.Properties[PropertyKeys.Grow], "Grow should retain its neutral proportional weight.");

        AssertEqual(
            ContentDistribution.SpaceBetween,
            Component.Div(DivTypes.Row).SpaceBetween().Properties[PropertyKeys.JustifyContent],
            "SpaceBetween should configure the matching content distribution.");
        AssertEqual(
            ContentDistribution.SpaceAround,
            Component.Div(DivTypes.Row).SpaceAround().Properties[PropertyKeys.JustifyContent],
            "SpaceAround should configure the matching content distribution.");
        AssertEqual(
            ContentDistribution.SpaceEvenly,
            Component.Div(DivTypes.Row).SpaceEvenly().Properties[PropertyKeys.JustifyContent],
            "SpaceEvenly should configure the matching content distribution.");

        AssertThrows<ArgumentOutOfRangeException>(
            () => Component.Div().Spacing(-1),
            "Negative spacing should fail before reaching a renderer.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => Component.Div().Grow(-1),
            "Negative Grow weights should fail before reaching a renderer.");
        var grid = Component.Grid()
            .Spacing(8)
            .RowSpacing(6);
        AssertEqual(6d, grid.Properties[PropertyKeys.RowSpacing], "RowSpacing should override uniform Grid spacing for rows.");
        AssertEqual(8d, grid.Properties[PropertyKeys.ColumnSpacing], "Uniform Grid spacing should configure column spacing.");

        AssertThrows<InvalidOperationException>(
            () => Component.Div().RowSpacing(8),
            "RowSpacing should remain specific to Grid layouts.");
        AssertThrows<InvalidOperationException>(
            () => Component.Grid().SpaceEvenly(),
            "Grid should continue using row and column definitions instead of main-axis content distribution.");
        AssertThrows<InvalidOperationException>(
            () => Component.Div(DivTypes.Scroll).SpaceBetween(),
            "Scroll layouts should reject main-axis content distribution.");
        AssertThrows<InvalidOperationException>(
            () => Component.Div(DivTypes.Scroll).Spacing(8),
            "Scroll layouts should delegate spacing to their single content child.");
        AssertThrows<InvalidOperationException>(
            () => Component.Div(DivTypes.Scroll, Component.Text("first"), Component.Text("second")),
            "Scroll layouts should reject multiple direct children.");
        AssertEqual(
            1,
            Component.Div(DivTypes.Scroll, Component.Div(Component.Text("content"))).Children.Count,
            "Scroll layouts should accept one content child.");

        var first = Component.Text("first");
        var second = Component.Text("second");
        var third = Component.Text("third");
        var fourth = Component.Text("fourth");
        var autoGrid = Component.Grid(first, second, third, fourth)
            .Columns("*,*,*")
            .AutoFlow();
        var autoRows = (List<LengthValue>)autoGrid.Properties["RowDefinitions"];

        AssertEqual(2, autoRows.Count, "AutoFlow should add enough Auto rows for every child.");
        AssertEqual(LengthUnit.Auto, autoRows[0].Unit, "AutoFlow should use Auto for generated rows.");
        AssertEqual(0, first.Properties["Grid.Row"], "AutoFlow should start in the first row.");
        AssertEqual(0, first.Properties["Grid.Column"], "AutoFlow should start in the first column.");
        AssertEqual(0, third.Properties["Grid.Row"], "AutoFlow should fill the first row before advancing.");
        AssertEqual(2, third.Properties["Grid.Column"], "AutoFlow should use every configured column.");
        AssertEqual(1, fourth.Properties["Grid.Row"], "AutoFlow should continue on the next row.");
        AssertEqual(0, fourth.Properties["Grid.Column"], "AutoFlow should restart from the first column on a new row.");

        AssertThrows<InvalidOperationException>(
            () => Component.Grid(Component.Text("missing columns")).AutoFlow(),
            "AutoFlow should require an explicit column count.");
        AssertThrows<InvalidOperationException>(
            () => Component.Grid(Component.Text("explicit").Row(0)).Columns("*").AutoFlow(),
            "AutoFlow should reject mixed explicit placement in its first version.");

        var sizedRows = Component.Grid(
                Component.Text("a"),
                Component.Text("b"),
                Component.Text("c"),
                Component.Text("d"))
            .Columns("*,*")
            .Rows("*,2*")
            .AutoFlow();
        var retainedRows = (List<LengthValue>)sizedRows.Properties["RowDefinitions"];
        AssertEqual(2, retainedRows.Count, "AutoFlow should retain explicitly configured rows.");
        AssertEqual(1d, retainedRows[0].Value, "AutoFlow should retain the first explicit row size.");
        AssertEqual(2d, retainedRows[1].Value, "AutoFlow should retain the second explicit row size.");

        AssertThrows<InvalidOperationException>(
            () => Component.Grid(
                    Component.Text("a"),
                    Component.Text("b"),
                    Component.Text("c"),
                    Component.Text("d"),
                    Component.Text("overflow"))
                .Columns("*,*")
                .Rows("*,*")
                .AutoFlow(),
            "AutoFlow should reject children that exceed an explicitly sized Grid.");
    }

    private static void GridLengthDslTreatsNumbersAsPixels()
    {
        var element = Component.Grid()
            .Rows(100, Component.Auto, Component.Star)
            .Columns(12.5, Component.Stars(2), Component.Pixels(42));

        var rows = (List<LengthValue>)element.Properties["RowDefinitions"];
        var columns = (List<LengthValue>)element.Properties["ColumnDefinitions"];

        AssertEqual(LengthUnit.Pixel, rows[0].Unit, "A numeric row should use pixels.");
        AssertEqual(100d, rows[0].Value, "A numeric row should retain its value.");
        AssertEqual(LengthUnit.Auto, rows[1].Unit, "Auto should remain available beside numeric rows.");
        AssertEqual(LengthUnit.Star, rows[2].Unit, "Star should remain available beside numeric rows.");
        AssertEqual(12.5d, columns[0].Value, "A fractional numeric column should retain its value.");
        AssertEqual(2d, columns[1].Value, "Weighted stars should remain available beside numeric columns.");
        AssertEqual(42d, columns[2].Value, "The explicit Pixels form should remain compatible.");
    }

    private static void GridLengthDslParsesStringDefinitions()
    {
        var fluent = Component.Grid()
            .Rows(" auto , * , 100 , 2.5*")
            .Columns("240,3*");

        var rows = (List<LengthValue>)fluent.Properties["RowDefinitions"];
        var columns = (List<LengthValue>)fluent.Properties["ColumnDefinitions"];

        AssertEqual(4, rows.Count, "String rows should parse every comma-separated token.");
        AssertEqual(LengthUnit.Auto, rows[0].Unit, "Auto parsing should be case-insensitive.");
        AssertEqual(LengthUnit.Star, rows[1].Unit, "An unweighted star should parse as one star.");
        AssertEqual(1d, rows[1].Value, "An unweighted star should have weight one.");
        AssertEqual(LengthUnit.Pixel, rows[2].Unit, "A numeric string token should use pixels.");
        AssertEqual(100d, rows[2].Value, "A numeric string token should retain its value.");
        AssertEqual(2.5d, rows[3].Value, "A weighted star string should retain its weight.");
        AssertEqual(240d, columns[0].Value, "String columns should parse pixel values.");
        AssertEqual(3d, columns[1].Value, "String columns should parse weighted stars.");

        var factory = Component.Grid(
            Component.Rows("Auto,100"),
            Component.Columns("*,2*"));
        var factoryRows = (List<LengthValue>)factory.Properties["RowDefinitions"];
        var factoryColumns = (List<LengthValue>)factory.Properties["ColumnDefinitions"];

        AssertEqual(LengthUnit.Auto, factoryRows[0].Unit, "The Rows factory should use the shared parser.");
        AssertEqual(100d, factoryRows[1].Value, "The Rows factory should parse numeric pixels.");
        AssertEqual(1d, factoryColumns[0].Value, "The Columns factory should parse a default star.");
        AssertEqual(2d, factoryColumns[1].Value, "The Columns factory should parse a weighted star.");
    }

    private static void GridLengthDslRejectsInvalidStringDefinitions()
    {
        AssertThrows<ArgumentNullException>(
            () => Component.Grid().Rows((string)null!),
            "Null grid definitions should fail immediately.");
        AssertThrows<FormatException>(
            () => Component.Grid().Rows(" "),
            "Empty grid definitions should fail immediately.");

        var emptyToken = AssertThrows<FormatException>(
            () => Component.Grid().Rows("Auto,,*"),
            "Empty grid tokens should fail immediately.");
        AssertEqual(true, emptyToken.Message.Contains("index 1"), "An invalid token error should identify its index.");

        AssertThrows<FormatException>(
            () => Component.Grid().Columns("wide,*"),
            "Unknown grid tokens should fail immediately.");
        AssertThrows<FormatException>(
            () => Component.Grid().Columns("Auto,two*"),
            "Malformed star weights should fail immediately.");
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

    private static TException AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"{message} Expected exception: {typeof(TException).Name}");
    }

    private sealed class HookProbe : Component
    {
        public int MemoFactoryCallCount { get; private set; }

        public int StateChangedCount { get; private set; }

        public List<string> EffectLog { get; } = new List<string>();

        public (int state, Action<int> dispatch) UseCounter()
        {
            return useReducer<int, int>((state, action) => state + action, 0);
        }

        public (int state, Action<Func<int, int>> setter) UseNumberState(int initialValue)
        {
            return useState(initialValue);
        }

        public Action<Func<int, int>>[] UseNumberStates(int count)
        {
            var setters = new Action<Func<int, int>>[count];
            for (var i = 0; i < count; i++)
                setters[i] = useState(i).setState;
            return setters;
        }

        protected override void OnStateChanged()
        {
            StateChangedCount++;
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

    private static void DiagnosticsTrackPatchBatchesAndVirtualizedRows()
    {
        const string rootId = "diagnostics-patch-root";
        const string hostId = "diagnostics-virtualized-host";
        var entry = Parent().WithIdentity(rootId, null);

        NuriDiagnostics.Enable();
        NuriDiagnostics.RegisterRoot(rootId, "Test", () => entry);
        try
        {
            NuriDiagnostics.RecordPatchBatch(rootId, new PatchOperation[]
            {
                new UpdatePropertyPatch(entry, "Text", "updated"),
                new UpdatePropertyPatch(entry, "Opacity", 0.5),
                new RemovePropertyPatch(entry, "Margin")
            });
            NuriDiagnostics.RecordVirtualizedItems(hostId, 10_000, 19);

            var snapshot = NuriDiagnostics.GetSnapshot();
            var root = snapshot.Roots.Single(item => item.RootId == rootId);
            var virtualized = snapshot.VirtualizedItems.Single(item => item.HostId == hostId);

            AssertEqual(1L, root.PatchBatchCount, "Diagnostics should count applied patch batches.");
            AssertEqual(3L, root.PatchCount, "Diagnostics should accumulate applied patches.");
            AssertEqual(3, root.LastPatchCount, "Diagnostics should retain the last patch batch size.");
            AssertEqual(2, root.LastPatchCounts[PatchOperationType.UpdateProperty], "Diagnostics should group the last batch by patch type.");
            AssertEqual(1, root.LastPatchCounts[PatchOperationType.RemoveProperty], "Diagnostics should retain every patch type in the last batch.");
            AssertEqual(10_000, virtualized.ItemCount, "Diagnostics should report the virtual item count.");
            AssertEqual(19, virtualized.RealizedCount, "Diagnostics should report the realized native row count.");

            NuriDiagnostics.Disable();
            NuriDiagnostics.RemoveVirtualizedItems(hostId);
            AssertEqual(false, NuriDiagnostics.GetSnapshot().VirtualizedItems.Any(item => item.HostId == hostId), "Virtualized diagnostics cleanup should remain deterministic after diagnostics are disabled.");
            NuriDiagnostics.Enable();
        }
        finally
        {
            NuriDiagnostics.RemoveVirtualizedItems(hostId);
            NuriDiagnostics.UnregisterRoot(rootId);
            NuriDiagnostics.Disable();
        }
    }

    private static void DiagnosticsLogOnceDeduplicatesUntilLogsAreCleared()
    {
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        try
        {
            NuriDiagnostics.LogOnce(RuntimeLogKind.UnsupportedProperty, "test:property", null, "component", "first");
            NuriDiagnostics.LogOnce(RuntimeLogKind.UnsupportedProperty, "test:property", null, "component", "duplicate");

            var logs = NuriDiagnostics.GetSnapshot().RecentLogs;
            AssertEqual(1, logs.Count, "LogOnce should retain only the first entry for a dedupe key.");
            AssertEqual("first", logs[0].Message, "LogOnce should preserve the first diagnostic message.");

            NuriDiagnostics.ClearLogs();
            NuriDiagnostics.LogOnce(RuntimeLogKind.UnsupportedProperty, "test:property", null, "component", "after-clear");
            logs = NuriDiagnostics.GetSnapshot().RecentLogs;
            AssertEqual(1, logs.Count, "Clearing logs should reset LogOnce deduplication.");
            AssertEqual("after-clear", logs[0].Message, "LogOnce should emit the diagnostic again after logs are cleared.");
        }
        finally
        {
            NuriDiagnostics.ClearLogs();
            NuriDiagnostics.Disable();
        }
    }

    private sealed record VirtualizedProbe(string Id, int Version);

    private sealed class CapturingRenderer : IRendererAdapter<object>
    {
        public IReadOnlyList<PatchOperation> Operations { get; private set; } = Array.Empty<PatchOperation>();

        public object Build(VirtualEntry entry) => new object();

        public void ApplyDiff(object root, IReadOnlyList<PatchOperation> operations)
        {
            Operations = operations;
        }
    }

    private sealed class CapturingHost : IHostAdapter<object>
    {
        public void SetContent(object root)
        {
        }
    }

    private sealed class VirtualizedComponentProbe : Component
    {
        public override IElement Render() => Text("component");
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
