using Duxel.Core;
using Nuri.AnimatedDashboardSample.Components;
using Nuri.Duxel;
using Nuri.ExplorerTreeSample.Components;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

internal static class Program
{
    private static readonly UiFontAtlas FontAtlas = CreateFontAtlas();
    private static readonly UiFrameInfo FrameInfo = new(
        1f / 60f,
        new UiVector2(1280, 720),
        new UiVector2(1, 1));

    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("renderer assembly excludes Windows host dependencies", RendererAssemblyExcludesWindowsHostDependencies),
            ("effect flush and cleanup", EffectFlushAndCleanup),
            ("root content uses viewport bounds", RootContentUsesViewportBounds),
            ("measured client size overrides viewport bounds", MeasuredClientSizeOverridesViewportBounds),
            ("grid rows advance by their tallest cell", GridRowsAdvanceByTallestCell),
            ("grid has no implicit track spacing", GridHasNoImplicitTrackSpacing),
            ("scroll projects one spaced Column child", ScrollProjectsOneSpacedColumnChild),
            ("scroll clips deferred panel decorations", ScrollClipsDeferredPanelDecorations),
            ("scrollbar respects viewport corners", ScrollbarRespectsViewportCorners),
            ("fixed grid tracks do not expand to overflowing content", FixedGridTracksDoNotExpand),
            ("implicit grid row fills its arranged height", ImplicitGridRowFillsArrangedHeight),
            ("measured auto row reduces remaining star height", MeasuredAutoRowReducesRemainingStarHeight),
            ("dirty state requests and projects a frame", DirtyStateRequestsAndProjectsFrame),
            ("full rebuild requests and projects a frame", FullRebuildRequestsAndProjectsFrame),
            ("runtime theme changes request and apply on the next frame", RuntimeThemeChangesRequestAndApply),
            ("Duxel theme colors use the neutral Background DSL", DuxelThemeColorsUseNeutralBackgroundDsl),
            ("opacity animation requests frames and supports interruption", OpacityAnimationRequestsFramesAndSupportsInterruption),
            ("diagnostics track Duxel roots and patches", DiagnosticsTrackDuxelRootsAndPatches),
            ("input queue preserves semantic event order", InputQueuePreservesSemanticEventOrder),
            ("wheel routes independently to Nuri scroll regions", WheelRoutesIndependentlyToNuriScrollRegions),
            ("shared Animated Dashboard projects headlessly", SharedAnimatedDashboardProjectsHeadlessly),
            ("shared Explorer tree projects headlessly", SharedExplorerTreeProjectsHeadlessly)
        };

        foreach (var test in tests)
        {
            test.Run();
            Console.WriteLine($"PASS: {test.Name}");
        }

        return 0;
    }

    private static void RendererAssemblyExcludesWindowsHostDependencies()
    {
        var references = typeof(NuriDuxelScreen).Assembly.GetReferencedAssemblies();
        AssertTrue(
            references.All(reference =>
                !string.Equals(reference.Name, "Duxel.Windows.App", StringComparison.Ordinal)
                && !string.Equals(reference.Name, "Duxel.Platform.Windows", StringComparison.Ordinal)),
            "The Nuri.Duxel renderer assembly must not reference the Duxel Windows host or platform assemblies.");
    }

    private static void RootContentUsesViewportBounds()
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new ViewportRootComponent(),
            () => { },
            "viewport-test");
        var probe = new ViewportProbeScreen(screen, 48f);
        var basicFrame = new UiFrameInfo(
            1f / 60f,
            new UiVector2(720, 580),
            new UiVector2(1, 1));

        RenderFrame(context, probe, basicFrame);

        AssertEqual(
            new UiVector2(0, 48),
            probe.Position,
            "Duxel root content must start below the Duxel title bar.");
        AssertEqual(
            new UiVector2(720, 532),
            probe.Size,
            "The Basic sample must use the title-bar-excluded viewport work area.");
    }

    private static void MeasuredClientSizeOverridesViewportBounds()
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new ImplicitViewportComponent(),
            () => { },
            "measured-client-test",
            input,
            () => new UiVector2(320, 180));

        RenderFrame(context, screen);

        var region = input.GetScrollRegions().Single();
        AssertEqual(
            new UiRect(0, 0, 320, 180),
            region.Bounds,
            "A host-provided client size must define the renderer bounds independently of the Duxel viewport size.");
    }

    private static void EffectFlushAndCleanup()
    {
        ProbeComponent.Reset();
        using var context = CreateContext();
        var screen = new NuriDuxelScreen(new ProbeComponent(), () => { }, "effect-test");

        RenderFrame(context, screen);
        AssertSequence(
            new[] { "render:0", "effect:0" },
            ProbeComponent.Log,
            "Duxel effects must flush after the initial frame projection.");

        screen.Dispose();
        AssertEqual(
            "cleanup:0",
            ProbeComponent.Log[^1],
            "Disposing the Duxel screen must clean the mounted effect.");
    }

    private static void GridRowsAdvanceByTallestCell()
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new UnevenGridComponent(),
            () => { },
            "grid-row-test");
        var probe = new CursorProbeScreen(screen);

        RenderFrame(context, probe);

        AssertTrue(
            probe.Cursor.Y >= 200f,
            $"Grid rows must advance by the tallest cell in each row. Cursor Y was {probe.Cursor.Y}.");
    }

    private static void GridHasNoImplicitTrackSpacing()
    {
        var defaultBottom = RenderGridBottom(new GridSpacingComponent(null), "grid-spacing-default-test");
        var explicitBottom = RenderGridBottom(new GridSpacingComponent(6), "grid-spacing-explicit-test");

        AssertEqual(
            6f,
            explicitBottom - defaultBottom,
            "An explicit RowSpacing must be measured from zero rather than Duxel's default item spacing.");
    }

    private static void ScrollProjectsOneSpacedColumnChild()
    {
        var withoutSpacing = RenderScrollMaxOffset(0, "scroll-spacing-zero-test");
        var withSpacing = RenderScrollMaxOffset(18, "scroll-spacing-explicit-test");

        AssertEqual(
            18f,
            withSpacing - withoutSpacing,
            "Spacing on the Scroll's single Column child must contribute exactly once.");
    }

    private static float RenderScrollMaxOffset(double spacing, string rootId)
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new ScrollSpacingComponent(spacing),
            () => { },
            rootId,
            input);

        RenderFrame(context, screen);
        return input.GetScrollRegions().Single().MaxOffset;
    }

    private static void ScrollClipsDeferredPanelDecorations()
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new DecoratedScrollComponent(),
            () => { },
            "scroll-decoration-clip-test",
            input);

        RenderFrame(context, screen);
        var initial = input.GetScrollRegions().Single();
        AssertTrue(initial.MaxOffset > 0f, "The decorated Scroll test must contain overflow.");

        var handleCenter = Center(initial.ScrollbarHandle);
        var trackBottom = new UiVector2(
            initial.ScrollbarTrack.X + (initial.ScrollbarTrack.Width * 0.5f),
            initial.ScrollbarTrack.Y + initial.ScrollbarTrack.Height - 1f);
        input.Enqueue(1, DuxelInputEventKind.PointerDown, handleCenter, code: 0, capturedByNuri: true);
        input.Enqueue(2, DuxelInputEventKind.PointerMove, trackBottom, capturedByNuri: true);
        input.Enqueue(3, DuxelInputEventKind.PointerUp, trackBottom, code: 0, capturedByNuri: true);

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var region = input.GetScrollRegions().Single();
            AssertTrue(
                MathF.Abs(region.Offset - region.MaxOffset) <= 1f,
                "The decorated Scroll test must inspect the maximum offset.");
            var contentBounds = new UiRect(
                region.Bounds.X + 2f,
                region.Bounds.Y + 2f,
                MathF.Max(0f, region.Bounds.Width - 4f),
                MathF.Max(0f, region.Bounds.Height - 4f));
            var decorations = drawData.DrawLists
                .SelectMany(drawList => drawList.Commands)
                .Where(command => command.Kind == UiDrawCommandKind.RectFilledPrimitives
                    && command.HasBounds
                    && command.Bounds.Width >= 150f
                    && command.Bounds.Height >= 40f
                    && (command.Bounds.Y < contentBounds.Y
                        || command.Bounds.Y + command.Bounds.Height > contentBounds.Y + contentBounds.Height))
                .ToArray();

            AssertTrue(
                decorations.Length > 0,
                "The test must include a deferred panel decoration crossing a Scroll boundary.");
            AssertTrue(
                decorations.All(command => Contains(contentBounds, command.ClipRect)),
                "Every deferred panel decoration must retain the Scroll content clip rect.");
        }
        finally
        {
            drawData.ReleasePooled();
        }
    }

    private static void FixedGridTracksDoNotExpand()
    {
        var normalBottom = RenderGridBottom(new FixedTrackOverflowComponent(40), "grid-fixed-track-normal-test");
        var overflowBottom = RenderGridBottom(new FixedTrackOverflowComponent(100), "grid-fixed-track-overflow-test");

        AssertEqual(
            normalBottom,
            overflowBottom,
            "Pixel Grid tracks must keep their assigned size when a child draws beyond its cell.");
    }

    private static void ScrollbarRespectsViewportCorners()
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new DecoratedScrollComponent(),
            () => { },
            "scrollbar-corner-test",
            input);

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var region = input.GetScrollRegions().Single();

            AssertTrue(region.MaxOffset > 0f, "The Scroll test must contain overflow.");
            AssertTrue(
                region.ScrollbarTrack.X > region.Bounds.X
                    && region.ScrollbarTrack.Y > region.Bounds.Y
                    && region.ScrollbarTrack.X + region.ScrollbarTrack.Width
                        < region.Bounds.X + region.Bounds.Width
                    && region.ScrollbarTrack.Y + region.ScrollbarTrack.Height
                        < region.Bounds.Y + region.Bounds.Height,
                "The scrollbar track must stay inside the viewport edges so it cannot square off rounded corners.");

            var squareScrollbarPrimitives = drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .Where(primitive => primitive.Rect.Equals(region.ScrollbarTrack)
                    || primitive.Rect.Equals(region.ScrollbarHandle))
                .ToArray();
            AssertTrue(
                squareScrollbarPrimitives.Length == 0,
                "The scrollbar track and handle must use rounded geometry instead of square filled rectangles.");
        }
        finally
        {
            drawData.ReleasePooled();
        }
    }

    private static void ImplicitGridRowFillsArrangedHeight()
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new ImplicitRowComponent(),
            () => { },
            "grid-implicit-row-test",
            input);

        RenderFrame(context, screen);

        var region = input.GetScrollRegions().Single();
        AssertEqual(
            200f,
            region.Bounds.Height,
            "A Grid without RowDefinitions must arrange its single implicit row across the available height.");
    }

    private static void MeasuredAutoRowReducesRemainingStarHeight()
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new AutoStarAutoComponent(),
            () => { },
            "grid-auto-star-auto-test",
            input);

        RenderFrame(context, screen);

        var footer = input.GetScrollRegions().MaxBy(region => region.Order);
        AssertEqual(
            200f,
            footer.Bounds.Y + footer.Bounds.Height,
            "A measured Auto row must take space back from a following Star row instead of extending the Grid.");
    }

    private static float RenderGridBottom(Component component, string rootId)
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(component, () => { }, rootId);
        var probe = new CursorProbeScreen(screen);
        RenderFrame(context, probe);
        return probe.Cursor.Y;
    }

    private static void DirtyStateRequestsAndProjectsFrame()
    {
        ProbeComponent.Reset();
        using var context = CreateContext();
        var requestedFrames = 0;
        using var screen = new NuriDuxelScreen(
            new ProbeComponent(),
            () => requestedFrames++,
            "state-test");

        RenderFrame(context, screen);
        ProbeComponent.Update!(1);
        AssertEqual(1, requestedFrames, "A state change must request a Duxel frame.");

        RenderFrame(context, screen);
        AssertSequence(
            new[] { "render:0", "effect:0", "render:1", "cleanup:0", "effect:1" },
            ProbeComponent.Log,
            "A dirty component must rerender and replace its effect after frame projection.");
    }

    private static void FullRebuildRequestsAndProjectsFrame()
    {
        ProbeComponent.Reset();
        using var context = CreateContext();
        var requestedFrames = 0;
        using var screen = new NuriDuxelScreen(
            new ProbeComponent(),
            () => requestedFrames++,
            "full-rebuild-test");

        RenderFrame(context, screen);
        ProbeComponent.Update!(1);
        RenderFrame(context, screen);
        requestedFrames = 0;
        screen.RequestFullRebuild();
        AssertEqual(1, requestedFrames, "A hot-reload rebuild must request a Duxel frame.");

        RenderFrame(context, screen);
        AssertEqual(
            2,
            ProbeComponent.Log.Count(item => item == "render:1"),
            "A requested full rebuild must invoke the root component again while preserving state.");
    }

    private static void RuntimeThemeChangesRequestAndApply()
    {
        using var context = CreateContext();
        var requestedFrames = 0;
        UiTheme? observedTheme = null;
        using var screen = new NuriDuxelScreen(
            new ViewportRootComponent(),
            () => requestedFrames++,
            "theme-test",
            themeObserver: theme => observedTheme = theme);

        RenderFrame(context, screen);
        AssertEqual(
            context.GetStyle().WindowBg,
            screen.CurrentTheme!.Value.WindowBg,
            "The screen must expose the theme applied to its current frame.");
        AssertEqual(
            context.GetStyle().WindowBg,
            observedTheme!.Value.WindowBg,
            "A host observer must receive the theme applied to the current frame.");
        screen.RequestTheme(UiTheme.Nord);
        AssertEqual(1, requestedFrames, "A runtime theme change must request a Duxel frame.");

        RenderFrame(context, screen);
        AssertTrue(
            context.GetStyle().WindowBg != UiTheme.Nord.WindowBg,
            "Duxel applies a requested theme at the next frame boundary, not during the requesting frame.");

        RenderFrame(context, screen);
        AssertEqual(
            UiTheme.Nord.WindowBg,
            context.GetStyle().WindowBg,
            "A requested Duxel theme must become active on the following frame.");
        AssertEqual(
            UiTheme.Nord.WindowBg,
            screen.CurrentTheme!.Value.WindowBg,
            "CurrentTheme must advance only after Duxel applies the requested palette.");
        AssertEqual(
            UiTheme.Nord.WindowBg,
            observedTheme!.Value.WindowBg,
            "The host observer must track the palette Duxel actually applied.");
    }

    private static void DuxelThemeColorsUseNeutralBackgroundDsl()
    {
        var source = new UiColor(0x11, 0x22, 0x33, 0x44);
        var converted = source.ToColorValue();
        AssertEqual((byte)0x44, converted.A, "UiColor alpha must be preserved.");
        AssertEqual((byte)0x11, converted.R, "UiColor red must be preserved.");
        AssertEqual((byte)0x22, converted.G, "UiColor green must be preserved.");
        AssertEqual((byte)0x33, converted.B, "UiColor blue must be preserved.");

        var element = Component.Div().Background(source);
        var brush = (BrushValue.Solid)element.Properties[Nuri.Constants.PropertyKeys.Background];
        AssertEqual(converted, brush.Color, "Background(UiColor) must use the neutral ColorValue property path.");
    }

    private static void SharedExplorerTreeProjectsHeadlessly()
    {
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        using var context = CreateContext();
        var requestedFrames = 0;
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new ExplorerTreeComponent(),
            () => requestedFrames++,
            "explorer-test",
            input);
        var probe = new ViewportProbeScreen(screen, 48f);
        var explorerFrame = new UiFrameInfo(
            1f / 60f,
            new UiVector2(1120, 720),
            new UiVector2(1, 1));

        RenderFrame(context, probe, explorerFrame);
        AssertTrue(
            requestedFrames > 0,
            "Explorer mount effects must route state invalidation through the Duxel frame scheduler.");
        AssertEqual(
            new UiVector2(1120, 672),
            probe.Size,
            "Explorer must receive the real Duxel work area for its configured sample size.");
        AssertEqual(
            2,
            input.GetScrollRegions().Count,
            "The Explorer Windows path must publish independent tree and detail Scroll hit regions.");
        AssertTrue(
            probe.LastItemMax.Y <= explorerFrame.DisplaySize.Y + 1f,
            $"Explorer Grid projection must remain within the viewport. Last item max Y was {probe.LastItemMax.Y}.");
        AssertTrue(
            NuriDiagnostics.GetSnapshot().RecentLogs.Any(log =>
                log.Kind == RuntimeLogKind.UnsupportedProperty
                && log.Message.Contains("FontWeight", StringComparison.Ordinal)),
            "Explorer projection must diagnose its unsupported FontWeight usage.");
        AssertTrue(
            NuriDiagnostics.GetSnapshot().RecentLogs.All(log =>
                !log.Message.Contains("RowDefinitions", StringComparison.Ordinal)),
            "Duxel Grid row definitions must be materialized without an unsupported warning.");

        var resizedExplorerFrame = new UiFrameInfo(
            1f / 60f,
            new UiVector2(800, 500),
            new UiVector2(1, 1));
        RenderFrame(context, probe, resizedExplorerFrame);
        AssertEqual(
            new UiVector2(800, 452),
            probe.Size,
            "Explorer must recalculate the Duxel work area after a window resize.");
        AssertTrue(
            probe.LastItemMax.Y <= resizedExplorerFrame.DisplaySize.Y + 1f,
            $"Resized Explorer projection must remain within the viewport. Last item max Y was {probe.LastItemMax.Y}.");
    }

    private static void SharedAnimatedDashboardProjectsHeadlessly()
    {
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new AnimatedDashboardComponent(),
            () => { },
            "dashboard-test");
        var probe = new ViewportProbeScreen(screen, 48f);
        var dashboardFrame = new UiFrameInfo(
            1f / 60f,
            new UiVector2(900, 640),
            new UiVector2(1, 1));

        RenderFrame(context, probe, dashboardFrame);
        AssertEqual(
            new UiVector2(900, 592),
            probe.Size,
            "Dashboard must receive the title-bar-excluded Duxel work area.");
        AssertTrue(
            probe.LastItemMax.Y <= dashboardFrame.DisplaySize.Y + 1f,
            $"Dashboard root Scroll must remain within the viewport. Last item max Y was {probe.LastItemMax.Y}.");
        var logs = NuriDiagnostics.GetSnapshot().RecentLogs;
        AssertTrue(
            logs.Any(log => log.Message.Contains("Animation:Background", StringComparison.Ordinal)),
            "The shared Dashboard must diagnose its not-yet-supported background transition.");
        AssertTrue(
            logs.All(log => !log.Message.Contains("Animation:Opacity", StringComparison.Ordinal)),
            "The shared Dashboard opacity transition must be materialized without an unsupported warning.");

        var resizedDashboardFrame = new UiFrameInfo(
            1f / 60f,
            new UiVector2(700, 480),
            new UiVector2(1, 1));
        RenderFrame(context, probe, resizedDashboardFrame);
        AssertEqual(
            new UiVector2(700, 432),
            probe.Size,
            "Dashboard must recalculate the Duxel work area after a window resize.");
        AssertTrue(
            probe.LastItemMax.Y <= resizedDashboardFrame.DisplaySize.Y + 1f,
            $"Resized Dashboard Scroll must remain within the viewport. Last item max Y was {probe.LastItemMax.Y}.");
    }

    private static void DiagnosticsTrackDuxelRootsAndPatches()
    {
        ProbeComponent.Reset();
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        using var context = CreateContext();
        var screen = new NuriDuxelScreen(new ProbeComponent(), () => { }, "diagnostics-test");

        RenderFrame(context, screen);
        var root = NuriDiagnostics.GetSnapshot().Roots.Single(item => item.Renderer == "Duxel");

        ProbeComponent.Update!(1);
        RenderFrame(context, screen);
        var updated = NuriDiagnostics.GetSnapshot().Roots.Single(item => item.RootId == root.RootId);
        AssertTrue(updated.LastPatchCount > 0, "Duxel diagnostics must record the dirty subtree patch batch.");

        screen.Dispose();
        AssertTrue(
            NuriDiagnostics.GetSnapshot().Roots.All(item => item.RootId != root.RootId),
            "Disposing a Duxel screen must unregister its diagnostics root.");
    }

    private static void OpacityAnimationRequestsFramesAndSupportsInterruption()
    {
        OpacityProbeComponent.Reset();
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        using var context = CreateContext();
        var requestedFrames = 0;
        using var screen = new NuriDuxelScreen(
            new OpacityProbeComponent(),
            () => requestedFrames++,
            "opacity-animation-test");

        RenderFrame(context, screen);
        AssertTrue(!screen.HasActiveAnimations, "The initial opacity target must materialize without animation.");

        OpacityProbeComponent.Update!(0.2);
        RenderFrame(context, screen);
        AssertTrue(screen.HasActiveAnimations, "Changing opacity must start a Duxel animation track.");
        AssertTrue(requestedFrames >= 2, "An active opacity animation must request a continuation frame.");

        RenderFrame(context, screen);
        OpacityProbeComponent.Update!(0.8);
        RenderFrame(context, screen);
        AssertTrue(screen.HasActiveAnimations, "Replacing the opacity target must keep the track active.");

        for (var frame = 0; frame < 120 && screen.HasActiveAnimations; frame++)
        {
            RenderFrame(context, screen);
        }

        AssertTrue(!screen.HasActiveAnimations, "The replaced opacity animation must converge to its latest target.");
        AssertTrue(
            NuriDiagnostics.GetSnapshot().RecentLogs.All(log =>
                !log.Message.Contains("Animation:Opacity", StringComparison.Ordinal)),
            "Supported opacity animations must not emit unsupported-property diagnostics.");
    }

    private static void InputQueuePreservesSemanticEventOrder()
    {
        var input = new DuxelInputEventQueue();
        input.Enqueue(1, DuxelInputEventKind.PointerMove, new UiVector2(1, 1));
        input.Enqueue(2, DuxelInputEventKind.PointerMove, new UiVector2(2, 2));
        input.Enqueue(3, DuxelInputEventKind.PointerDown, new UiVector2(2, 2), code: 0);
        input.Enqueue(4, DuxelInputEventKind.PointerUp, new UiVector2(2, 2), code: 0);
        input.Enqueue(5, DuxelInputEventKind.Wheel, new UiVector2(10, 20), new UiVector2(0, -1));
        input.Enqueue(6, DuxelInputEventKind.Wheel, new UiVector2(30, 40), new UiVector2(0, -1));
        input.Enqueue(7, DuxelInputEventKind.Resize, delta: new UiVector2(1120, 720));
        input.Enqueue(8, DuxelInputEventKind.Resize, delta: new UiVector2(1240, 810));

        var events = input.Drain();
        AssertSequence(
            new[] { "PointerMove", "PointerDown", "PointerUp", "Wheel", "Wheel", "Resize" },
            events.Select(item => item.Kind.ToString()).ToArray(),
            "Only consecutive pointer moves and resize samples may coalesce; semantic transitions and wheel samples must remain ordered.");
        AssertEqual(new UiVector2(2, 2), events[0].Position, "Pointer move coalescing must retain the latest sample.");
        AssertEqual(new UiVector2(10, 20), events[3].Position, "The first wheel event must retain its own position.");
        AssertEqual(new UiVector2(30, 40), events[4].Position, "The second wheel event must retain its own position.");
        AssertEqual(new UiVector2(1240, 810), events[5].Delta, "Resize coalescing must retain the latest predictive client size.");
        AssertTrue(
            events.Zip(events.Skip(1), (left, right) => left.Sequence < right.Sequence).All(value => value),
            "Drained input events must retain their original sequence order.");
    }

    private static void WheelRoutesIndependentlyToNuriScrollRegions()
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        var frameRequests = 0;
        using var screen = new NuriDuxelScreen(
            new TwoScrollRegionsComponent(),
            () => frameRequests++,
            "scroll-input-test",
            input);

        RenderFrame(context, screen);
        var initial = input.GetScrollRegions().OrderBy(region => region.Bounds.X).ToArray();
        AssertEqual(2, initial.Length, "The test component must publish two independent Nuri scroll regions.");
        AssertTrue(initial.All(region => region.MaxOffset > 0f), "Both test regions must contain overflow.");

        var leftPosition = Center(initial[0].Bounds);
        var rightPosition = Center(initial[1].Bounds);
        AssertTrue(input.ShouldCaptureWheel(leftPosition), "The left Scroll hit-map must capture wheel input.");
        AssertTrue(input.ShouldCaptureWheel(rightPosition), "The right Scroll hit-map must capture wheel input.");
        input.Enqueue(
            1,
            DuxelInputEventKind.Wheel,
            leftPosition,
            new UiVector2(0, -1),
            capturedByNuri: input.ShouldCaptureWheel(leftPosition));
        input.Enqueue(
            2,
            DuxelInputEventKind.Wheel,
            rightPosition,
            new UiVector2(0, -1),
            capturedByNuri: input.ShouldCaptureWheel(rightPosition));
        input.Enqueue(
            3,
            DuxelInputEventKind.Wheel,
            rightPosition,
            new UiVector2(0, -1),
            capturedByNuri: input.ShouldCaptureWheel(rightPosition));
        input.Enqueue(
            4,
            DuxelInputEventKind.Wheel,
            leftPosition,
            new UiVector2(0, 1),
            capturedByNuri: input.ShouldCaptureWheel(leftPosition));

        RenderFrame(context, screen);
        var updated = input.GetScrollRegions().OrderBy(region => region.Bounds.X).ToArray();
        AssertTrue(updated[0].Offset > 0f, "The first direction of a rapid reversal must remain visible for at least one frame.");
        AssertTrue(updated[1].Offset > updated[0].Offset, "Wheel samples must retain their event-time Scroll region while the cursor moves between regions.");
        AssertTrue(frameRequests > 0, "A deferred opposite wheel direction must request a continuation frame.");
        AssertTrue(screen.HasActiveScrollMotion, "Wheel input must start inertial Scroll motion.");

        RenderFrame(context, screen);
        var reversed = input.GetScrollRegions().OrderBy(region => region.Bounds.X).ToArray();
        AssertTrue(
            reversed[0].Offset < updated[0].Offset,
            "The deferred opposite wheel direction must decelerate or reverse the existing velocity on the continuation frame.");
        AssertTrue(reversed[1].Offset > updated[1].Offset, "The other Scroll region must continue its independent inertial motion.");

        for (var frame = 0; frame < 180 && screen.HasActiveScrollMotion; frame++)
        {
            RenderFrame(context, screen);
        }

        var settled = input.GetScrollRegions().OrderBy(region => region.Bounds.X).ToArray();
        AssertTrue(!screen.HasActiveScrollMotion, "Inertial Scroll motion must settle after friction removes its velocity.");
        AssertTrue(
            settled.All(region => region.Offset >= 0f && region.Offset <= region.MaxOffset),
            "Inertial Scroll motion must remain clamped to every region's bounds.");

        var handleCenter = Center(settled[0].ScrollbarHandle);
        var trackBottom = new UiVector2(
            settled[0].ScrollbarTrack.X + (settled[0].ScrollbarTrack.Width * 0.5f),
            settled[0].ScrollbarTrack.Y + settled[0].ScrollbarTrack.Height - 1f);
        AssertTrue(input.ShouldCapturePointer(handleCenter), "The Nuri scrollbar handle must own pointer drag input.");
        input.Enqueue(5, DuxelInputEventKind.PointerDown, handleCenter, code: 0, capturedByNuri: true);
        input.Enqueue(6, DuxelInputEventKind.PointerMove, trackBottom, capturedByNuri: true);
        input.Enqueue(7, DuxelInputEventKind.PointerUp, trackBottom, code: 0, capturedByNuri: true);

        RenderFrame(context, screen);
        var dragged = input.GetScrollRegions().OrderBy(region => region.Bounds.X).ToArray();
        AssertTrue(
            MathF.Abs(dragged[0].Offset - dragged[0].MaxOffset) <= 1f,
            "Dragging the Nuri scrollbar handle to the track end must reach the maximum offset.");
        AssertTrue(
            input.ShouldCaptureWheel(leftPosition),
            "Host capture must not use the previous frame's boundary state; ordered renderer routing decides whether each direction can move.");
    }

    private static UiVector2 Center(UiRect bounds)
    {
        return new UiVector2(bounds.X + (bounds.Width * 0.5f), bounds.Y + (bounds.Height * 0.5f));
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

    private static void RenderFrame(UiContext context, UiScreen screen)
    {
        RenderFrame(context, screen, FrameInfo);
    }

    private static void RenderFrame(UiContext context, UiScreen screen, UiFrameInfo frameInfo)
    {
        context.SetClipRect(new UiRect(0, 0, frameInfo.DisplaySize.X, frameInfo.DisplaySize.Y));
        context.NewFrame(frameInfo);
        context.Render(screen);
        _ = context.GetDrawData();
    }

    private static UiDrawData RenderFrameWithDrawData(
        UiContext context,
        UiScreen screen,
        UiFrameInfo frameInfo)
    {
        context.SetClipRect(new UiRect(0, 0, frameInfo.DisplaySize.X, frameInfo.DisplaySize.Y));
        context.NewFrame(frameInfo);
        context.Render(screen);
        return context.GetDrawData();
    }

    private static bool Contains(UiRect outer, UiRect inner)
    {
        const float epsilon = 0.01f;
        return inner.X >= outer.X - epsilon
            && inner.Y >= outer.Y - epsilon
            && inner.X + inner.Width <= outer.X + outer.Width + epsilon
            && inner.Y + inner.Height <= outer.Y + outer.Height + epsilon;
    }

    private static UiFontAtlas CreateFontAtlas()
    {
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

    private static void AssertSequence(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual,
        string message)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}].");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class ProbeComponent : Component
    {
        public static List<string> Log { get; } = new();

        public static Action<int>? Update { get; private set; }

        public static void Reset()
        {
            Log.Clear();
            Update = null;
        }

        public override IElement Render()
        {
            var (value, setValue) = useState(0);
            Update = next => setValue(_ => next);
            Log.Add($"render:{value}");

            useEffect(() =>
            {
                Log.Add($"effect:{value}");
                return () => Log.Add($"cleanup:{value}");
            }, value);

            return Div(Text($"value:{value}"))
                .Padding(8)
                .Margin(top: 4, bottom: 4)
                .Background("#111827")
                .Brush("#334155")
                .Thickness(1)
                .CornerRadius(6);
        }
    }

    private sealed class OpacityProbeComponent : Component
    {
        public static Action<double>? Update { get; private set; }

        public static void Reset()
        {
            Update = null;
        }

        public override IElement Render()
        {
            var (opacity, setOpacity) = useState(1.0);
            Update = next => setOpacity(_ => next);

            return Div(Text("Animated opacity"))
                .Padding(12)
                .Background("#1d4ed8")
                .Opacity(opacity)
                .Transition(TimeSpan.FromMilliseconds(200), EasingValue.CubicOut);
        }
    }

    private sealed class ViewportProbeScreen(UiScreen content, float topInset = 0f) : UiScreen
    {
        public UiVector2 Position { get; private set; }

        public UiVector2 Size { get; private set; }

        public UiVector2 Cursor { get; private set; }

        public UiVector2 LastItemMax { get; private set; }

        public override void Render(UiImmediateContext ui)
        {
            ui.SetViewportTopInset(topInset);
            ui.EnableRootViewportContentLayout();
            content.Render(ui);
            Position = ui.GetWindowPos();
            Size = ui.GetWindowSize();
            Cursor = ui.GetCursorPos();
            LastItemMax = ui.GetItemRectMax();
        }
    }

    private sealed class CursorProbeScreen(UiScreen content) : UiScreen
    {
        public UiVector2 Cursor { get; private set; }

        public override void Render(UiImmediateContext ui)
        {
            content.Render(ui);
            Cursor = ui.GetCursorPos();
        }
    }

    private sealed class ViewportRootComponent : Component
    {
        public override IElement Render()
        {
            return Div(Text("viewport root")).Padding(8);
        }
    }

    private sealed class ImplicitViewportComponent : Component
    {
        public override IElement Render()
        {
            return Grid(Div(DivTypes.Scroll, Text("content")))
                .Columns(Star);
        }
    }

    private sealed class UnevenGridComponent : Component
    {
        public override IElement Render()
        {
            return Grid(
                    Button("short-a").Size(80, 20).Row(0).Column(0),
                    Button("tall-a").Size(80, 100).Row(0).Column(1),
                    Button("tall-b").Size(80, 100).Row(1).Column(0),
                    Button("short-b").Size(80, 20).Row(1).Column(1))
                .Rows(Auto, Auto)
                .Columns(Star, Star);
        }
    }

    private sealed class GridSpacingComponent(double? rowSpacing) : Component
    {
        public override IElement Render()
        {
            var grid = Grid(
                    Button("first").Size(80, 20).Row(0),
                    Button("second").Size(80, 20).Row(1))
                .Rows(Pixels(20), Pixels(20))
                .Columns(Pixels(100));
            return rowSpacing is double spacing ? grid.RowSpacing(spacing) : grid;
        }
    }

    private sealed class ScrollSpacingComponent(double spacing) : Component
    {
        public override IElement Render()
        {
            return Div(
                    DivTypes.Scroll,
                    Div(
                            Button("first").Size(80, 30),
                            Button("second").Size(80, 30))
                        .Spacing(spacing))
                .Height(40);
        }
    }

    private sealed class FixedTrackOverflowComponent(double firstChildHeight) : Component
    {
        public override IElement Render()
        {
            return Grid(
                    Button("overflow").Size(80, firstChildHeight).Row(0),
                    Button("footer").Size(80, 20).Row(1))
                .Rows(Pixels(40), Pixels(20))
                .Columns(Pixels(100));
        }
    }

    private sealed class ImplicitRowComponent : Component
    {
        public override IElement Render()
        {
            return Grid(
                    Div(DivTypes.Scroll, Text("content")).Column(0))
                .Columns(Pixels(100))
                .Size(100, 200);
        }
    }

    private sealed class AutoStarAutoComponent : Component
    {
        public override IElement Render()
        {
            return Grid(
                    Div(
                            Text("Header").FontSize(28),
                            Text("Subtitle").FontSize(13).Margin(top: 6, bottom: 18))
                        .Row(0),
                    Div(DivTypes.Scroll, Text("main")).Row(1),
                    Div(DivTypes.Scroll, Text("footer")).Height(20).Row(2))
                .Rows(Auto, Star, Auto)
                .Columns(Pixels(200))
                .Size(200, 200);
        }
    }

    private sealed class DecoratedScrollComponent : Component
    {
        public override IElement Render()
        {
            return Div(
                    Text("Header").Margin(bottom: 60),
                    Div(
                            DivTypes.Scroll,
                            Div(
                                    DecoratedPanel("top"),
                                    DecoratedPanel("middle"),
                                    DecoratedPanel("bottom"))
                                .Spacing(0))
                        .Size(200, 120))
                .Spacing(0);
        }

        private static IElement DecoratedPanel(string label)
        {
            return Div(Text(label))
                .Padding(30)
                .Background("#7c3aed");
        }
    }

    private sealed class TwoScrollRegionsComponent : Component
    {
        public override IElement Render()
        {
            return Grid(
                    ScrollColumn("left").Column(0),
                    ScrollColumn("right").Column(1))
                .Columns(Pixels(220), Pixels(220));
        }

        private static IElement ScrollColumn(string prefix)
        {
            var children = Enumerable.Range(0, 30)
                .Select(index => (IElement)Text($"{prefix}-{index}").Height(24))
                .ToArray();
            return Div(DivTypes.Scroll, Div(children)).Size(200, 120);
        }
    }
}
