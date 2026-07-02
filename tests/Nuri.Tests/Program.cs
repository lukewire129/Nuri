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
        public (int state, Action<int> dispatch) UseCounter()
        {
            return useReducer<int, int>((state, action) => state + action, 0);
        }

        public Ref<int> UseNumberRef()
        {
            return useRef(1);
        }

        public override IElement Render()
        {
            return Div();
        }
    }
}
