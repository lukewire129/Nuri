using Duxel.Core;
using Nuri.AnimatedDashboardSample.Components;
using Nuri.Duxel;
using Nuri.ExplorerTreeSample.Components;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Navigation;
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
            ("preview content scale is applied to the Duxel context", PreviewContentScaleIsApplied),
            ("grid rows advance by their tallest cell", GridRowsAdvanceByTallestCell),
            ("grid has no implicit track spacing", GridHasNoImplicitTrackSpacing),
            ("auto grid padding centers an equal-height row and button", AutoGridPaddingCentersEqualHeightControls),
            ("grid applies horizontal and vertical element alignment", GridAppliesElementAlignment),
            ("row applies child vertical alignment", RowAppliesChildVerticalAlignment),
            ("column applies child horizontal alignment", ColumnAppliesChildHorizontalAlignment),
            ("button content alignment is supported without diagnostics", ButtonContentAlignmentIsSupported),
            ("auto grid columns measure nested div content", AutoGridColumnsMeasureNestedDivContent),
            ("decorated grid preserves bottom padding before its sibling", DecoratedGridPreservesBottomPaddingBeforeSibling),
            ("header grid preserves visible bottom padding", HeaderGridPreservesVisibleBottomPadding),
            ("scroll projects one spaced Column child", ScrollProjectsOneSpacedColumnChild),
            ("scroll clips deferred panel decorations", ScrollClipsDeferredPanelDecorations),
            ("nested panel backgrounds preserve painter order", NestedPanelBackgroundsPreservePainterOrder),
            ("scrollbar respects viewport corners", ScrollbarRespectsViewportCorners),
            ("fixed grid tracks do not expand to overflowing content", FixedGridTracksDoNotExpand),
            ("implicit grid row fills its arranged height", ImplicitGridRowFillsArrangedHeight),
            ("measured auto row reduces remaining star height", MeasuredAutoRowReducesRemainingStarHeight),
            ("dirty state requests and projects a frame", DirtyStateRequestsAndProjectsFrame),
            ("full rebuild requests and projects a frame", FullRebuildRequestsAndProjectsFrame),
            ("root replacement preserves or resets state", RootReplacementPreservesOrResetsState),
            ("runtime theme changes request and apply on the next frame", RuntimeThemeChangesRequestAndApply),
            ("Duxel theme colors use the neutral Background DSL", DuxelThemeColorsUseNeutralBackgroundDsl),
            ("opacity animation requests frames and supports interruption", OpacityAnimationRequestsFramesAndSupportsInterruption),
            ("standard router projects route-local state updates", StandardRouterProjectsRouteLocalStateUpdates),
            ("diagnostics track Duxel roots and patches", DiagnosticsTrackDuxelRootsAndPatches),
            ("diagnostics can exclude an inspector screen", DiagnosticsCanExcludeInspectorScreen),
            ("input queue preserves semantic event order", InputQueuePreservesSemanticEventOrder),
            ("virtualized items project bounded viewport rows", VirtualizedItemsProjectBoundedViewportRows),
            ("virtualized items clip rows before the scrollbar", VirtualizedItemsClipRowsBeforeScrollbar),
            ("measured virtualized items learn variable row heights", MeasuredVirtualizedItemsLearnVariableRowHeights),
            ("measured decorated rows stay aligned without overlap", MeasuredDecoratedRowsStayAlignedWithoutOverlap),
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

    private static void PreviewContentScaleIsApplied()
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new ViewportRootComponent(),
            () => { },
            "content-scale-test",
            contentScaleProvider: () => 1.75f);
        var probe = new ContentScaleProbeScreen(screen);

        RenderFrame(context, probe);

        AssertTrue(
            MathF.Abs(probe.ContentScale - 1.75f) <= 0.01f,
            $"The preview scale must be applied to the Duxel context. Actual: {probe.ContentScale}.");
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

    private static void GridAppliesElementAlignment()
    {
        var buttons = RenderAlignmentButtons(
            new GridElementAlignmentComponent(),
            "grid-element-alignment-test");

        AssertEqual(3, buttons.Length, "The Grid alignment probe must render three buttons.");
        var origin = buttons[0].Rect;
        AssertEqual(origin.X + 140f, buttons[1].Rect.X, "HCenter must center a 20px child in its 100px Grid cell.");
        AssertEqual(origin.Y + 40f, buttons[1].Rect.Y, "VCenter must center a 20px child in its 100px Grid cell.");
        AssertEqual(origin.X + 280f, buttons[2].Rect.X, "End must right-align a 20px child in its 100px Grid cell.");
        AssertEqual(origin.Y + 80f, buttons[2].Rect.Y, "Bottom must bottom-align a 20px child in its 100px Grid cell.");
    }

    private static void AutoGridPaddingCentersEqualHeightControls()
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new AutoGridPaddingAlignmentComponent(),
            () => { },
            "auto-grid-padding-alignment-test");
        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var fills = drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .ToArray();
            var parents = fills
                .Where(primitive =>
                    primitive.Rect.Width > 300f
                    && MathF.Abs(primitive.Rect.Height - 48f) <= 0.01f)
                .ToArray();
            AssertEqual(
                1,
                parents.Length,
                $"The natural-height toolbar must measure 48px. Fills: {string.Join(" | ", fills.Select(primitive => primitive.Rect))}");
            var parent = parents[0];
            var buttons = fills
                .Where(primitive =>
                    MathF.Abs(primitive.Rect.Width - 100f) <= 0.01f
                    && MathF.Abs(primitive.Rect.Height - 32f) <= 0.01f)
                .ToArray();

            AssertEqual(3, buttons.Length, "The natural-height toolbar must render its three 32px buttons.");
            foreach (var button in buttons)
            {
                AssertEqual(
                    parent.Rect.Y + 8f,
                    button.Rect.Y,
                    "Auto sizing must place each button after the 8px top padding.");
                AssertEqual(
                    parent.Rect.Y + parent.Rect.Height - 8f,
                    button.Rect.Y + button.Rect.Height,
                    "Auto sizing must preserve the matching 8px bottom padding.");
            }
        }
        finally
        {
            drawData.ReleasePooled();
        }
    }

    private static void RowAppliesChildVerticalAlignment()
    {
        var buttons = RenderAlignmentButtons(
            new RowChildAlignmentComponent(),
            "row-child-alignment-test");

        AssertEqual(3, buttons.Length, "The Row alignment probe must render three buttons.");
        var originY = buttons[0].Rect.Y;
        AssertEqual(originY + 40f, buttons[1].Rect.Y, "VCenter must center a Row child in the Row height.");
        AssertEqual(originY + 80f, buttons[2].Rect.Y, "Bottom must align a Row child to the Row bottom.");
    }

    private static void ColumnAppliesChildHorizontalAlignment()
    {
        var buttons = RenderAlignmentButtons(
            new ColumnChildAlignmentComponent(),
            "column-child-alignment-test");

        AssertEqual(3, buttons.Length, "The Column alignment probe must render three buttons.");
        var originX = buttons[0].Rect.X;
        AssertEqual(originX + 40f, buttons[1].Rect.X, "HCenter must center a Column child in the Column width.");
        AssertEqual(originX + 80f, buttons[2].Rect.X, "End must align a Column child to the Column end.");
    }

    private static void ButtonContentAlignmentIsSupported()
    {
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        try
        {
            using var context = CreateContext();
            using var screen = new NuriDuxelScreen(
                new ButtonContentAlignmentComponent(),
                () => { },
                "button-content-alignment-test");

            RenderFrame(context, screen);

            AssertTrue(
                NuriDiagnostics.GetSnapshot().RecentLogs.All(log =>
                    log.Kind != RuntimeLogKind.UnsupportedProperty
                    || !log.Message.Contains("Alignment", StringComparison.Ordinal)),
                "Button horizontal and vertical content alignment must not emit unsupported diagnostics.");
        }
        finally
        {
            NuriDiagnostics.Disable();
            NuriDiagnostics.ClearLogs();
        }
    }

    private static UiRectFilledPrimitive[] RenderAlignmentButtons(Component component, string rootId)
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(component, () => { }, rootId);
        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            return drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .Where(primitive =>
                    MathF.Abs(primitive.Rect.Width - 20f) <= 0.01f
                    && MathF.Abs(primitive.Rect.Height - 20f) <= 0.01f)
                .OrderBy(primitive => primitive.Rect.X)
                .ToArray();
        }
        finally
        {
            drawData.ReleasePooled();
        }
    }

    private static void AutoGridColumnsMeasureNestedDivContent()
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new AutoColumnNestedDivComponent(),
            () => { },
            "grid-auto-column-nested-div-test");

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var autoColumnSurface = drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .Single(primitive => MathF.Abs(primitive.Rect.Height - 30f) <= 0.01f);

            AssertEqual(
                148f,
                autoColumnSurface.Rect.Width,
                "An Auto Grid column must include a nested Div's child width and horizontal padding.");
        }
        finally
        {
            drawData.ReleasePooled();
        }
    }

    private static void DecoratedGridPreservesBottomPaddingBeforeSibling()
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new DecoratedGridBottomPaddingComponent(),
            () => { },
            "grid-bottom-padding-test");

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var fills = drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .ToArray();
            var paddedGrid = fills.Single(primitive =>
                MathF.Abs(primitive.Rect.Width - 200f) <= 0.01f
                && MathF.Abs(primitive.Rect.Height - 56f) <= 0.01f);
            var sibling = fills.Single(primitive =>
                MathF.Abs(primitive.Rect.Width - 200f) <= 0.01f
                && MathF.Abs(primitive.Rect.Height - 24f) <= 0.01f);

            AssertTrue(
                sibling.Rect.Y + 0.01f >= paddedGrid.Rect.Y + paddedGrid.Rect.Height,
                $"A decorated Grid's bottom padding must advance its sibling. Grid: {paddedGrid.Rect}; Sibling: {sibling.Rect}.");
        }
        finally
        {
            drawData.ReleasePooled();
        }
    }

    private static void HeaderGridPreservesVisibleBottomPadding()
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new HeaderGridPaddingComponent(),
            () => { },
            "header-grid-padding-test");

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var fills = drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .ToArray();
            var outer = fills.Single(primitive =>
                MathF.Abs(primitive.Rect.Width - 600f) <= 0.01f
                && primitive.Rect.Height > 24f);
            var inner = fills
                .Where(primitive => primitive.Rect.Width < 600f)
                .MaxBy(primitive => primitive.Rect.Width);
            var sibling = fills.Single(primitive =>
                MathF.Abs(primitive.Rect.Width - 600f) <= 0.01f
                && MathF.Abs(primitive.Rect.Height - 24f) <= 0.01f);
            var bottomGap = outer.Rect.Y + outer.Rect.Height - (inner.Rect.Y + inner.Rect.Height);
            var siblingGap = sibling.Rect.Y - (outer.Rect.Y + outer.Rect.Height);

            AssertEqual(
                18f,
                bottomGap,
                $"The header Grid must keep 18px below its nested surface. Outer: {outer.Rect}; Inner: {inner.Rect}");
            AssertEqual(
                12f,
                siblingGap,
                $"A decorated header must preserve its parent's Spacing before the next sibling. Header: {outer.Rect}; Sibling: {sibling.Rect}");
        }
        finally
        {
            drawData.ReleasePooled();
        }
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

    private static void NestedPanelBackgroundsPreservePainterOrder()
    {
        using var context = CreateContext();
        using var screen = new NuriDuxelScreen(
            new NestedPanelDecorationComponent(),
            () => { },
            "nested-panel-decoration-test");

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var fills = drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .ToArray();
            var parentIndex = Array.FindIndex(
                fills,
                primitive => MathF.Abs(primitive.Rect.Width - 220f) <= 0.01f
                    && MathF.Abs(primitive.Rect.Height - 120f) <= 0.01f);
            var childIndex = Array.FindIndex(
                fills,
                primitive => MathF.Abs(primitive.Rect.Width - 100f) <= 0.01f
                    && MathF.Abs(primitive.Rect.Height - 40f) <= 0.01f);

            AssertTrue(parentIndex >= 0, "The nested decoration test must draw the parent background.");
            AssertTrue(childIndex >= 0, "The nested decoration test must draw the child background.");
            AssertTrue(
                parentIndex < childIndex,
                "A parent background must be painted before its child background so it cannot cover the child surface.");
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

            var scrollbarPrimitives = drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .Where(primitive => primitive.Rect.Equals(region.ScrollbarTrack)
                    || primitive.Rect.Equals(region.ScrollbarHandle))
                .ToArray();
            AssertTrue(
                scrollbarPrimitives.Length == 2
                    && scrollbarPrimitives.All(primitive => primitive.CornerRadius > 0f),
                "The scrollbar track and handle must use filled rectangles with rounded corners.");
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

    private static void RootReplacementPreservesOrResetsState()
    {
        RootReplacementProbeComponent.Reset();
        using var context = CreateContext();
        var requestedFrames = 0;
        using var screen = new NuriDuxelScreen(
            new RootReplacementProbeComponent(),
            () => requestedFrames++,
            "root-replacement-test");

        RenderFrame(context, screen);
        RootReplacementProbeComponent.Update!(7);
        RenderFrame(context, screen);
        AssertEqual(7, RootReplacementProbeComponent.LastValue, "The test state must update before replacement.");

        requestedFrames = 0;
        screen.ReplaceRoot(new RootReplacementProbeComponent(), resetState: false);
        AssertEqual(1, requestedFrames, "Replacing the root must request a Duxel frame.");
        RenderFrame(context, screen);
        AssertEqual(7, RootReplacementProbeComponent.LastValue, "A partial preview replacement must preserve hook state.");
        AssertEqual(1, RootReplacementProbeComponent.MountCount, "A partial replacement must not remount a stable effect.");

        screen.ReplaceRoot(new RootReplacementProbeComponent(), resetState: true);
        RenderFrame(context, screen);
        AssertEqual(0, RootReplacementProbeComponent.LastValue, "A full preview replacement must reset hook state.");
        AssertEqual(2, RootReplacementProbeComponent.MountCount, "A full replacement must mount a fresh effect.");
        AssertEqual(1, RootReplacementProbeComponent.CleanupCount, "A full replacement must clean up the previous effect.");
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

    private static void DiagnosticsCanExcludeInspectorScreen()
    {
        ProbeComponent.Reset();
        NuriDiagnostics.Enable();
        NuriDiagnostics.ClearLogs();
        using var context = CreateContext();
        var component = new ProbeComponent();
        var screen = new NuriDuxelScreen(
            component,
            () => { },
            "excluded-diagnostics-test",
            includeInDiagnostics: false);

        RenderFrame(context, screen);
        ProbeComponent.Update!(1);
        RenderFrame(context, screen);

        var snapshot = NuriDiagnostics.GetSnapshot();
        AssertTrue(
            snapshot.Roots.All(root => root.RootId != component.Id),
            "A diagnostics-excluded Duxel screen must not register itself as an inspected root.");
        AssertTrue(
            snapshot.RecentLogs.All(log => log.ComponentId != component.Id && log.RootId != component.Id),
            "A diagnostics-excluded Duxel screen must not emit component, invalidation, or patch logs for itself.");

        screen.Dispose();
        NuriDiagnostics.Log(RuntimeLogKind.AppLog, component.Id, component.Id, "exclusion released");
        AssertTrue(
            NuriDiagnostics.GetSnapshot().RecentLogs.Any(log => log.Message == "exclusion released"),
            "Disposing an excluded Duxel screen must release its diagnostics exclusion.");
        NuriDiagnostics.ClearLogs();
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

    private static void StandardRouterProjectsRouteLocalStateUpdates()
    {
        RouterCounterPage.Reset();
        RouterDetailsPage.Reset();
        using var context = CreateContext();
        var requestedFrames = 0;
        var root = new StandardRouterProbe();
        using var screen = new NuriDuxelScreen(
            root,
            () => requestedFrames++,
            "standard-router-state-test");

        RenderFrame(context, screen);
        AssertEqual(0, RouterCounterPage.LastRenderedCount, "The standard Router should initially project route-local state.");

        RouterCounterPage.Increment!();
        AssertTrue(requestedFrames > 0, "A routed page state update should request a Duxel frame.");
        RenderFrame(context, screen);
        AssertEqual(1, RouterCounterPage.LastRenderedCount, "A routed page state update should rerender its Duxel subtree.");

        root.NavigateDetails();
        RenderFrame(context, screen);
        AssertEqual("details", RouterDetailsPage.LastRenderedName, "Route replacement should mount the independently keyed page state.");

        RouterDetailsPage.UpdateName!("updated");
        RenderFrame(context, screen);
        AssertEqual("updated", RouterDetailsPage.LastRenderedName, "The replacement page setter should remain active after navigation.");
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

    private static void VirtualizedItemsProjectBoundedViewportRows()
    {
        NuriDiagnostics.Enable();
        VirtualizedItemsProbeComponent.RenderedIndices.Clear();
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        var screen = new NuriDuxelScreen(
            new VirtualizedItemsProbeComponent(),
            () => { },
            "virtualized-items-test",
            input);

        try
        {
            RenderFrame(context, screen);

            AssertTrue(
                VirtualizedItemsProbeComponent.RenderedIndices.Count is > 0 and <= 8,
                $"A 160px viewport must project a bounded number of 32px rows, not all 10,000 (actual {VirtualizedItemsProbeComponent.RenderedIndices.Count}).");
            AssertEqual(
                0,
                VirtualizedItemsProbeComponent.RenderedIndices.Min(),
                "The initial virtualized projection must start with the first item.");

            var metrics = NuriDiagnostics.GetSnapshot().VirtualizedItems.Single();
            AssertEqual(10_000, metrics.ItemCount, "Virtualized diagnostics must retain the full item count.");
            AssertEqual(
                VirtualizedItemsProbeComponent.RenderedIndices.Count,
                metrics.RealizedCount,
                "Virtualized diagnostics must report only the rows projected for the current frame.");

            var region = input.GetScrollRegions().Single();
            var handleCenter = Center(region.ScrollbarHandle);
            var trackBottom = new UiVector2(
                region.ScrollbarTrack.X + (region.ScrollbarTrack.Width * 0.5f),
                region.ScrollbarTrack.Y + region.ScrollbarTrack.Height - 1f);
            VirtualizedItemsProbeComponent.RenderedIndices.Clear();
            input.Enqueue(1, DuxelInputEventKind.PointerDown, handleCenter, code: 0, capturedByNuri: true);
            input.Enqueue(2, DuxelInputEventKind.PointerMove, trackBottom, capturedByNuri: true);
            input.Enqueue(3, DuxelInputEventKind.PointerUp, trackBottom, code: 0, capturedByNuri: true);

            RenderFrame(context, screen);

            AssertTrue(
                VirtualizedItemsProbeComponent.RenderedIndices.Count is > 0 and <= 8,
                "Scrolling to the end must retain bounded per-frame row projection.");
            AssertEqual(
                9_999,
                VirtualizedItemsProbeComponent.RenderedIndices.Max(),
                "Dragging to the end must project the final item without traversing preceding rows.");

            screen.Dispose();
            AssertEqual(
                0,
                NuriDiagnostics.GetSnapshot().VirtualizedItems.Count,
                "Disposing a Duxel screen must remove its virtualized diagnostics.");

            VirtualizedItemsProbeComponent.RenderedIndices.Clear();
            using (var directContext = CreateContext())
            using (var directScreen = new NuriDuxelScreen(
                new VirtualizedItemsProbeComponent(),
                () => { },
                "direct-virtualized-items-test"))
            {
                RenderFrame(directContext, directScreen);
                AssertTrue(
                    VirtualizedItemsProbeComponent.RenderedIndices.Count is > 0 and <= 8,
                    "A direct Duxel host must use CalcListClipping instead of projecting all 10,000 rows.");
            }

            AssertEqual(
                0,
                NuriDiagnostics.GetSnapshot().VirtualizedItems.Count,
                "Disposing a direct Duxel screen must remove its virtualized diagnostics.");
        }
        finally
        {
            screen.Dispose();
            NuriDiagnostics.Disable();
        }
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

    private sealed class StandardRouterProbe : Component
    {
        private Navigator? _navigator;

        public void NavigateDetails()
        {
            _navigator!.Navigate("details");
        }

        public override IElement Render()
        {
            var (navigation, navigator) = useNavigation("home");
            _navigator = navigator;
            return Div(
                Router(
                    navigation,
                    Route("home", () => new RouterCounterPage()),
                    Route("details", () => new RouterDetailsPage())));
        }
    }

    private sealed class RouterCounterPage : Component
    {
        public static Action? Increment { get; private set; }

        public static int LastRenderedCount { get; private set; }

        public static void Reset()
        {
            Increment = null;
            LastRenderedCount = -1;
        }

        public override IElement Render()
        {
            var (count, setCount) = useState(0);
            Increment = () => setCount(current => current + 1);
            LastRenderedCount = count;
            return Div(
                Text($"count:{count}"),
                Button("Increment", Increment));
        }
    }

    private sealed class RouterDetailsPage : Component
    {
        public static Action<string>? UpdateName { get; private set; }

        public static string LastRenderedName { get; private set; } = string.Empty;

        public static void Reset()
        {
            UpdateName = null;
            LastRenderedName = string.Empty;
        }

        public override IElement Render()
        {
            var (name, setName) = useState("details");
            UpdateName = value => setName(_ => value);
            LastRenderedName = name;
            return Div(Text(name));
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

    private sealed class ContentScaleProbeScreen(UiScreen content) : UiScreen
    {
        public float ContentScale { get; private set; }

        public override void Render(UiImmediateContext ui)
        {
            content.Render(ui);
            ContentScale = ui.ContentScale;
        }
    }

    private sealed class RootReplacementProbeComponent : Component
    {
        public static Action<int>? Update { get; private set; }

        public static int LastValue { get; private set; }

        public static int MountCount { get; private set; }

        public static int CleanupCount { get; private set; }

        public static void Reset()
        {
            Update = null;
            LastValue = -1;
            MountCount = 0;
            CleanupCount = 0;
        }

        public override IElement Render()
        {
            var (value, setValue) = useState(0);
            Update = next => setValue(_ => next);
            LastValue = value;
            useEffect(() =>
            {
                MountCount++;
                return () => CleanupCount++;
            }, []);
            return Text($"replacement:{value}");
        }
    }

    private sealed class AutoColumnNestedDivComponent : Component
    {
        public override IElement Render()
        {
            return Grid(
                    Text("flexible").Column(0),
                    Div(Button("content").Size(120, 20))
                        .Padding(14, 5, 14, 5)
                        .Background("#135E75")
                        .Column(1))
                .Columns(Star, Auto)
                .Rows(Auto)
                .Size(500, 80);
        }
    }

    private sealed class GridElementAlignmentComponent : Component
    {
        public override IElement Render()
        {
            return Grid(
                    Button("start").Size(20, 20).Start().Top().Column(0),
                    Button("center").Size(20, 20).Center().Column(1),
                    Button("end").Size(20, 20).End().Bottom().Column(2))
                .Columns(Pixels(100), Pixels(100), Pixels(100))
                .Rows(Pixels(100))
                .Size(300, 100);
        }
    }

    private sealed class AutoGridPaddingAlignmentComponent : Component
    {
        public override IElement Render()
        {
            var toolbar = Grid(
                    Div(
                            DivTypes.Row,
                            Button("Inspector").Size(100, 32),
                            Button("Console").Size(100, 32))
                        .Spacing(8)
                        .VCenter()
                        .Column(0),
                    Button("Clear logs").Size(100, 32).VCenter().Column(1))
                .Columns(Star, Auto)
                .Rows(Auto)
                .Padding(8)
                .Background("#F8FAFD");

            return Div(
                    toolbar,
                    Div().Size(200, 20))
                .Spacing(0);
        }
    }

    private sealed class RowChildAlignmentComponent : Component
    {
        public override IElement Render()
        {
            return Div(
                    DivTypes.Row,
                    Button("top").Size(20, 20).Top(),
                    Button("center").Size(20, 20).VCenter(),
                    Button("bottom").Size(20, 20).Bottom())
                .Size(100, 100)
                .Spacing(10);
        }
    }

    private sealed class ColumnChildAlignmentComponent : Component
    {
        public override IElement Render()
        {
            return Div(
                    Button("start").Size(20, 20).Start(),
                    Button("center").Size(20, 20).HCenter(),
                    Button("end").Size(20, 20).End())
                .Size(100, 100)
                .Spacing(5);
        }
    }

    private sealed class ButtonContentAlignmentComponent : Component
    {
        public override IElement Render()
        {
            return Grid(
                    Button("top-left").Size(120, 50).TextStart().TextTop().Column(0),
                    Button("center").Size(120, 50).TextCenter().Column(1),
                    Button("bottom-right").Size(120, 50).TextEnd().TextBottom().Column(2))
                .Columns(Pixels(120), Pixels(120), Pixels(120))
                .Rows(Pixels(50));
        }
    }

    private sealed class DecoratedGridBottomPaddingComponent : Component
    {
        public override IElement Render()
        {
            return Div(
                    Grid(Button("content").Size(100, 20))
                        .Columns(Pixels(100))
                        .Rows(Auto)
                        .Padding(18)
                        .Background("#14233A"),
                    Div(Text("sibling"))
                        .Size(200, 24)
                        .Background("#EAF0F7"))
                .Width(200)
                .Spacing(0);
        }
    }

    private sealed class HeaderGridPaddingComponent : Component
    {
        public override IElement Render()
        {
            var header = Grid(
                    Div(
                            Text("NURI DEVTOOLS").FontSize(12),
                            Text("Runtime inspector").FontSize(24),
                            Text("Inspect components, hooks, stores, and renderer activity in real time."))
                        .Spacing(4)
                        .Column(0),
                    Div(
                            Text("LIVE SNAPSHOT").FontSize(11),
                            Text("1 roots  |  12 components  |  30 logs"))
                        .Padding(14, 10, 14, 10)
                        .Spacing(3)
                        .Background("#1D3352")
                        .Column(1))
                .Columns(Star, Auto)
                .Rows(Auto)
                .Padding(18)
                .Width(600)
                .Background("#14233A");

            return Div(
                    header,
                    Div(Text("following content"))
                        .Size(600, 24)
                        .Background("#EAF0F7"))
                .Width(600)
                .Spacing(12);
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

    private static void VirtualizedItemsClipRowsBeforeScrollbar()
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new VirtualizedItemsClipProbeComponent(),
            () => { },
            "virtualized-items-scrollbar-clip-test",
            input);

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var region = input.GetScrollRegions().Single();
            AssertTrue(region.ScrollbarTrack.Width > 0f, "The clip probe must produce a scrollbar.");

            var rowBackgrounds = drawData.DrawLists
                .SelectMany(drawList => drawList.Commands)
                .Where(command => command.Kind == UiDrawCommandKind.RectFilledPrimitives
                    && command.HasBounds
                    && command.Bounds.Width >= 200f
                    && command.Bounds.Height is >= 20f and <= 40f)
                .ToArray();

            AssertTrue(rowBackgrounds.Length > 0, "The clip probe must draw virtualized row backgrounds.");
            AssertTrue(
                rowBackgrounds.All(command =>
                    command.ClipRect.X + command.ClipRect.Width
                        <= region.ScrollbarTrack.X + 0.01f),
                "Virtualized row drawing must be clipped at the scrollbar track instead of extending underneath it.");
        }
        finally
        {
            drawData.ReleasePooled();
        }
    }

    private static void MeasuredVirtualizedItemsLearnVariableRowHeights()
    {
        MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Clear();
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        var requestedFrames = 0;
        using var screen = new NuriDuxelScreen(
            new MeasuredVirtualizedItemsProbeComponent(),
            () => requestedFrames++,
            "measured-virtualized-items-test",
            input);

        for (var frame = 0; frame < 4; frame++)
        {
            MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Clear();
            RenderFrame(context, screen);
        }

        AssertTrue(requestedFrames > 0, "Learning variable row heights must request a stabilization frame.");
        AssertTrue(!screen.HasPendingLayout, "Measured row extents must settle once the visible range has been learned.");
        AssertTrue(
            MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Count is > 0 and <= 12,
            $"Measured virtualization must project a bounded pixel range instead of all 1,000 rows (actual {MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Count}).");
        AssertEqual(
            0,
            MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Min(),
            "The initial measured range must start with the first row.");

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var rowHeights = drawData.DrawLists
                .SelectMany(drawList => drawList.Commands)
                .Where(command => command.Kind == UiDrawCommandKind.RectFilledPrimitives
                    && command.HasBounds
                    && command.Bounds.Width >= 200f
                    && command.Bounds.Height is >= 20f and <= 90f)
                .Select(command => MathF.Round(command.Bounds.Height))
                .Distinct()
                .ToArray();
            AssertTrue(
                rowHeights.Contains(24f) && rowHeights.Contains(80f),
                "Measured virtualization must preserve both short and tall realized row heights.");
        }
        finally
        {
            drawData.ReleasePooled();
        }

        var region = input.GetScrollRegions().Single();
        var handleCenter = Center(region.ScrollbarHandle);
        var trackBottom = new UiVector2(
            region.ScrollbarTrack.X + (region.ScrollbarTrack.Width * 0.5f),
            region.ScrollbarTrack.Y + region.ScrollbarTrack.Height - 1f);
        MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Clear();
        input.Enqueue(1, DuxelInputEventKind.PointerDown, handleCenter, code: 0, capturedByNuri: true);
        input.Enqueue(2, DuxelInputEventKind.PointerMove, trackBottom, capturedByNuri: true);
        input.Enqueue(3, DuxelInputEventKind.PointerUp, trackBottom, code: 0, capturedByNuri: true);
        RenderFrame(context, screen);

        AssertTrue(
            MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Count is > 0 and <= 12,
            $"Scrolling measured rows to the end must retain bounded pixel-range projection (actual {MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Count}).");
        AssertEqual(
            999,
            MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Max(),
            "Measured virtualization must reach the final row without traversing all preceding templates.");

        MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Clear();
        using var directContext = CreateContext();
        using var directScreen = new NuriDuxelScreen(
            new MeasuredVirtualizedItemsProbeComponent(),
            () => { },
            "direct-measured-virtualized-items-test");
        for (var frame = 0; frame < 4; frame++)
        {
            MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Clear();
            RenderFrame(directContext, directScreen);
        }

        AssertTrue(
            MeasuredVirtualizedItemsProbeComponent.RenderedIndices.Count is > 0 and <= 12,
            "A direct Duxel host must also keep measured row projection bounded.");
    }

    private static void MeasuredDecoratedRowsStayAlignedWithoutOverlap()
    {
        using var context = CreateContext();
        var input = new DuxelInputEventQueue();
        using var screen = new NuriDuxelScreen(
            new MeasuredDecoratedRowsProbeComponent(),
            () => { },
            "measured-decorated-rows-test",
            input);

        for (var frame = 0; frame < 4; frame++)
        {
            RenderFrame(context, screen);
        }

        var drawData = RenderFrameWithDrawData(context, screen, FrameInfo);
        try
        {
            var fills = drawData.DrawLists
                .SelectMany(drawList => drawList.RectFilledPrimitives?.ToArray()
                    ?? Array.Empty<UiRectFilledPrimitive>())
                .ToArray();
            var backgrounds = fills
                .Where(primitive => primitive.Rect.Width >= 100f
                    && primitive.Rect.Height is > 20f and < 120f)
                .OrderBy(primitive => primitive.Rect.Y)
                .ToArray();

            AssertTrue(
                backgrounds.Length >= 3,
                $"The decorated row test must draw several measured row backgrounds. Fills: {string.Join(" | ", fills.Select(primitive => primitive.Rect))}");
            for (var index = 1; index < backgrounds.Length; index++)
            {
                var previous = backgrounds[index - 1].Rect;
                var current = backgrounds[index].Rect;
                AssertTrue(
                    current.Y + 0.01f >= previous.Y + previous.Height,
                    $"Measured row backgrounds must not overlap. Previous: {previous}; Current: {current}.");
                AssertTrue(
                    current.Y - (previous.Y + previous.Height) >= 5.5f,
                    $"Measured row spacing must preserve the declared bottom margin. Previous: {previous}; Current: {current}.");
                AssertTrue(
                    MathF.Abs(current.X - previous.X) <= 0.01f
                        && MathF.Abs(current.Width - previous.Width) <= 0.01f,
                    $"Measured row backgrounds must share one aligned content column. Previous: {previous}; Current: {current}.");
            }
        }
        finally
        {
            drawData.ReleasePooled();
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

    private sealed class NestedPanelDecorationComponent : Component
    {
        public override IElement Render()
        {
            return Div(
                    Div(Text("child"))
                        .Size(100, 40)
                        .Background("#ffffff")
                        .Brush("#000000")
                        .Thickness(1))
                .Size(220, 120)
                .Padding(10)
                .Background("#1e293b")
                .Brush("#0f172a")
                .Thickness(1);
        }
    }

    private sealed class VirtualizedItemsProbeComponent : Component
    {
        private static readonly int[] SourceItems = Enumerable.Range(0, 10_000).ToArray();

        public static List<int> RenderedIndices { get; } = new();

        public override IElement Render()
        {
            return VirtualizedItems(
                    SourceItems,
                    item =>
                    {
                        RenderedIndices.Add(item);
                        return Text(item.ToString()).Height(32);
                    },
                    buffer: 1,
                    itemExtent: 32,
                    itemKey: item => item.ToString())
                .Size(300, 160);
        }
    }

    private sealed class VirtualizedItemsClipProbeComponent : Component
    {
        private static readonly int[] SourceItems = Enumerable.Range(0, 100).ToArray();

        public override IElement Render()
        {
            return VirtualizedItems(
                    SourceItems,
                    item => Div(Text(item == 0 ? "selected" : item.ToString()))
                        .Height(32)
                        .Background(item == 0 ? "#dbeafe" : "#ffffff"),
                    buffer: 0,
                    itemExtent: 32,
                    itemKey: item => item.ToString())
                .Size(300, 160);
        }
    }

    private sealed class MeasuredVirtualizedItemsProbeComponent : Component
    {
        private static readonly int[] SourceItems = Enumerable.Range(0, 1_000).ToArray();

        public static List<int> RenderedIndices { get; } = new();

        public override IElement Render()
        {
            return VirtualizedItems(
                    SourceItems,
                    item =>
                    {
                        RenderedIndices.Add(item);
                        var height = item % 2 == 0 ? 24d : 80d;
                        return Div(Text(item.ToString()))
                            .Height(height)
                            .Background(item == 0 ? "#dbeafe" : "#ffffff");
                    },
                    estimatedItemExtent: 36,
                    bufferPixels: 64,
                    itemKey: item => item.ToString())
                .Size(300, 160);
        }
    }

    private sealed class MeasuredDecoratedRowsProbeComponent : Component
    {
        private static readonly int[] SourceItems = Enumerable.Range(0, 20).ToArray();

        public override IElement Render()
        {
            return VirtualizedItems(
                    SourceItems,
                    item => Div(
                            Text($"12:34:56.789  Message {item}"),
                            Text($"SampleComponent.Render:{item}"))
                        .Spacing(2)
                        .Padding(8)
                        .Margin(bottom: 6)
                        .Background("#f5f8fc")
                        .Brush("#dce5f0")
                        .Thickness(1),
                    estimatedItemExtent: 58,
                    bufferPixels: 320,
                    itemKey: item => item.ToString())
                .Size(420, 240);
        }
    }
}
