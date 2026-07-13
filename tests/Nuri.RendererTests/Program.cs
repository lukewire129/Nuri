using System.Collections.Generic;
using System.Linq;
using Avalonia.Animation;
using Nuri.Platform.Abstractions;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using AvaloniaControl = Avalonia.Controls.Control;
using AvaloniaPanel = Avalonia.Controls.Panel;
using AvaloniaTextBlock = Avalonia.Controls.TextBlock;
using AvaloniaVisual = Avalonia.Visual;
using WpfFrameworkElement = System.Windows.FrameworkElement;
using WpfBorder = System.Windows.Controls.Border;
using WpfDecorator = System.Windows.Controls.Decorator;
using WpfPanel = System.Windows.Controls.Panel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfUIElement = System.Windows.UIElement;
using WpfColor = System.Windows.Media.Color;
using WpfRotateTransform = System.Windows.Media.RotateTransform;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfThickness = System.Windows.Thickness;

namespace Nuri.RendererTests;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        RunSuite(() => new WpfDriver());
        WpfTransitionsReplaceAndClearNativeAnimations(new WpfDriver());
        RunSuite(() => new AvaloniaDriver());
        Console.WriteLine("Nuri.RendererTests passed.");
    }

    private static void WpfTransitionsReplaceAndClearNativeAnimations(WpfDriver driver)
    {
        var component = new WpfTransitionComponent();
        using var root = driver.Initialize(component);

        component.Margin = 18;
        component.Background = "#2563eb";
        component.Foreground = "#fef3c7";
        component.Rotation = 5;
        root.Rebuild();

        var targets = driver.RootChildren.Cast<WpfFrameworkElement>().ToArray();
        AssertWpfTransitions(targets, true);

        component.Margin = 12;
        component.Background = "#7c3aed";
        component.Foreground = "#f8fafc";
        component.Rotation = -6;
        root.Rebuild();

        var updatedTargets = driver.RootChildren.Cast<WpfFrameworkElement>().ToArray();
        for (var index = 0; index < targets.Length; index++)
            AssertSame(targets[index], updatedTargets[index], "WPF: replacing a transition should preserve the native target.");

        AssertWpfTransitions(updatedTargets, true);
        AssertEqual(new WpfThickness(12), (WpfThickness)updatedTargets[0].GetAnimationBaseValue(WpfFrameworkElement.MarginProperty), "WPF: Margin should retain the latest base value.");

        var backgroundBrush = (WpfSolidColorBrush)((WpfBorder)updatedTargets[1]).Background;
        AssertEqual(WpfColor.FromRgb(124, 58, 237), (WpfColor)backgroundBrush.GetAnimationBaseValue(WpfSolidColorBrush.ColorProperty), "WPF: Background should retain the latest base color.");

        var foregroundBrush = (WpfSolidColorBrush)((WpfTextBlock)updatedTargets[2]).Foreground;
        AssertEqual(WpfColor.FromRgb(248, 250, 252), (WpfColor)foregroundBrush.GetAnimationBaseValue(WpfSolidColorBrush.ColorProperty), "WPF: Foreground should retain the latest base color.");

        var rotateTransform = (WpfRotateTransform)updatedTargets[3].RenderTransform;
        AssertEqual(-6d, (double)rotateTransform.GetAnimationBaseValue(WpfRotateTransform.AngleProperty), "WPF: Rotate should retain the latest base angle.");

        component.Animate = false;
        root.Rebuild();
        AssertWpfTransitions(updatedTargets, false);
    }

    private static void AssertWpfTransitions(IReadOnlyList<WpfFrameworkElement> targets, bool expected)
    {
        AssertEqual(4, targets.Count, "WPF: the animation probe should retain four targets.");
        AssertEqual(expected, targets[0].HasAnimatedProperties, "WPF: Margin animation state should match the virtual transition.");

        var backgroundBrush = (WpfSolidColorBrush)((WpfBorder)targets[1]).Background;
        AssertEqual(expected, backgroundBrush.HasAnimatedProperties, "WPF: Background animation state should match the virtual transition.");

        var foregroundBrush = (WpfSolidColorBrush)((WpfTextBlock)targets[2]).Foreground;
        AssertEqual(expected, foregroundBrush.HasAnimatedProperties, "WPF: Foreground animation state should match the virtual transition.");

        var rotateTransform = (WpfRotateTransform)targets[3].RenderTransform;
        AssertEqual(expected, rotateTransform.HasAnimatedProperties, "WPF: Rotate animation state should match the virtual transition.");
    }

    private static void RunSuite(Func<RendererDriver> createDriver)
    {
        EffectsRunAfterNativeCommit(createDriver());
        SubtreeEffectsRunAfterNativeCommit(createDriver());
        RemovedSubtreesCleanUpAfterNativeRemoval(createDriver());
        KeyReplacementCleansUpBeforeMount(createDriver());
        KeyedMovesPreserveNativeControlsAndEffects(createDriver());
        OpacityTransitionsAreMaterializedReplacedAndRemoved(createDriver());
        RootDisposeCleansDescendantsExactlyOnce(createDriver());
    }

    private static void OpacityTransitionsAreMaterializedReplacedAndRemoved(RendererDriver driver)
    {
        var component = new OpacityTransitionComponent();
        using var root = driver.Initialize(component);

        component.Opacity = 0.8;
        root.Rebuild();
        var target = driver.RootChildren.Single();
        AssertEqual(1, driver.GetOpacityTransitionCount(target), $"{driver.Name}: an opacity update should materialize one native transition.");

        component.Opacity = 0.4;
        root.Rebuild();
        AssertEqual(1, driver.GetOpacityTransitionCount(target), $"{driver.Name}: replacing an active opacity transition should not duplicate native transitions.");

        component.Animate = false;
        root.Rebuild();
        AssertEqual(0, driver.GetOpacityTransitionCount(target), $"{driver.Name}: removing the transition description should remove the native transition.");
    }

    private static void EffectsRunAfterNativeCommit(RendererDriver driver)
    {
        var log = new List<string>();
        var component = new CommitProbeComponent(() => driver.RootText, log);
        using var root = driver.Initialize(component);

        AssertSequence(
            new[] { "effect:initial:initial" },
            log,
            $"{driver.Name}: the initial effect should observe attached native content.");

        component.Value = "updated";
        root.Rebuild();

        AssertSequence(
            new[] { "effect:initial:initial", "cleanup:initial", "effect:updated:updated" },
            log,
            $"{driver.Name}: an updated effect should observe committed native content.");
    }

    private static void RemovedSubtreesCleanUpAfterNativeRemoval(RendererDriver driver)
    {
        var log = new List<string>();
        var component = new ToggleChildComponent(log);
        using var root = driver.Initialize(component);

        AssertEqual(1, driver.RootChildren.Count, $"{driver.Name}: the child should initially be materialized.");
        component.ShowChild = false;
        root.Rebuild();

        AssertEqual(0, driver.RootChildren.Count, $"{driver.Name}: the removed child should leave the native tree.");
        AssertEqual(1, log.Count(entry => entry == "cleanup:child"), $"{driver.Name}: subtree removal should clean up once.");
    }

    private static void SubtreeEffectsRunAfterNativeCommit(RendererDriver driver)
    {
        var log = new List<string>();
        var component = new CommitProbeContainer(() => driver.RootText, log);
        using var root = driver.Initialize(component);

        component.Child.Value = "updated";
        root.ScheduleComponentRebuild(component.Child);

        AssertSequence(
            new[] { "effect:initial:initial", "cleanup:initial", "effect:updated:updated" },
            log,
            $"{driver.Name}: a dirty child effect should observe its committed native subtree.");
    }

    private static void KeyReplacementCleansUpBeforeMount(RendererDriver driver)
    {
        var log = new List<string>();
        var component = new KeyReplacementComponent(log);
        using var root = driver.Initialize(component);
        var oldChild = driver.RootChildren.Single();

        component.CurrentKey = "b";
        root.Rebuild();

        var newChild = driver.RootChildren.Single();
        AssertNotSame(oldChild, newChild, $"{driver.Name}: a key replacement should materialize a new native control.");
        AssertEqual("b", driver.GetText(newChild), $"{driver.Name}: the replacement should contain the new value.");
        AssertSequence(
            new[] { "mount:a", "cleanup:a", "mount:b" },
            log,
            $"{driver.Name}: key replacement should clean up the old component before mounting the new one.");
    }

    private static void KeyedMovesPreserveNativeControlsAndEffects(RendererDriver driver)
    {
        var log = new List<string>();
        var component = new KeyedMoveComponent(log);
        using var root = driver.Initialize(component);
        var original = driver.RootChildren.ToDictionary(driver.GetText, child => child);
        var mountLog = log.ToArray();

        component.Order = new[] { "c", "a", "b" };
        root.Rebuild();
        AssertChildOrderAndIdentity(driver, component.Order, original);
        AssertSequence(mountLog, log, $"{driver.Name}: moving keyed children should not rerun effects or cleanup.");

        component.Order = new[] { "b", "c", "a" };
        root.Rebuild();
        AssertChildOrderAndIdentity(driver, component.Order, original);
        AssertSequence(mountLog, log, $"{driver.Name}: repeated keyed moves should preserve effect lifetimes.");
    }

    private static void RootDisposeCleansDescendantsExactlyOnce(RendererDriver driver)
    {
        var log = new List<string>();
        var component = new DisposeTreeComponent(log);
        var root = driver.Initialize(component);

        root.Dispose();
        root.Dispose();

        AssertEqual(1, log.Count(entry => entry == "cleanup:first"), $"{driver.Name}: root disposal should clean the first descendant once.");
        AssertEqual(1, log.Count(entry => entry == "cleanup:second"), $"{driver.Name}: root disposal should clean the second descendant once.");
    }

    private static void AssertChildOrderAndIdentity(
        RendererDriver driver,
        IReadOnlyList<string> expectedOrder,
        IReadOnlyDictionary<string, object> original)
    {
        var children = driver.RootChildren;
        AssertEqual(expectedOrder.Count, children.Count, $"{driver.Name}: keyed moves should retain the child count.");

        for (var index = 0; index < expectedOrder.Count; index++)
        {
            var key = expectedOrder[index];
            AssertEqual(key, driver.GetText(children[index]), $"{driver.Name}: keyed children should move to the requested order.");
            AssertSame(original[key], children[index], $"{driver.Name}: keyed moves should retain native control identity.");
        }
    }

    private static void AssertSequence(IEnumerable<string> expected, IEnumerable<string> actual, string message)
    {
        var expectedValues = expected.ToArray();
        var actualValues = actual.ToArray();
        if (!expectedValues.SequenceEqual(actualValues))
            throw new InvalidOperationException($"{message} Expected: [{string.Join(", ", expectedValues)}]; Actual: [{string.Join(", ", actualValues)}]");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message} Expected: {expected}; Actual: {actual}");
    }

    private static void AssertSame(object expected, object actual, string message)
    {
        if (!ReferenceEquals(expected, actual))
            throw new InvalidOperationException(message);
    }

    private static void AssertNotSame(object expected, object actual, string message)
    {
        if (ReferenceEquals(expected, actual))
            throw new InvalidOperationException(message);
    }

    private abstract class RendererDriver
    {
        public abstract string Name { get; }

        public abstract string? RootText { get; }

        public abstract IReadOnlyList<object> RootChildren { get; }

        public abstract RendererRoot Initialize(IElement rootElement);

        public abstract string GetText(object control);

        public abstract int GetOpacityTransitionCount(object control);
    }

    private sealed class WpfDriver : RendererDriver
    {
        private readonly WpfTestHost _host = new WpfTestHost();

        public override string Name => "WPF";

        public override string? RootText => _host.Content == null ? null : FindText(_host.Content);

        public override IReadOnlyList<object> RootChildren
            => GetRootPanel(_host.Content) is WpfPanel panel ? panel.Children.Cast<object>().ToArray() : Array.Empty<object>();

        public override RendererRoot Initialize(IElement rootElement)
        {
            var root = WPF.ApplicationRoot.Initialize(rootElement, _host, () => null);
            return new RendererRoot(root.Rebuild, root.ScheduleComponentRebuild, root.Dispose);
        }

        public override string GetText(object control)
        {
            return control is WpfFrameworkElement element ? FindText(element) ?? string.Empty : string.Empty;
        }

        public override int GetOpacityTransitionCount(object control)
        {
            return control is WpfUIElement element && element.HasAnimatedProperties ? 1 : 0;
        }

        private static string? FindText(WpfFrameworkElement element)
        {
            if (element is WpfTextBlock textBlock)
                return textBlock.Text;

            if (element is WpfPanel panel)
            {
                foreach (var child in panel.Children.OfType<WpfFrameworkElement>())
                {
                    var text = FindText(child);
                    if (text != null)
                        return text;
                }
            }

            if (element is WpfDecorator decorator && decorator.Child is WpfFrameworkElement childElement)
                return FindText(childElement);

            return null;
        }

        private static WpfPanel? GetRootPanel(WpfFrameworkElement? element)
        {
            if (element is WpfPanel panel)
                return panel;

            return element is WpfDecorator decorator ? decorator.Child as WpfPanel : null;
        }
    }

    private sealed class AvaloniaDriver : RendererDriver
    {
        private readonly AvaloniaTestHost _host = new AvaloniaTestHost();

        public override string Name => "Avalonia";

        public override string? RootText => _host.Content == null ? null : FindText(_host.Content);

        public override IReadOnlyList<object> RootChildren
            => _host.Content is AvaloniaPanel panel ? panel.Children.Cast<object>().ToArray() : Array.Empty<object>();

        public override RendererRoot Initialize(IElement rootElement)
        {
            var root = Avalonia.AvaloniaApplicationRoot.Initialize(rootElement, _host, new ImmediateScheduler());
            return new RendererRoot(root.Rebuild, root.ScheduleComponentRebuild, root.Dispose);
        }

        public override string GetText(object control)
        {
            return control is AvaloniaControl element ? FindText(element) ?? string.Empty : string.Empty;
        }

        public override int GetOpacityTransitionCount(object control)
        {
            return control is AvaloniaControl element
                ? element.Transitions?.OfType<TransitionBase>().Count(transition => transition.Property == AvaloniaVisual.OpacityProperty) ?? 0
                : 0;
        }

        private static string? FindText(AvaloniaControl control)
        {
            if (control is AvaloniaTextBlock textBlock)
                return textBlock.Text;

            if (control is AvaloniaPanel panel)
            {
                foreach (var child in panel.Children)
                {
                    var text = FindText(child);
                    if (text != null)
                        return text;
                }
            }

            return null;
        }
    }

    private sealed class RendererRoot : IDisposable
    {
        private readonly Action _rebuild;
        private readonly Action<Component> _scheduleComponentRebuild;
        private readonly Action _dispose;

        public RendererRoot(Action rebuild, Action<Component> scheduleComponentRebuild, Action dispose)
        {
            _rebuild = rebuild;
            _scheduleComponentRebuild = scheduleComponentRebuild;
            _dispose = dispose;
        }

        public void Rebuild() => _rebuild();

        public void ScheduleComponentRebuild(Component component) => _scheduleComponentRebuild(component);

        public void Dispose() => _dispose();
    }

    private sealed class WpfTestHost : IHostAdapter<WpfFrameworkElement>
    {
        public WpfFrameworkElement? Content { get; private set; }

        public void SetContent(WpfFrameworkElement root)
        {
            Content = root;
        }
    }

    private sealed class AvaloniaTestHost : IHostAdapter<AvaloniaControl>
    {
        public AvaloniaControl? Content { get; private set; }

        public void SetContent(AvaloniaControl root)
        {
            Content = root;
        }
    }

    private sealed class ImmediateScheduler : IUiScheduler
    {
        public void Schedule(Action action) => action();
    }

    private sealed class WpfTransitionComponent : Component
    {
        public double Margin { get; set; } = 4;

        public string Background { get; set; } = "#111827";

        public string Foreground { get; set; } = "#94a3b8";

        public double Rotation { get; set; } = -2;

        public bool Animate { get; set; } = true;

        public override IElement Render()
        {
            var margin = Div().Margin(Margin);
            var background = Div().Background(Background);
            var foreground = Text("foreground").FontColor(Foreground);
            var rotate = Text("rotate").Rotate(Rotation);

            if (Animate)
            {
                margin.Transition(500, EasingValue.CubicOut);
                background.Transition(500, EasingValue.CubicOut);
                foreground.Transition(500, EasingValue.CubicOut);
                rotate.Transition(500, EasingValue.CubicOut);
            }

            return Grid(margin, background, foreground, rotate);
        }
    }

    private sealed class OpacityTransitionComponent : Component
    {
        public double Opacity { get; set; } = 0.2;

        public bool Animate { get; set; } = true;

        public override IElement Render()
        {
            var card = Text("opacity-card").Opacity(Opacity);
            if (Animate)
                card.Transition(TimeSpan.FromMilliseconds(500), EasingValue.CubicOut);

            return Grid(card);
        }
    }

    private sealed class CommitProbeComponent : Component
    {
        private readonly Func<string?> _readNativeText;
        private readonly List<string> _log;

        public CommitProbeComponent(Func<string?> readNativeText, List<string> log)
        {
            _readNativeText = readNativeText;
            _log = log;
        }

        public string Value { get; set; } = "initial";

        public override IElement Render()
        {
            var value = Value;
            useEffect(() =>
            {
                _log.Add($"effect:{value}:{_readNativeText()}");
                return () => _log.Add($"cleanup:{value}");
            }, value);

            return Text(value);
        }
    }

    private sealed class EffectChild : Component
    {
        private readonly string _name;
        private readonly List<string> _log;

        public EffectChild(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public override IElement Render()
        {
            useEffect(() =>
            {
                _log.Add($"mount:{_name}");
                return () => _log.Add($"cleanup:{_name}");
            }, []);

            return Text(_name);
        }
    }

    private sealed class CommitProbeContainer : Component
    {
        public CommitProbeContainer(Func<string?> readNativeText, List<string> log)
        {
            Child = new CommitProbeComponent(readNativeText, log).Key("commit-child");
        }

        public CommitProbeComponent Child { get; }

        public override IElement Render()
        {
            return Grid(Child);
        }
    }

    private sealed class ToggleChildComponent : Component
    {
        private readonly List<string> _log;

        public ToggleChildComponent(List<string> log)
        {
            _log = log;
        }

        public bool ShowChild { get; set; } = true;

        public override IElement Render()
        {
            return ShowChild
                ? Grid(new EffectChild("child", _log).Key("child"))
                : Grid();
        }
    }

    private sealed class KeyReplacementComponent : Component
    {
        private readonly List<string> _log;

        public KeyReplacementComponent(List<string> log)
        {
            _log = log;
        }

        public string CurrentKey { get; set; } = "a";

        public override IElement Render()
        {
            return Grid(new EffectChild(CurrentKey, _log).Key(CurrentKey));
        }
    }

    private sealed class KeyedMoveComponent : Component
    {
        private readonly List<string> _log;

        public KeyedMoveComponent(List<string> log)
        {
            _log = log;
        }

        public string[] Order { get; set; } = new[] { "a", "b", "c" };

        public override IElement Render()
        {
            return Grid(Order
                .Select(key => (IElement)new EffectChild(key, _log).Key(key))
                .ToArray());
        }
    }

    private sealed class DisposeTreeComponent : Component
    {
        private readonly List<string> _log;

        public DisposeTreeComponent(List<string> log)
        {
            _log = log;
        }

        public override IElement Render()
        {
            return Grid(
                new EffectChild("first", _log).Key("first"),
                new EffectChild("second", _log).Key("second"));
        }
    }
}
