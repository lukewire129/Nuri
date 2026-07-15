using System.Collections.Generic;
using System.Linq;
using Avalonia.Animation;
using Nuri.Constants;
using Nuri.Platform.Abstractions;
using Nuri.Runtime;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.VirtualDom;
using Nuri.WPF;
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
using WpfScaleTransform = System.Windows.Media.ScaleTransform;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfThickness = System.Windows.Thickness;
using WpfTransformGroup = System.Windows.Media.TransformGroup;
using WpfTranslateTransform = System.Windows.Media.TranslateTransform;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfListBoxItem = System.Windows.Controls.ListBoxItem;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace Nuri.RendererTests;

internal static class Program
{
    private sealed record VirtualizedRow(int Index, bool Selected);

    [STAThread]
    private static void Main()
    {
        RunSuite(() => new WpfDriver());
        WpfRepeatedEffectLifecycleRemainsStable();
        WpfDisposedRootIgnoresQueuedInvalidations();
        WpfMultipleRootsIsolateStateAndCleanupClosedWindows();
        WpfInputEventSubscriptionsRemainStable();
        WpfUnsupportedPropertyDiagnosticsAreDeduplicated();
        WpfUnsupportedEventDiagnosticsAreDeduplicated();
        WpfDiagnosticsTrackAppliedPatchBatches();
        WpfRootDisposalRemovesVirtualizedDiagnostics();
        WpfTransitionsReplaceAndClearNativeAnimations(new WpfDriver());
        WpfVirtualizedItemsStayLazyAndRecycleContainers();
        RunSuite(() => new AvaloniaDriver());
        WpfRunBootstrapsStaAndClosesEveryWindowWithTheMainWindow();
        Console.WriteLine("Nuri.RendererTests passed.");
    }

    private static void WpfRunBootstrapsStaAndClosesEveryWindowWithTheMainWindow()
    {
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        ApplicationLifetimeProbe.Reset();
        var initialRootCount = NuriDiagnostics.GetSnapshot().Roots.Count;
        Exception? threadFailure = null;
        System.Windows.Application? application = null;
        var configuredShutdownMode = System.Windows.ShutdownMode.OnLastWindowClose;
        var childWasVisible = false;
        var childClosed = false;
        var callerApartmentState = System.Threading.ApartmentState.Unknown;
        var applicationApartmentState = System.Threading.ApartmentState.Unknown;

        ApplicationLifetimeProbe.OnMainMounted = () =>
        {
            applicationApartmentState = System.Threading.Thread.CurrentThread.GetApartmentState();
            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    application = System.Windows.Application.Current
                        ?? throw new InvalidOperationException("WPF: Run should create an Application before mounting the main root.");
                    configuredShutdownMode = application.ShutdownMode;
                    var mainWindow = application.MainWindow
                        ?? throw new InvalidOperationException("WPF: Run should assign its window as Application.MainWindow.");
                    var childWindow = NuriApplication.Show<ApplicationLifetimeChildComponent>(
                        "Nuri.RendererTests.ApplicationLifetime.Child",
                        width: 1,
                        height: 1);
                    childWindow.ShowInTaskbar = false;
                    childWindow.WindowStyle = System.Windows.WindowStyle.None;
                    childWindow.Opacity = 0;
                    childWindow.Closed += (_, _) => childClosed = true;
                    childWasVisible = childWindow.IsVisible;
                    mainWindow.Close();
                }
                catch (Exception exception)
                {
                    threadFailure = exception;
                    System.Windows.Application.Current?.Shutdown(-1);
                }
            }));
        };

        var applicationThread = new System.Threading.Thread(() =>
        {
            try
            {
                callerApartmentState = System.Threading.Thread.CurrentThread.GetApartmentState();
                NuriApplication.Run<ApplicationLifetimeMainComponent>(
                    "Nuri.RendererTests.ApplicationLifetime.Main",
                    width: 1,
                    height: 1);
            }
            catch (Exception exception)
            {
                threadFailure = exception;
            }
        });
        applicationThread.Start();

        if (!applicationThread.Join(TimeSpan.FromSeconds(15)))
        {
            application?.Dispatcher.Invoke(() => application.Shutdown(-1));
            applicationThread.Join(TimeSpan.FromSeconds(5));
            throw new InvalidOperationException("WPF: closing the main window should shut down the application without waiting for child windows.");
        }

        if (threadFailure != null)
            throw new InvalidOperationException("WPF: the application-lifetime probe failed.", threadFailure);

        AssertEqual(System.Threading.ApartmentState.MTA, callerApartmentState, "WPF: the application-lifetime probe should call Run from a default MTA thread.");
        AssertEqual(System.Threading.ApartmentState.STA, applicationApartmentState, "WPF: Run should bootstrap a dedicated STA application thread when its caller is not STA.");
        AssertEqual(System.Windows.ShutdownMode.OnMainWindowClose, configuredShutdownMode, "WPF: Run should select main-window shutdown semantics.");
        AssertEqual(true, childWasVisible, "WPF: the lifetime probe should show a child window before closing the main window.");
        AssertEqual(true, childClosed, "WPF: closing the main window should close every remaining application window.");
        AssertEqual(1, ApplicationLifetimeProbe.Count("mount:main"), "WPF: the main root effect should mount once.");
        AssertEqual(1, ApplicationLifetimeProbe.Count("mount:child"), "WPF: the child root effect should mount once.");
        AssertEqual(1, ApplicationLifetimeProbe.Count("cleanup:main"), "WPF: application shutdown should clean the main root once.");
        AssertEqual(1, ApplicationLifetimeProbe.Count("cleanup:child"), "WPF: application shutdown should clean the child root once.");
        AssertEqual(initialRootCount, NuriDiagnostics.GetSnapshot().Roots.Count, "WPF: application shutdown should unregister every application root.");

        ApplicationLifetimeProbe.Reset();
        NuriDiagnostics.ClearLogs();
        NuriDiagnostics.Disable();
    }

    private static void WpfInputEventSubscriptionsRemainStable()
    {
        var log = new List<string>();
        var component = new InputEventLifecycleComponent(log);
        var driver = new WpfDriver();
        var root = driver.Initialize(component);
        using var keyboardSource = new System.Windows.Interop.HwndSource(
            new System.Windows.Interop.HwndSourceParameters("Nuri.RendererTests.InputEvents")
            {
                Width = 1,
                Height = 1,
                WindowStyle = 0
            });

        var controls = driver.RootChildren;
        var button = (WpfButton)controls[0];
        var textBox = (WpfTextBox)controls[1];
        var checkBox = (WpfCheckBox)controls[2];
        var pointerSurface = (WpfTextBlock)controls[3];

        RaiseWpfInputEvents(button, textBox, checkBox, pointerSurface, keyboardSource, "initial-input");
        AssertSequence(ExpectedInputEventLog(1, "initial-input"), log, "WPF: supported input events should deliver their native payloads once.");

        log.Clear();
        component.Generation = 2;
        root.Rebuild();
        AssertSame(button, driver.RootChildren[0], "WPF: replacing event handlers should preserve the button control.");
        AssertSame(textBox, driver.RootChildren[1], "WPF: replacing event handlers should preserve the text input control.");
        AssertSame(checkBox, driver.RootChildren[2], "WPF: replacing event handlers should preserve the check control.");
        AssertSame(pointerSurface, driver.RootChildren[3], "WPF: replacing event handlers should preserve the pointer surface.");
        RaiseWpfInputEvents(button, textBox, checkBox, pointerSurface, keyboardSource, "replacement-input");
        AssertSequence(ExpectedInputEventLog(2, "replacement-input"), log, "WPF: handler replacement should remove every previous callback.");

        for (var generation = 3; generation <= 52; generation++)
        {
            component.Generation = generation;
            root.Rebuild();
        }

        log.Clear();
        RaiseWpfInputEvents(button, textBox, checkBox, pointerSurface, keyboardSource, "stress-input");
        AssertSequence(ExpectedInputEventLog(52, "stress-input"), log, "WPF: repeated rebuilds should not accumulate event handlers.");

        component.ShowInputs = false;
        root.Rebuild();
        log.Clear();
        RaiseWpfInputEvents(button, textBox, checkBox, pointerSurface, keyboardSource, "removed-input");
        AssertEqual(0, log.Count, "WPF: removed subtrees should detach native event handlers.");
        root.Dispose();

        var disposeLog = new List<string>();
        var disposeComponent = new InputEventLifecycleComponent(disposeLog) { Generation = 100 };
        var disposeDriver = new WpfDriver();
        var disposeRoot = disposeDriver.Initialize(disposeComponent);
        var disposeControls = disposeDriver.RootChildren;
        var disposedButton = (WpfButton)disposeControls[0];
        var disposedTextBox = (WpfTextBox)disposeControls[1];
        var disposedCheckBox = (WpfCheckBox)disposeControls[2];
        var disposedPointerSurface = (WpfTextBlock)disposeControls[3];
        disposeRoot.Dispose();
        RaiseWpfInputEvents(disposedButton, disposedTextBox, disposedCheckBox, disposedPointerSurface, keyboardSource, "disposed-input");
        AssertEqual(0, disposeLog.Count, "WPF: root disposal should detach native event handlers.");
    }

    private static void RaiseWpfInputEvents(
        WpfButton button,
        WpfTextBox textBox,
        WpfCheckBox checkBox,
        WpfTextBlock pointerSurface,
        System.Windows.PresentationSource keyboardSource,
        string textValue)
    {
        button.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        textBox.Text = textValue;
        checkBox.IsChecked = true;
        checkBox.IsChecked = false;

        pointerSurface.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount)
        {
            RoutedEvent = System.Windows.Input.Mouse.MouseEnterEvent
        });
        pointerSurface.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount)
        {
            RoutedEvent = System.Windows.Input.Mouse.MouseLeaveEvent
        });
        pointerSurface.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left)
        {
            RoutedEvent = System.Windows.UIElement.MouseLeftButtonDownEvent
        });
        pointerSurface.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left)
        {
            RoutedEvent = System.Windows.UIElement.MouseLeftButtonUpEvent
        });

        textBox.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, keyboardSource, Environment.TickCount, System.Windows.Input.Key.Escape)
        {
            RoutedEvent = System.Windows.Input.Keyboard.KeyDownEvent
        });
        textBox.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, keyboardSource, Environment.TickCount, System.Windows.Input.Key.Escape)
        {
            RoutedEvent = System.Windows.Input.Keyboard.KeyUpEvent
        });
        textBox.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.UIElement.GotFocusEvent));
        textBox.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.UIElement.LostFocusEvent));
    }

    private static IReadOnlyList<string> ExpectedInputEventLog(int generation, string textValue)
    {
        return new[]
        {
            $"click:{generation}",
            $"text:{generation}:{textValue}",
            $"check:{generation}:True",
            $"check:{generation}:False",
            $"hover:{generation}:True",
            $"hover:{generation}:False",
            $"mouse-down:{generation}",
            $"mouse-up:{generation}",
            $"key-down:{generation}:Escape",
            $"key-up:{generation}:Escape",
            $"focus:{generation}:True",
            $"focus:{generation}:False"
        };
    }

    private static void WpfUnsupportedEventDiagnosticsAreDeduplicated()
    {
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        var component = new UnsupportedEventDiagnosticsComponent();
        using (var root = new WpfDriver().Initialize(component))
        {
            component.Version++;
            root.Rebuild();
            component.Version++;
            root.Rebuild();

            var unsupportedLogs = NuriDiagnostics.GetSnapshot().RecentLogs
                .Where(entry => entry.Kind == RuntimeLogKind.UnsupportedEvent)
                .ToArray();
            AssertEqual(2, unsupportedLogs.Length, "WPF: repeated unsupported event updates should emit one diagnostic per control type and event.");
            AssertEqual(true, unsupportedLogs.Any(entry => entry.Message.Contains(EventKeys.TextChanged, StringComparison.Ordinal) && entry.Message.Contains("TextBlock", StringComparison.Ordinal)), "WPF: a missing native event diagnostic should name the event and control type.");
            AssertEqual(true, unsupportedLogs.Any(entry => entry.Message.Contains("UnknownNeutral", StringComparison.Ordinal) && entry.Message.Contains("could not be mapped", StringComparison.Ordinal)), "WPF: an unmappable neutral event should explain the mapping failure.");

            NuriDiagnostics.ClearLogs();
            component.Version++;
            root.Rebuild();
            AssertEqual(2, NuriDiagnostics.GetSnapshot().RecentLogs.Count(entry => entry.Kind == RuntimeLogKind.UnsupportedEvent), "WPF: clearing diagnostics should allow unsupported events to be reported again.");

            NuriDiagnostics.ClearLogs();
            component.IncludeUnsupportedEvents = false;
            root.Rebuild();
            AssertEqual(0, NuriDiagnostics.GetSnapshot().RecentLogs.Count(entry => entry.Kind == RuntimeLogKind.UnsupportedEvent), "WPF: removing unsupported events should not emit new diagnostics.");
        }

        NuriDiagnostics.ClearLogs();
        NuriDiagnostics.Disable();
        WpfVirtualEntryRenderer.Build(
            new UnsupportedEventDiagnosticsComponent()
                .ToVirtualEntry()
                .WithIdentity("unsupported-event-disabled", null));
        AssertEqual(0, NuriDiagnostics.GetSnapshot().RecentLogs.Count(entry => entry.Kind == RuntimeLogKind.UnsupportedEvent), "WPF: disabled diagnostics should not record unsupported events.");
    }

    private static void WpfUnsupportedPropertyDiagnosticsAreDeduplicated()
    {
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        var component = new UnsupportedPropertyDiagnosticsComponent();
        using (var root = new WpfDriver().Initialize(component))
        {
            component.Value = "updated";
            root.Rebuild();
            component.Value = "updated-again";
            root.Rebuild();

            var unsupportedLogs = NuriDiagnostics.GetSnapshot().RecentLogs
                .Where(entry => entry.Kind == RuntimeLogKind.UnsupportedProperty)
                .ToArray();
            AssertEqual(1, unsupportedLogs.Length, "WPF: repeated unsupported property updates should emit one diagnostic per control type and property.");
            AssertEqual(true, unsupportedLogs[0].Message.Contains("UnsupportedProbe", StringComparison.Ordinal), "WPF: unsupported property diagnostics should name the property.");
            AssertEqual(true, unsupportedLogs[0].Message.Contains("TextBlock", StringComparison.Ordinal), "WPF: unsupported property diagnostics should name the native control type.");

            NuriDiagnostics.ClearLogs();
            component.IncludeUnsupportedProperty = false;
            root.Rebuild();
            AssertEqual(1, NuriDiagnostics.GetSnapshot().RecentLogs.Count(entry => entry.Kind == RuntimeLogKind.UnsupportedProperty), "WPF: clearing diagnostics should allow an unsupported property removal to be reported again.");
        }

        NuriDiagnostics.ClearLogs();
        NuriDiagnostics.Disable();
        WpfVirtualEntryRenderer.Build(
            Component.Text("disabled")
                .SetProperty("UnsupportedProbe", "value")
                .ToVirtualEntry()
                .WithIdentity("unsupported-disabled", null));
        AssertEqual(0, NuriDiagnostics.GetSnapshot().RecentLogs.Count(entry => entry.Kind == RuntimeLogKind.UnsupportedProperty), "WPF: disabled diagnostics should not record unsupported properties.");
    }

    private static void WpfRepeatedEffectLifecycleRemainsStable()
    {
        const int iterations = 50;

        var toggleLog = new List<string>();
        var toggleComponent = new ToggleChildComponent(toggleLog);
        var toggleRoot = new WpfDriver().Initialize(toggleComponent);
        for (var index = 0; index < iterations; index++)
        {
            toggleComponent.ShowChild = false;
            toggleRoot.Rebuild();
            toggleComponent.ShowChild = true;
            toggleRoot.Rebuild();
        }

        AssertEqual(iterations + 1, toggleLog.Count(entry => entry == "mount:child"), "WPF: repeated mounts should run the effect exactly once per mount.");
        AssertEqual(iterations, toggleLog.Count(entry => entry == "cleanup:child"), "WPF: repeated unmounts should clean up exactly once per removal.");
        toggleRoot.Dispose();
        AssertEqual(iterations + 1, toggleLog.Count(entry => entry == "cleanup:child"), "WPF: root disposal should clean up the final mounted child exactly once.");

        var replacementLog = new List<string>();
        var replacementComponent = new KeyReplacementComponent(replacementLog);
        var replacementRoot = new WpfDriver().Initialize(replacementComponent);
        for (var index = 0; index < iterations; index++)
        {
            replacementComponent.CurrentKey = index % 2 == 0 ? "b" : "a";
            replacementRoot.Rebuild();
        }

        AssertEqual(iterations + 1, replacementLog.Count(entry => entry.StartsWith("mount:", StringComparison.Ordinal)), "WPF: repeated key replacement should mount one logical child per key.");
        AssertEqual(iterations, replacementLog.Count(entry => entry.StartsWith("cleanup:", StringComparison.Ordinal)), "WPF: repeated key replacement should clean the previous logical child exactly once.");
        replacementRoot.Dispose();
        AssertEqual(iterations + 1, replacementLog.Count(entry => entry.StartsWith("cleanup:", StringComparison.Ordinal)), "WPF: disposal should balance repeated replacement mounts and cleanups.");

        var moveLog = new List<string>();
        var moveComponent = new KeyedMoveComponent(moveLog);
        var moveDriver = new WpfDriver();
        using var moveRoot = moveDriver.Initialize(moveComponent);
        var originalControls = moveDriver.RootChildren.ToDictionary(moveDriver.GetText, child => child);
        var initialMoveLog = moveLog.ToArray();
        var orders = new[]
        {
            new[] { "b", "c", "a" },
            new[] { "c", "a", "b" },
            new[] { "a", "b", "c" }
        };

        for (var index = 0; index < iterations; index++)
        {
            moveComponent.Order = orders[index % orders.Length];
            moveRoot.Rebuild();
            AssertChildOrderAndIdentity(moveDriver, moveComponent.Order, originalControls);
        }

        AssertSequence(initialMoveLog, moveLog, "WPF: repeated keyed moves should preserve effect lifetimes.");
    }

    private static void WpfDisposedRootIgnoresQueuedInvalidations()
    {
        var log = new List<string>();
        var component = new DeferredInvalidationContainer(log);
        var host = new WpfTestHost();
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        var root = WPF.ApplicationRoot.Initialize(component, host, () => dispatcher);

        AssertEqual(1, component.Child.RenderCount, "WPF: the deferred child should render once during initialization.");
        root.ScheduleComponentRebuild(component.Child);
        root.Dispose();
        DrainDispatcher(dispatcher);

        AssertEqual(1, component.Child.RenderCount, "WPF: a queued invalidation must not render after root disposal.");
        AssertSequence(new[] { "mount:deferred", "cleanup:deferred" }, log, "WPF: disposal should not remount an effect from a queued invalidation.");
    }

    private static void WpfMultipleRootsIsolateStateAndCleanupClosedWindows()
    {
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        var initialRootCount = NuriDiagnostics.GetSnapshot().Roots.Count;
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        var sharedStore = new Store<int>(0);
        var lifecycleLog = new List<string>();
        var firstComponent = new MultiRootProbeComponent("first", sharedStore, lifecycleLog);
        var secondComponent = new MultiRootProbeComponent("second", sharedStore, lifecycleLog);
        var firstWindow = CreateHiddenWpfWindow("Nuri.RendererTests.MultiRoot.First");
        var secondWindow = CreateHiddenWpfWindow("Nuri.RendererTests.MultiRoot.Second");
        var firstClosed = false;
        var secondClosed = false;

        try
        {
            NuriApplication.Attach(firstWindow, firstComponent);
            NuriApplication.Attach(secondWindow, secondComponent);
            firstWindow.Show();
            secondWindow.Show();

            AssertEqual(initialRootCount + 2, NuriDiagnostics.GetSnapshot().Roots.Count, "WPF: attaching two windows should register two independent roots.");
            AssertEqual(1, firstComponent.RenderCount, "WPF: the first root should render once during initialization.");
            AssertEqual(1, secondComponent.RenderCount, "WPF: the second root should render once during initialization.");
            AssertSequence(new[] { "mount:first", "mount:second" }, lifecycleLog, "WPF: each root should mount its effect exactly once.");
            AssertEqual(2, CountStoreSubscriptions(firstComponent.Id, secondComponent.Id), "WPF: each root should own an independent shared Store subscription.");

            firstComponent.SetLocalState!(current => current + 1);
            DrainDispatcher(dispatcher);
            AssertEqual(2, firstComponent.RenderCount, "WPF: local state should rerender its owning root.");
            AssertEqual(1, secondComponent.RenderCount, "WPF: local state should not rerender another root.");
            AssertEqual(1, firstComponent.LocalValue, "WPF: the first root should retain its local state.");
            AssertEqual(0, secondComponent.LocalValue, "WPF: the second root should retain independent local state.");

            sharedStore.Set(1);
            DrainDispatcher(dispatcher);
            AssertEqual(3, firstComponent.RenderCount, "WPF: a shared Store update should rerender the first subscribed root.");
            AssertEqual(2, secondComponent.RenderCount, "WPF: a shared Store update should rerender the second subscribed root.");
            AssertEqual(1, firstComponent.SharedValue, "WPF: the first root should observe the shared Store value.");
            AssertEqual(1, secondComponent.SharedValue, "WPF: the second root should observe the shared Store value.");

            firstWindow.Close();
            firstClosed = true;
            DrainDispatcher(dispatcher);
            AssertEqual(initialRootCount + 1, NuriDiagnostics.GetSnapshot().Roots.Count, "WPF: closing one window should unregister only its root.");
            AssertEqual(1, lifecycleLog.Count(entry => entry == "cleanup:first"), "WPF: closing one window should clean its effect exactly once.");
            AssertEqual(0, lifecycleLog.Count(entry => entry == "cleanup:second"), "WPF: closing one window should not clean another root.");
            AssertEqual(1, CountStoreSubscriptions(firstComponent.Id, secondComponent.Id), "WPF: closing one window should remove only its Store subscription.");

            var firstRenderCountAfterClose = firstComponent.RenderCount;
            firstComponent.SetLocalState!(current => current + 1);
            sharedStore.Set(2);
            DrainDispatcher(dispatcher);
            AssertEqual(firstRenderCountAfterClose, firstComponent.RenderCount, "WPF: stale local setters and Store subscriptions must not rerender a closed root.");
            AssertEqual(3, secondComponent.RenderCount, "WPF: the remaining root should continue receiving shared Store updates.");
            AssertEqual(2, secondComponent.SharedValue, "WPF: the remaining root should observe the latest shared Store value.");

            secondWindow.Close();
            secondClosed = true;
            DrainDispatcher(dispatcher);
            AssertEqual(initialRootCount, NuriDiagnostics.GetSnapshot().Roots.Count, "WPF: closing every window should restore the original registered-root count.");
            AssertEqual(1, lifecycleLog.Count(entry => entry == "cleanup:second"), "WPF: the remaining root should clean its effect exactly once when closed.");
            AssertEqual(0, CountStoreSubscriptions(firstComponent.Id, secondComponent.Id), "WPF: closing every window should remove all of their Store subscriptions.");
        }
        finally
        {
            if (!firstClosed)
                firstWindow.Close();
            if (!secondClosed)
                secondWindow.Close();

            NuriDiagnostics.ClearLogs();
            NuriDiagnostics.Disable();
        }
    }

    private static int CountStoreSubscriptions(params string[] componentIds)
    {
        var expectedIds = componentIds.ToHashSet(StringComparer.Ordinal);
        return NuriDiagnostics.GetSnapshot().Stores
            .SelectMany(store => store.Subscriptions)
            .Count(subscription => expectedIds.Contains(subscription.ComponentId));
    }

    private static System.Windows.Window CreateHiddenWpfWindow(string title)
    {
        return new System.Windows.Window
        {
            Title = title,
            Width = 1,
            Height = 1,
            ShowInTaskbar = false,
            WindowStyle = System.Windows.WindowStyle.None,
            Opacity = 0
        };
    }

    private static void DrainDispatcher(System.Windows.Threading.Dispatcher dispatcher)
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    private static void WpfVirtualizedItemsStayLazyAndRecycleContainers()
    {
        NuriDiagnostics.Enable();
        var templateCalls = 0;
        var items = Enumerable.Range(0, 10_000).Select(index => new VirtualizedRow(index, false)).ToArray();
        var oldElement = Component.VirtualizedItems(
            items,
            item => item.Index.ToString(),
            32,
            item =>
            {
                templateCalls++;
                return Component.Text(item.Index.ToString());
            });
        var oldEntry = oldElement.ToVirtualEntry().WithIdentity("virtualized-test", null);
        var native = (WpfListBox)WpfVirtualEntryRenderer.Build(oldEntry);

        AssertEqual(0, templateCalls, "WPF: building a virtualized host should not render item templates eagerly.");

        var window = new System.Windows.Window
        {
            Width = 400,
            Height = 700,
            Content = native,
            ShowInTaskbar = false,
            WindowStyle = System.Windows.WindowStyle.None,
            Opacity = 0
        };
        window.Show();
        native.ApplyTemplate();
        native.UpdateLayout();

        var realizedBefore = CountRealized(native);
        var initialMetrics = NuriDiagnostics.GetSnapshot().VirtualizedItems.Single(item => item.HostId == oldEntry.Id);
        AssertEqual(true, realizedBefore > 0 && realizedBefore <= 100, $"WPF: a 700px viewport should realize a bounded number of 32px rows (actual {realizedBefore}).");
        AssertEqual(realizedBefore, initialMetrics.RealizedCount, "WPF: diagnostics should report the current realized row count.");
        AssertEqual(true, templateCalls <= 100, "WPF: initial template calls should be proportional to realized rows.");

        var firstContainer = (WpfListBoxItem?)native.ItemContainerGenerator.ContainerFromIndex(0);
        AssertEqual(new System.Windows.Thickness(0), firstContainer!.Padding, "WPF: virtualized containers should not shrink row content with theme padding.");
        AssertEqual(new System.Windows.Thickness(0), firstContainer.BorderThickness, "WPF: virtualized containers should not shrink row content with theme borders.");
        AssertEqual(System.Windows.VerticalAlignment.Stretch, firstContainer.VerticalContentAlignment, "WPF: virtualized row content should stretch through the fixed extent.");
        AssertEqual(32d, ((WpfFrameworkElement)firstContainer.Content).ActualHeight, "WPF: virtualized row content should receive the full fixed extent.");
        var updatedItems = (VirtualizedRow[])items.Clone();
        updatedItems[0] = updatedItems[0] with { Selected = true };
        var newElement = Component.VirtualizedItems(
            updatedItems,
            item => item.Index.ToString(),
            32,
            item =>
            {
                templateCalls++;
                return Component.Text(item.Selected ? $"selected:{item.Index}" : item.Index.ToString());
            });
        var newEntry = newElement.ToVirtualEntry().WithIdentity("virtualized-test", null);
        var operations = VirtualTreeDiff.Diff(oldEntry, newEntry);
        WpfVirtualEntryRenderer.ApplyDiff(native, operations);

        AssertSame(firstContainer!, native.ItemContainerGenerator.ContainerFromIndex(0)!, "WPF: a same-key update should preserve its native item container.");
        AssertEqual(true, templateCalls <= 200, "WPF: a template refresh should remain proportional to realized rows, not 10k source rows.");

        var reorderedItems = updatedItems.ToArray();
        (reorderedItems[0], reorderedItems[1]) = (reorderedItems[1], reorderedItems[0]);
        var reorderedEntry = VirtualizedEntry(
            reorderedItems,
            item => item.Selected ? $"selected:{item.Index}" : item.Index.ToString(),
            () => templateCalls++);
        ApplyVirtualizedDiff(native, ref newEntry, reorderedEntry);
        native.UpdateLayout();

        AssertSame(firstContainer!, native.ItemContainerGenerator.ContainerFromIndex(1)!, "WPF: a visible keyed move should retain its native item container.");
        AssertEqual("selected:0", GetRealizedText(native, 1), "WPF: a moved row should retain its latest rendered value.");

        var filteredItems = reorderedItems.Where(item => item.Index % 2 == 0).ToArray();
        var filteredEntry = VirtualizedEntry(
            filteredItems,
            item => item.Selected ? $"selected:{item.Index}" : item.Index.ToString(),
            () => templateCalls++);
        ApplyVirtualizedDiff(native, ref newEntry, filteredEntry);
        native.UpdateLayout();

        AssertEqual(5_000, native.Items.Count, "WPF: filtering should remove every missing virtual item.");
        AssertEqual("selected:0", GetRealizedText(native, 0), "WPF: filtering should keep retained keyed row content current.");

        var reversedEntry = VirtualizedEntry(
            filteredItems.Reverse().ToArray(),
            item => item.Selected ? $"selected:{item.Index}" : item.Index.ToString(),
            () => templateCalls++);
        ApplyVirtualizedDiff(native, ref newEntry, reversedEntry);
        native.UpdateLayout();
        AssertEqual(5_000, native.Items.Count, "WPF: a large keyed reversal should retain every virtual item.");
        AssertEqual("9998", GetRealizedText(native, 0), "WPF: a large keyed reversal should materialize the requested order.");

        var emptyEntry = VirtualizedEntry(
            Array.Empty<VirtualizedRow>(),
            item => item.Index.ToString(),
            () => templateCalls++);
        ApplyVirtualizedDiff(native, ref newEntry, emptyEntry);
        native.UpdateLayout();
        AssertEqual(0, native.Items.Count, "WPF: clearing a virtualized source should remove every item.");

        var duplicateEntry = VirtualizedEntry(
            new[]
            {
                new VirtualizedRow(7, false),
                new VirtualizedRow(7, true)
            },
            item => item.Selected ? "selected:7" : "plain:7",
            () => templateCalls++);
        ApplyVirtualizedDiff(native, ref newEntry, duplicateEntry);
        native.UpdateLayout();
        AssertEqual(2, native.Items.Count, "WPF: duplicate-key fallback identities should keep every row independently addressable.");
        AssertEqual("plain:7", GetRealizedText(native, 0), "WPF: the first duplicate-key row should render independently.");
        AssertEqual("selected:7", GetRealizedText(native, 1), "WPF: the second duplicate-key row should render independently.");

        var replacementItems = Enumerable.Range(20_000, 10_000).Select(index => new VirtualizedRow(index, false)).ToArray();
        var replacementEntry = VirtualizedEntry(
            replacementItems,
            item => item.Index.ToString(),
            () => templateCalls++);
        ApplyVirtualizedDiff(native, ref newEntry, replacementEntry);
        native.UpdateLayout();
        AssertEqual(10_000, native.Items.Count, "WPF: a cleared source should accept a full replacement.");
        AssertEqual("20000", GetRealizedText(native, 0), "WPF: replacement rows should render from the new source.");

        foreach (var index in new[] { 100, 200, 300, 400, 500 })
        {
            native.ScrollIntoView(native.Items[index]);
            native.UpdateLayout();
            AssertEqual(true, CountRealized(native) <= 100, $"WPF: repeated scrolling should keep a bounded realized set at index {index}.");
        }

        window.Content = null;
        window.UpdateLayout();
        window.Content = native;
        window.UpdateLayout();
        AssertEqual(true, CountRealized(native) > 0, "WPF: reloading a virtualized host should restore realized rows.");
        AssertEqual(true, native.ItemContainerGenerator.ContainerFromIndex(500) is WpfListBoxItem reloaded && reloaded.Content != null, "WPF: reloaded containers should restore their row content.");
        window.Content = null;
        window.UpdateLayout();
        window.Close();
        NuriDiagnostics.Disable();
    }

    private static void WpfDiagnosticsTrackAppliedPatchBatches()
    {
        NuriDiagnostics.Enable();
        var component = new PatchDiagnosticsComponent();
        using (var root = new WpfDriver().Initialize(component))
        {
            component.Value = "updated";
            root.Rebuild();

            var metrics = NuriDiagnostics.GetSnapshot().Roots.Single();
            AssertEqual(1L, metrics.PatchBatchCount, "WPF: diagnostics should record one applied rebuild batch.");
            AssertEqual(1, metrics.LastPatchCount, "WPF: a text-only rebuild should apply one patch.");
            AssertEqual(1, metrics.LastPatchCounts[PatchOperationType.UpdateProperty], "WPF: diagnostics should identify the applied property patch.");
        }

        NuriDiagnostics.Disable();
    }

    private static void WpfRootDisposalRemovesVirtualizedDiagnostics()
    {
        NuriDiagnostics.Enable();
        var element = Component.VirtualizedItems(
            Enumerable.Range(0, 100).ToArray(),
            item => item.ToString(),
            32,
            item => Component.Text(item.ToString()));
        var root = new WpfDriver().Initialize(element);

        AssertEqual(1, NuriDiagnostics.GetSnapshot().VirtualizedItems.Count, "WPF: a mounted virtualized host should register diagnostics.");
        root.Dispose();
        AssertEqual(0, NuriDiagnostics.GetSnapshot().VirtualizedItems.Count, "WPF: root disposal should remove virtualized diagnostics deterministically.");
        NuriDiagnostics.Disable();
    }

    private static VirtualEntry VirtualizedEntry(
        IReadOnlyList<VirtualizedRow> items,
        Func<VirtualizedRow, string> text,
        Action onRender)
    {
        return Component.VirtualizedItems(
                items,
                item => item.Index.ToString(),
                32,
                item =>
                {
                    onRender();
                    return Component.Text(text(item));
                })
            .ToVirtualEntry()
            .WithIdentity("virtualized-test", null);
    }

    private static void ApplyVirtualizedDiff(WpfListBox native, ref VirtualEntry current, VirtualEntry next)
    {
        WpfVirtualEntryRenderer.ApplyDiff(native, VirtualTreeDiff.Diff(current, next));
        current = next;
    }

    private static string? GetRealizedText(WpfListBox listBox, int index)
    {
        return listBox.ItemContainerGenerator.ContainerFromIndex(index) is WpfListBoxItem container
            && container.Content is WpfTextBlock text
                ? text.Text
                : null;
    }

    private static int CountRealized(WpfListBox listBox)
    {
        var count = 0;
        for (var index = 0; index < listBox.Items.Count; index++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(index) != null)
                count++;
        }

        return count;
    }

    private static void WpfTransitionsReplaceAndClearNativeAnimations(WpfDriver driver)
    {
        var component = new WpfTransitionComponent();
        using var root = driver.Initialize(component);

        component.Margin = 18;
        component.Background = "#2563eb";
        component.Foreground = "#fef3c7";
        component.Rotation = 5;
        component.ScaleX = 1.05;
        component.ScaleY = 0.97;
        component.TranslateX = 6;
        component.TranslateY = -2;
        root.Rebuild();

        var targets = driver.RootChildren.Cast<WpfFrameworkElement>().ToArray();
        AssertWpfTransitions(targets, true);

        component.Margin = 12;
        component.Background = "#7c3aed";
        component.Foreground = "#f8fafc";
        component.Rotation = -6;
        component.ScaleX = 1.12;
        component.ScaleY = 0.94;
        component.TranslateX = 14;
        component.TranslateY = -8;
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

        var transformGroup = (WpfTransformGroup)updatedTargets[3].RenderTransform;
        var rotateTransform = transformGroup.Children.OfType<WpfRotateTransform>().Single();
        var scaleTransform = transformGroup.Children.OfType<WpfScaleTransform>().Single();
        var translateTransform = transformGroup.Children.OfType<WpfTranslateTransform>().Single();
        AssertEqual(-6d, (double)rotateTransform.GetAnimationBaseValue(WpfRotateTransform.AngleProperty), "WPF: Rotate should retain the latest base angle.");
        AssertEqual(1.12d, (double)scaleTransform.GetAnimationBaseValue(WpfScaleTransform.ScaleXProperty), "WPF: ScaleX should retain the latest base value.");
        AssertEqual(0.94d, (double)scaleTransform.GetAnimationBaseValue(WpfScaleTransform.ScaleYProperty), "WPF: ScaleY should retain the latest base value.");
        AssertEqual(14d, (double)translateTransform.GetAnimationBaseValue(WpfTranslateTransform.XProperty), "WPF: TranslateX should retain the latest base value.");
        AssertEqual(-8d, (double)translateTransform.GetAnimationBaseValue(WpfTranslateTransform.YProperty), "WPF: TranslateY should retain the latest base value.");

        component.Animate = false;
        root.Rebuild();
        AssertWpfTransitions(updatedTargets, false);

        component.IncludeTransforms = false;
        root.Rebuild();
        AssertEqual(0d, rotateTransform.Angle, "WPF: removing Rotate should restore its default value.");
        AssertEqual(1d, scaleTransform.ScaleX, "WPF: removing ScaleX should restore its default value.");
        AssertEqual(1d, scaleTransform.ScaleY, "WPF: removing ScaleY should restore its default value.");
        AssertEqual(0d, translateTransform.X, "WPF: removing TranslateX should restore its default value.");
        AssertEqual(0d, translateTransform.Y, "WPF: removing TranslateY should restore its default value.");
    }

    private static void AssertWpfTransitions(IReadOnlyList<WpfFrameworkElement> targets, bool expected)
    {
        AssertEqual(4, targets.Count, "WPF: the animation probe should retain four targets.");
        AssertEqual(expected, targets[0].HasAnimatedProperties, "WPF: Margin animation state should match the virtual transition.");

        var backgroundBrush = (WpfSolidColorBrush)((WpfBorder)targets[1]).Background;
        AssertEqual(expected, backgroundBrush.HasAnimatedProperties, "WPF: Background animation state should match the virtual transition.");

        var foregroundBrush = (WpfSolidColorBrush)((WpfTextBlock)targets[2]).Foreground;
        AssertEqual(expected, foregroundBrush.HasAnimatedProperties, "WPF: Foreground animation state should match the virtual transition.");

        var transformGroup = (WpfTransformGroup)targets[3].RenderTransform;
        var rotateTransform = transformGroup.Children.OfType<WpfRotateTransform>().Single();
        var scaleTransform = transformGroup.Children.OfType<WpfScaleTransform>().Single();
        var translateTransform = transformGroup.Children.OfType<WpfTranslateTransform>().Single();
        AssertEqual(expected, rotateTransform.HasAnimatedProperties, "WPF: Rotate animation state should match the virtual transition.");
        AssertEqual(expected, scaleTransform.HasAnimatedProperties, "WPF: Scale animation state should match the virtual transition.");
        AssertEqual(expected, translateTransform.HasAnimatedProperties, "WPF: Translate animation state should match the virtual transition.");
    }

    private static void RunSuite(Func<RendererDriver> createDriver)
    {
        EffectsRunAfterNativeCommit(createDriver());
        SubtreeEffectsRunAfterNativeCommit(createDriver());
        RemovedSubtreesCleanUpAfterNativeRemoval(createDriver());
        KeyReplacementCleansUpBeforeMount(createDriver());
        KeyedMovesPreserveNativeControlsAndEffects(createDriver());
        OpacityTransitionsAreMaterializedReplacedAndRemoved(createDriver());
        ContentDistributionLayoutsRemainConsistent(createDriver());
        GrowLayoutsRemainConsistent(createDriver());
        GridSpacingLayoutsRemainConsistent(createDriver());
        RootDisposeCleansDescendantsExactlyOnce(createDriver());
    }

    private static void GridSpacingLayoutsRemainConsistent(RendererDriver driver)
    {
        var columns = Component.Grid(
                Component.Div().Column(0).Size(10, 10).Start().Top(),
                Component.Div().Column(1).Size(10, 10).Start().Top())
            .Rows("*")
            .Columns("*,*")
            .Size(100, 20)
            .ColumnSpacing(10);

        using (driver.Initialize(columns))
        {
            AssertOffsets(
                new[] { 0d, 55d },
                driver.ArrangeAndGetMainOffsets(100, 20, horizontal: true),
                $"{driver.Name}: ColumnSpacing should reserve space between Grid columns.");
        }

        var rows = Component.Grid(
                Component.Div().Row(0).Size(10, 10).Start().Top(),
                Component.Div().Row(1).Size(10, 10).Start().Top())
            .Rows("*,*")
            .Columns("*")
            .Size(20, 100)
            .RowSpacing(10);

        using (driver.Initialize(rows))
        {
            AssertOffsets(
                new[] { 0d, 55d },
                driver.ArrangeAndGetMainOffsets(100, 20, horizontal: false),
                $"{driver.Name}: RowSpacing should reserve space between Grid rows.");
        }

        var autoFlow = Component.Grid(
                Component.Div().Size(10, 10).Start().Top(),
                Component.Div().Size(10, 10).Start().Top(),
                Component.Div().Size(10, 10).Start().Top(),
                Component.Div().Size(10, 10).Start().Top())
            .Columns("*,*")
            .AutoFlow()
            .Size(100, 100)
            .Spacing(10);

        using (driver.Initialize(autoFlow))
        {
            AssertOffsets(
                new[] { 0d, 55d, 0d, 55d },
                driver.ArrangeAndGetMainOffsets(100, 100, horizontal: true),
                $"{driver.Name}: AutoFlow should fill Grid columns in row-major order.");
            AssertAutoFlowRows(
                driver.ArrangeAndGetMainOffsets(100, 100, horizontal: false),
                $"{driver.Name}: AutoFlow should continue in generated Auto rows.");
        }
    }

    private static void GrowLayoutsRemainConsistent(RendererDriver driver)
    {
        var equalRow = Component.Div(
                DivTypes.Row,
                Component.Div().Height(10).Grow(),
                Component.Div().Height(10).Grow())
            .Size(100, 20)
            .Spacing(10)
            .SpaceEvenly();

        using (driver.Initialize(equalRow))
        {
            AssertOffsets(
                new[] { 0d, 55d },
                driver.ArrangeAndGetMainOffsets(100, 20, horizontal: true),
                $"{driver.Name}: equal Grow children should fill the Row without remaining distributed space.");
            AssertOffsets(
                new[] { 45d, 45d },
                driver.ArrangeAndGetMainSizes(100, 20, horizontal: true),
                $"{driver.Name}: equal Grow weights should create equal Row slots.");
        }

        var weightedRow = Component.Div(
                DivTypes.Row,
                Component.Div().Height(10).Grow(),
                Component.Div().Height(10).Grow(2))
            .Size(100, 20)
            .Spacing(10);

        using (driver.Initialize(weightedRow))
        {
            AssertOffsets(
                new[] { 30d, 60d },
                driver.ArrangeAndGetMainSizes(100, 20, horizontal: true),
                $"{driver.Name}: weighted Grow children should divide Row space proportionally.");
        }

        var fixedAndGrow = Component.Div(
                DivTypes.Row,
                Component.Div().Size(20, 10),
                Component.Div().Height(10).Grow())
            .Size(100, 20)
            .Spacing(10);

        using (driver.Initialize(fixedAndGrow))
        {
            AssertOffsets(
                new[] { 20d, 70d },
                driver.ArrangeAndGetMainSizes(100, 20, horizontal: true),
                $"{driver.Name}: Grow should consume the Row space left by fixed children.");
        }

        var equalColumn = Component.Div(
                Component.Div().Width(10).Grow(),
                Component.Div().Width(10).Grow())
            .Size(20, 100)
            .Spacing(10);

        using (driver.Initialize(equalColumn))
        {
            AssertOffsets(
                new[] { 45d, 45d },
                driver.ArrangeAndGetMainSizes(100, 20, horizontal: false),
                $"{driver.Name}: equal Grow weights should create equal Column slots.");
        }
    }

    private static void AssertAutoFlowRows(IReadOnlyList<double> offsets, string message)
    {
        AssertEqual(4, offsets.Count, message + " Child count differs.");
        if (Math.Abs(offsets[0] - offsets[1]) > 0.001
            || Math.Abs(offsets[2] - offsets[3]) > 0.001
            || offsets[2] - offsets[0] < 20)
            throw new InvalidOperationException($"{message} Actual offsets: [{string.Join(", ", offsets)}].");
    }

    private static void ContentDistributionLayoutsRemainConsistent(RendererDriver driver)
    {
        var cases = new[]
        {
            (ContentDistribution.Start, new[] { 0d, 15d, 30d }),
            (ContentDistribution.Center, new[] { 30d, 45d, 60d }),
            (ContentDistribution.End, new[] { 60d, 75d, 90d }),
            (ContentDistribution.SpaceBetween, new[] { 0d, 45d, 90d }),
            (ContentDistribution.SpaceAround, new[] { 10d, 45d, 80d }),
            (ContentDistribution.SpaceEvenly, new[] { 15d, 45d, 75d })
        };

        foreach (var (distribution, expectedOffsets) in cases)
        {
            var element = Component.Div(
                    DivTypes.Row,
                    Component.Div().Size(10, 10),
                    Component.Div().Size(10, 10),
                    Component.Div().Size(10, 10))
                .Size(100, 20)
                .Spacing(5)
                .JustifyContent(distribution);

            using var root = driver.Initialize(element);
            AssertOffsets(
                expectedOffsets,
                driver.ArrangeAndGetMainOffsets(100, 20, horizontal: true),
                $"{driver.Name}: {distribution} should distribute horizontal children consistently.");
        }

        var column = Component.Div(
                Component.Div().Size(10, 10),
                Component.Div().Size(10, 10),
                Component.Div().Size(10, 10))
            .Size(20, 100)
            .Spacing(5)
            .SpaceEvenly();

        using var columnRoot = driver.Initialize(column);
        AssertOffsets(
            new[] { 15d, 45d, 75d },
            driver.ArrangeAndGetMainOffsets(100, 20, horizontal: false),
            $"{driver.Name}: SpaceEvenly should use the vertical axis for a column.");
    }

    private static void AssertOffsets(IReadOnlyList<double> expected, IReadOnlyList<double> actual, string message)
    {
        AssertEqual(expected.Count, actual.Count, message + " Child count differs.");
        for (var index = 0; index < expected.Count; index++)
        {
            if (Math.Abs(expected[index] - actual[index]) > 0.001)
                throw new InvalidOperationException($"{message} Expected offset {expected[index]} at index {index}; Actual: {actual[index]}.");
        }
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

        public abstract IReadOnlyList<double> ArrangeAndGetMainOffsets(double mainSize, double crossSize, bool horizontal);

        public abstract IReadOnlyList<double> ArrangeAndGetMainSizes(double mainSize, double crossSize, bool horizontal);
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

        public override IReadOnlyList<double> ArrangeAndGetMainOffsets(double mainSize, double crossSize, bool horizontal)
        {
            if (_host.Content == null || GetRootPanel(_host.Content) is not WpfPanel panel)
                return Array.Empty<double>();

            var width = horizontal ? mainSize : crossSize;
            var height = horizontal ? crossSize : mainSize;
            _host.Content.Measure(new System.Windows.Size(width, height));
            _host.Content.Arrange(new System.Windows.Rect(0, 0, width, height));

            return panel.Children
                .OfType<WpfFrameworkElement>()
                .Select(child => child.TranslatePoint(new System.Windows.Point(), panel))
                .Select(point => horizontal ? point.X : point.Y)
                .ToArray();
        }

        public override IReadOnlyList<double> ArrangeAndGetMainSizes(double mainSize, double crossSize, bool horizontal)
        {
            if (_host.Content == null || GetRootPanel(_host.Content) is not WpfPanel panel)
                return Array.Empty<double>();

            var width = horizontal ? mainSize : crossSize;
            var height = horizontal ? crossSize : mainSize;
            _host.Content.Measure(new System.Windows.Size(width, height));
            _host.Content.Arrange(new System.Windows.Rect(0, 0, width, height));

            return panel.Children
                .OfType<WpfFrameworkElement>()
                .Select(child => horizontal ? child.ActualWidth : child.ActualHeight)
                .ToArray();
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

        public override IReadOnlyList<double> ArrangeAndGetMainOffsets(double mainSize, double crossSize, bool horizontal)
        {
            if (_host.Content is not AvaloniaPanel panel)
                return Array.Empty<double>();

            var width = horizontal ? mainSize : crossSize;
            var height = horizontal ? crossSize : mainSize;
            panel.Measure(new global::Avalonia.Size(width, height));
            panel.Arrange(new global::Avalonia.Rect(0, 0, width, height));

            return panel.Children
                .Select(child => horizontal ? child.Bounds.X : child.Bounds.Y)
                .ToArray();
        }

        public override IReadOnlyList<double> ArrangeAndGetMainSizes(double mainSize, double crossSize, bool horizontal)
        {
            if (_host.Content is not AvaloniaPanel panel)
                return Array.Empty<double>();

            var width = horizontal ? mainSize : crossSize;
            var height = horizontal ? crossSize : mainSize;
            panel.Measure(new global::Avalonia.Size(width, height));
            panel.Arrange(new global::Avalonia.Rect(0, 0, width, height));

            return panel.Children
                .Select(child => horizontal ? child.Bounds.Width : child.Bounds.Height)
                .ToArray();
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

        public double ScaleX { get; set; } = 0.98;

        public double ScaleY { get; set; } = 1.02;

        public double TranslateX { get; set; } = -4;

        public double TranslateY { get; set; } = 3;

        public bool Animate { get; set; } = true;

        public bool IncludeTransforms { get; set; } = true;

        public override IElement Render()
        {
            var margin = Div().Margin(Margin);
            var background = Div().Background(Background);
            var foreground = Text("foreground").FontColor(Foreground);
            var transform = Text("transform");
            if (IncludeTransforms)
            {
                transform
                    .Rotate(Rotation)
                    .Scale(ScaleX, ScaleY)
                    .Translate(TranslateX, TranslateY);
            }

            if (Animate)
            {
                margin.Transition(500, EasingValue.CubicOut);
                background.Transition(500, EasingValue.CubicOut);
                foreground.Transition(500, EasingValue.CubicOut);
                transform.Transition(500, EasingValue.CubicOut);
            }

            return Grid(margin, background, foreground, transform);
        }
    }

    private sealed class PatchDiagnosticsComponent : Component
    {
        public string Value { get; set; } = "initial";

        public override IElement Render() => Text(Value);
    }

    private sealed class UnsupportedPropertyDiagnosticsComponent : Component
    {
        public string Value { get; set; } = "initial";

        public bool IncludeUnsupportedProperty { get; set; } = true;

        public override IElement Render()
        {
            var text = Text("supported")
                .Opacity(0.75)
                .Row(1);
            if (IncludeUnsupportedProperty)
                text.SetProperty("UnsupportedProbe", Value);

            return text;
        }
    }

    private sealed class UnsupportedEventDiagnosticsComponent : Component
    {
        public int Version { get; set; }

        public bool IncludeUnsupportedEvents { get; set; } = true;

        public override IElement Render()
        {
            var version = Version;
            var text = Text($"supported:{Version}")
                .OnClick(() => GC.KeepAlive(version));
            text.AddEvent(
                EventKeys.MouseEnter,
                new System.Windows.Input.MouseEventHandler((_, _) => GC.KeepAlive(version)));

            if (IncludeUnsupportedEvents)
            {
                text.AddVirtualEvent(
                    EventKeys.TextChanged,
                    new Nuri.UI.Events.VirtualEvent(
                        Nuri.UI.Events.VirtualEventKind.TextChanged,
                        new Action<string>(_ => GC.KeepAlive(version))));
                text.AddVirtualEvent(
                    "UnknownNeutral",
                    new Nuri.UI.Events.VirtualEvent(
                        (Nuri.UI.Events.VirtualEventKind)int.MaxValue,
                        new Action(() => GC.KeepAlive(version))));
            }

            return text;
        }
    }

    private sealed class InputEventLifecycleComponent : Component
    {
        private readonly List<string> _log;

        public InputEventLifecycleComponent(List<string> log)
        {
            _log = log;
        }

        public int Generation { get; set; } = 1;

        public bool ShowInputs { get; set; } = true;

        public override IElement Render()
        {
            if (!ShowInputs)
                return Grid();

            var generation = Generation;
            return Grid(
                Button($"button:{generation}")
                    .Key("event-button")
                    .OnClick(() => _log.Add($"click:{generation}")),
                TextBox(string.Empty)
                    .Key("event-text")
                    .OnTextChanged(value => _log.Add($"text:{generation}:{value}"))
                    .OnKeyDown(key => _log.Add($"key-down:{generation}:{key}"))
                    .OnKeyUp(key => _log.Add($"key-up:{generation}:{key}"))
                    .OnFocus(focused => _log.Add($"focus:{generation}:{focused}")),
                CheckBox($"check:{generation}")
                    .Key("event-check")
                    .OnCheckChanged(value => _log.Add($"check:{generation}:{value}")),
                Text("pointer")
                    .Key("event-pointer")
                    .OnHover(hovered => _log.Add($"hover:{generation}:{hovered}"))
                    .OnMouseDown(() => _log.Add($"mouse-down:{generation}"))
                    .OnMouseUp(() => _log.Add($"mouse-up:{generation}")));
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

    private sealed class DeferredInvalidationContainer : Component
    {
        public DeferredInvalidationContainer(List<string> log)
        {
            Child = new RenderCountingEffectChild("deferred", log).Key("deferred");
        }

        public RenderCountingEffectChild Child { get; }

        public override IElement Render() => Grid(Child);
    }

    private sealed class MultiRootProbeComponent : Component
    {
        private readonly string _name;
        private readonly Store<int> _sharedStore;
        private readonly List<string> _lifecycleLog;

        public MultiRootProbeComponent(string name, Store<int> sharedStore, List<string> lifecycleLog)
        {
            _name = name;
            _sharedStore = sharedStore;
            _lifecycleLog = lifecycleLog;
        }

        public int RenderCount { get; private set; }

        public int LocalValue { get; private set; }

        public int SharedValue { get; private set; }

        public Action<Func<int, int>>? SetLocalState { get; private set; }

        public override IElement Render()
        {
            RenderCount++;
            var (localValue, setLocalValue) = useState(0);
            var sharedValue = useStore(_sharedStore);
            LocalValue = localValue;
            SharedValue = sharedValue;
            SetLocalState = setLocalValue;

            useEffect(() =>
            {
                _lifecycleLog.Add($"mount:{_name}");
                return () => _lifecycleLog.Add($"cleanup:{_name}");
            }, []);

            return Text($"{_name}:{localValue}:{sharedValue}");
        }
    }

    private sealed class ApplicationLifetimeMainComponent : Component
    {
        public override IElement Render()
        {
            useEffect(() =>
            {
                ApplicationLifetimeProbe.Record("mount:main");
                ApplicationLifetimeProbe.OnMainMounted?.Invoke();
                return () => ApplicationLifetimeProbe.Record("cleanup:main");
            }, []);

            return Text("main");
        }
    }

    private sealed class ApplicationLifetimeChildComponent : Component
    {
        public override IElement Render()
        {
            useEffect(() =>
            {
                ApplicationLifetimeProbe.Record("mount:child");
                return () => ApplicationLifetimeProbe.Record("cleanup:child");
            }, []);

            return Text("child");
        }
    }

    private static class ApplicationLifetimeProbe
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<string> Entries = new List<string>();

        public static Action? OnMainMounted { get; set; }

        public static void Record(string entry)
        {
            lock (SyncRoot)
            {
                Entries.Add(entry);
            }
        }

        public static int Count(string entry)
        {
            lock (SyncRoot)
            {
                return Entries.Count(value => value == entry);
            }
        }

        public static void Reset()
        {
            lock (SyncRoot)
            {
                Entries.Clear();
                OnMainMounted = null;
            }
        }
    }

    private sealed class RenderCountingEffectChild : Component
    {
        private readonly string _name;
        private readonly List<string> _log;

        public RenderCountingEffectChild(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public int RenderCount { get; private set; }

        public override IElement Render()
        {
            RenderCount++;
            useEffect(() =>
            {
                _log.Add($"mount:{_name}");
                return () => _log.Add($"cleanup:{_name}");
            }, []);

            return Text(_name);
        }
    }
}
