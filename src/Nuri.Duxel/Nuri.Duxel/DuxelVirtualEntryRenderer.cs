using Duxel.Core;
using Nuri.Constants;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Events;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.Duxel;

public sealed class DuxelVirtualEntryRenderer
{
    private const float ScrollFrictionPerSecond = 11f;
    private const float ScrollImpulsePerStep = 12f;
    private const float ScrollStopVelocity = 4f;
    private const float MaximumScrollSpeedInSteps = 42f;
    private const float ScrollbarWidth = 12f;
    private const float ScrollbarInset = 2f;
    private readonly DuxelInputEventQueue? _inputEvents;
    private readonly Dictionary<string, ScrollRegionState> _scrollRegions = new(StringComparer.Ordinal);
    private readonly List<DuxelInputEvent> _deferredInputEvents = new();
    private readonly Stack<UiRect> _panelClipRects = new();
    private long _frameNumber;
    private int _scrollOrder;
    private double _lastFrameTime;

    public DuxelVirtualEntryRenderer(DuxelInputEventQueue? inputEvents = null)
    {
        _inputEvents = inputEvents;
    }

    public bool HasActiveAnimations { get; private set; }

    public bool HasActiveScrollMotion { get; private set; }

    public bool HasPendingInput => _deferredInputEvents.Count > 0 || (_inputEvents?.HasPending ?? false);

    public IReadOnlyList<DuxelInputEvent> LastInputEvents { get; private set; } = [];

    public void Render(UiImmediateContext ui, VirtualEntry entry)
    {
        ArgumentNullException.ThrowIfNull(ui);
        var origin = ui.GetCursorPos();
        var available = ui.GetContentRegionAvail();
        Render(ui, entry, new UiRect(origin.X, origin.Y, available.X, available.Y));
    }

    public void Render(UiImmediateContext ui, VirtualEntry entry, UiRect bounds)
    {
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(entry);

        BeginInputFrame(ui);
        AdvanceScrollPhysics(GetFrameDelta(ui));
        _frameNumber++;
        _scrollOrder = 0;

        if (NuriDiagnostics.IsEnabled)
        {
            LogUnsupportedFeatures(entry);
        }

        var drawList = ui.GetWindowDrawList();
        var ownsChannels = !drawList.HasChannels && ContainsPanelDecoration(entry);
        List<PanelDecoration>? decorations = ownsChannels ? new List<PanelDecoration>() : null;
        var hasActiveAnimations = false;
        HasActiveAnimations = false;

        if (ownsChannels)
        {
            drawList.Split(2);
            drawList.SetCurrentChannel(1);
        }

        try
        {
            ui.SetCursorPos(new UiVector2(bounds.X, bounds.Y));
            RenderEntry(
                ui,
                entry,
                decorations,
                1f,
                new LayoutConstraint(
                    MathF.Max(0f, bounds.Width),
                    MathF.Max(0f, bounds.Height)),
                ref hasActiveAnimations);
        }
        finally
        {
            HasActiveAnimations = hasActiveAnimations;
            if (ownsChannels)
            {
                drawList.SetCurrentChannel(0);
                foreach (var decoration in decorations!)
                {
                    DrawPanel(ui, drawList, decoration);
                }

                drawList.Merge();
            }

            PublishScrollRegions();
        }
    }

    private void RenderEntry(
        UiImmediateContext ui,
        VirtualEntry entry,
        List<PanelDecoration>? decorations,
        float inheritedOpacity,
        LayoutConstraint constraint,
        ref bool hasActiveAnimations)
    {
        var localOpacity = ResolveOpacity(ui, entry, ref hasActiveAnimations);
        var effectiveOpacity = Math.Clamp(inheritedOpacity * localOpacity, 0f, 1f);
        var margin = GetThickness(entry, "Margin");
        ApplyTopPadding(ui, margin.Top);
        if (margin.Left > 0)
        {
            ui.Indent(ToFloat(margin.Left));
        }

        var contentConstraint = constraint.Inset(margin);
        var styleScope = PushEntryStyle(ui, entry, localOpacity, effectiveOpacity);
        try
        {
            RenderEntryCore(
                ui,
                entry,
                decorations,
                effectiveOpacity,
                contentConstraint,
                ref hasActiveAnimations);
        }
        finally
        {
            if (styleScope.StyleVarCount > 0)
            {
                ui.PopStyleVar(styleScope.StyleVarCount);
            }

            if (styleScope.ColorCount > 0)
            {
                ui.PopStyleColor(styleScope.ColorCount);
            }

            if (styleScope.FontSizePushed)
            {
                ui.PopFontSize();
            }

            if (margin.Left > 0)
            {
                ui.Unindent(ToFloat(margin.Left));
            }

            ApplyBottomPadding(ui, margin.Bottom);
        }
    }

    private void RenderEntryCore(
        UiImmediateContext ui,
        VirtualEntry entry,
        List<PanelDecoration>? decorations,
        float effectiveOpacity,
        LayoutConstraint constraint,
        ref bool hasActiveAnimations)
    {
        switch (entry.Type)
        {
            case VirtualControlTypes.Window:
                RenderVerticalChildren(ui, entry, decorations, effectiveOpacity, constraint, ref hasActiveAnimations);
                return;
            case VirtualControlTypes.Div:
                RenderDiv(ui, entry, decorations, effectiveOpacity, constraint, ref hasActiveAnimations);
                return;
            case VirtualControlTypes.Text:
                RenderText(ui, entry, constraint);
                return;
            case VirtualControlTypes.Input:
                RenderInput(ui, entry, effectiveOpacity, constraint);
                return;
            default:
                RenderVerticalChildren(ui, entry, decorations, effectiveOpacity, constraint, ref hasActiveAnimations);
                return;
        }
    }

    private void RenderDiv(
        UiImmediateContext ui,
        VirtualEntry entry,
        List<PanelDecoration>? decorations,
        float effectiveOpacity,
        LayoutConstraint constraint,
        ref bool hasActiveAnimations)
    {
        constraint = constraint.WithExplicitSize(GetSize(entry));
        var decorate = decorations is not null && HasPanelDecoration(entry);
        var panelStart = default(UiVector2);
        var availableSize = default(UiVector2);
        if (decorate)
        {
            panelStart = ui.GetCursorScreenPos();
            var region = ui.GetContentRegionAvail();
            availableSize = new UiVector2(
                constraint.Width ?? region.X,
                constraint.Height ?? region.Y);
            ui.BeginGroup();
        }

        var isScroll = string.Equals(entry.Kind, DivTypes.Scroll, StringComparison.Ordinal);
        var useNuriScroll = isScroll && _inputEvents is not null;
        var directScrollClipPushed = false;
        if (isScroll && !useNuriScroll)
        {
            var requestedSize = GetSize(entry);
            if (requestedSize.X <= 0f && constraint.Width is float constrainedWidth)
            {
                requestedSize = requestedSize with { X = constrainedWidth };
            }

            if (requestedSize.Y <= 0f && constraint.Height is float constrainedHeight)
            {
                requestedSize = requestedSize with { Y = constrainedHeight };
            }

            var scrollStart = ui.GetCursorScreenPos();
            PushPanelClip(new UiRect(
                scrollStart.X,
                scrollStart.Y,
                MathF.Max(1f, requestedSize.X),
                MathF.Max(1f, requestedSize.Y)));
            directScrollClipPushed = true;
            _ = ui.BeginChild(
                string.IsNullOrWhiteSpace(entry.Id) ? "NuriScroll" : entry.Id,
                requestedSize,
                border: HasVisibleBorder(entry));
        }

        try
        {
            if (useNuriScroll)
            {
                RenderScrollDiv(
                    ui,
                    entry,
                    decorations,
                    effectiveOpacity,
                    constraint,
                    ref hasActiveAnimations);
            }
            else
            {
                RenderDivContent(
                    ui,
                    entry,
                    decorations,
                    effectiveOpacity,
                    constraint,
                    ref hasActiveAnimations);
            }
        }
        finally
        {
            if (isScroll && !useNuriScroll)
            {
                ui.EndChild();
                if (directScrollClipPushed)
                {
                    _panelClipRects.Pop();
                }
            }

            if (decorate)
            {
                ui.EndGroup();
                var panelEnd = ui.GetItemRectMax();
                var margin = GetThickness(entry, "Margin");
                var width = TryGetSingle(entry, PropertyKeys.Width, out var explicitWidth) && explicitWidth > 0
                    ? explicitWidth
                    : MathF.Max(0f, constraint.Width ?? (availableSize.X - ToFloat(margin.Right)));
                var padding = GetThickness(entry, "Padding");
                var height = TryGetSingle(entry, PropertyKeys.Height, out var explicitHeight) && explicitHeight > 0
                    ? explicitHeight
                    : constraint.Height ?? 0f;
                var panelRight = constraint.Width is not null
                    ? panelStart.X + MathF.Max(0f, width)
                    : MathF.Max(panelEnd.X, panelStart.X + MathF.Max(0f, width));
                var panelBottom = constraint.Height is not null
                    ? panelStart.Y + MathF.Max(1f, height)
                    : MathF.Max(
                        panelEnd.Y + ToFloat(padding.Bottom),
                        panelStart.Y + MathF.Max(1f, height));
                var rect = new UiRect(
                    panelStart.X,
                    panelStart.Y,
                    MathF.Max(0f, panelRight - panelStart.X),
                    MathF.Max(1f, panelBottom - panelStart.Y));
                var clipRect = _panelClipRects.TryPeek(out var activeClip)
                    ? activeClip
                    : (UiRect?)null;
                decorations!.Add(CreatePanelDecoration(entry, rect, effectiveOpacity, clipRect));
            }
        }
    }

    private void RenderScrollDiv(
        UiImmediateContext ui,
        VirtualEntry entry,
        List<PanelDecoration>? decorations,
        float effectiveOpacity,
        LayoutConstraint constraint,
        ref bool hasActiveAnimations)
    {
        var requestedSize = GetSize(entry);
        var available = ui.GetContentRegionAvail();
        var width = requestedSize.X > 0f
            ? requestedSize.X
            : constraint.Width ?? available.X;
        var height = requestedSize.Y > 0f
            ? requestedSize.Y
            : constraint.Height ?? (ui.GetFrameHeight() * 6f);
        var size = new UiVector2(MathF.Max(1f, width), MathF.Max(1f, height));
        var id = string.IsNullOrWhiteSpace(entry.Id) ? "NuriScroll" : entry.Id;
        if (!_scrollRegions.TryGetValue(id, out var state))
        {
            state = new ScrollRegionState();
            _scrollRegions.Add(id, state);
        }

        var clampedOffset = Math.Clamp(state.Offset, 0f, state.MaxOffset);
        if (MathF.Abs(clampedOffset - state.Offset) > 0.001f)
        {
            state.Velocity = 0f;
        }

        state.Offset = clampedOffset;
        var cursor = ui.GetCursorPos();
        var screenStart = ui.GetCursorScreenPos();
        var bounds = new UiRect(screenStart.X, screenStart.Y, size.X, size.Y);
        var contentBounds = new UiRect(
            bounds.X + 2f,
            bounds.Y + 2f,
            MathF.Max(0f, bounds.Width - 4f),
            MathF.Max(0f, bounds.Height - 4f));
        var contentStartY = contentBounds.Y - state.Offset;
        var contentEndY = contentStartY;

        ui.PushClipRect(contentBounds, true);
        PushPanelClip(contentBounds);
        try
        {
            ui.SetCursorPos(new UiVector2(contentBounds.X, contentStartY));
            RenderDivContent(
                ui,
                entry,
                decorations,
                effectiveOpacity,
                new LayoutConstraint(contentBounds.Width, null),
                ref hasActiveAnimations);
            contentEndY = ui.GetCursorPosY();
        }
        finally
        {
            _panelClipRects.Pop();
            ui.PopClipRect();
            ui.SetCursorPos(cursor);
        }

        ui.Dummy(size);

        var contentHeight = MathF.Max(0f, contentEndY - contentStartY);
        var visibleHeight = MathF.Max(0f, contentBounds.Height);
        state.MaxOffset = MathF.Max(0f, contentHeight - visibleHeight);
        state.Offset = Math.Clamp(state.Offset, 0f, state.MaxOffset);
        state.Bounds = bounds;
        state.Order = ++_scrollOrder;
        state.LastSeenFrame = _frameNumber;
        DrawScrollBar(ui, bounds, state, contentHeight);
    }

    private static void DrawScrollBar(
        UiImmediateContext ui,
        UiRect bounds,
        ScrollRegionState state,
        float contentHeight)
    {
        if (state.MaxOffset <= 0f || contentHeight <= 0f)
        {
            state.ScrollbarTrack = default;
            state.ScrollbarHandle = default;
            return;
        }

        const float minimumHandleHeight = 20f;
        var track = new UiRect(
            bounds.X + bounds.Width - ScrollbarInset - ScrollbarWidth,
            bounds.Y + ScrollbarInset,
            ScrollbarWidth,
            MathF.Max(0f, bounds.Height - (ScrollbarInset * 2f)));
        var handleHeight = Math.Clamp(
            track.Height * (track.Height / contentHeight),
            MathF.Min(minimumHandleHeight, track.Height),
            track.Height);
        var travel = MathF.Max(0f, track.Height - handleHeight);
        var handleY = track.Y + (state.Offset / state.MaxOffset) * travel;
        var handle = new UiRect(track.X, handleY, track.Width, handleHeight);
        state.ScrollbarTrack = track;
        state.ScrollbarHandle = handle;
        var drawList = ui.GetWindowDrawList();
        var trackRounding = MathF.Min(track.Width * 0.5f, track.Height * 0.5f);
        var handleRounding = MathF.Min(handle.Width * 0.5f, handle.Height * 0.5f);
        drawList.AddRectFilledRounded(
            track,
            ui.GetColorU32(UiStyleColor.ScrollbarBg),
            ui.WhiteTextureId,
            trackRounding,
            bounds);
        drawList.AddRectFilledRounded(
            handle,
            ui.GetColorU32(UiStyleColor.ScrollbarGrab),
            ui.WhiteTextureId,
            handleRounding,
            bounds);
    }

    private void RenderDivContent(
        UiImmediateContext ui,
        VirtualEntry entry,
        List<PanelDecoration>? decorations,
        float effectiveOpacity,
        LayoutConstraint constraint,
        ref bool hasActiveAnimations)
    {
        var padding = GetThickness(entry, "Padding");
        var contentConstraint = constraint.Inset(padding);
        ApplyTopPadding(ui, padding.Top);

        if (padding.Left > 0)
        {
            ui.Indent(ToFloat(padding.Left));
        }

        var spacingPushed = PushSpacing(ui, entry);
        try
        {
            switch (entry.Kind)
            {
                case DivTypes.Row:
                    ui.BeginRow();
                    try
                    {
                        RenderChildren(
                            ui,
                            entry,
                            decorations,
                            effectiveOpacity,
                            LayoutConstraint.Unconstrained,
                            ref hasActiveAnimations);
                    }
                    finally
                    {
                        ui.EndRow();
                    }

                    break;
                case DivTypes.Grid:
                    RenderGrid(
                        ui,
                        entry,
                        ToFloat(padding.Right),
                        ToFloat(padding.Bottom),
                        decorations,
                        effectiveOpacity,
                        contentConstraint,
                        ref hasActiveAnimations);
                    break;
                default:
                    RenderVerticalChildren(
                        ui,
                        entry,
                        decorations,
                        effectiveOpacity,
                        contentConstraint,
                        ref hasActiveAnimations);
                    break;
            }
        }
        finally
        {
            if (spacingPushed)
            {
                ui.PopStyleVar();
            }

            if (padding.Left > 0)
            {
                ui.Unindent();
            }

            ApplyBottomPadding(ui, padding.Bottom);
        }
    }

    private void RenderGrid(
        UiImmediateContext ui,
        VirtualEntry entry,
        float rightPadding,
        float bottomPadding,
        List<PanelDecoration>? decorations,
        float effectiveOpacity,
        LayoutConstraint constraint,
        ref bool hasActiveAnimations)
    {
        var columnDefinitions = GetColumnDefinitions(entry);
        var rowDefinitions = GetRowDefinitions(entry);
        var columnCount = Math.Max(1, columnDefinitions.Count);
        var rowCount = Math.Max(1, rowDefinitions.Count);
        var cells = BuildGridCells(entry, columnCount, rowCount);
        var spacing = ui.GetItemSpacing();
        var region = ui.GetContentRegionAvail();
        var availableWidth = MathF.Max(
            columnCount,
            constraint.Width ?? MathF.Max(0f, region.X - rightPadding));
        var availableHeight = MathF.Max(
            0f,
            constraint.Height ?? MathF.Max(0f, region.Y - bottomPadding));
        var columnWidths = ResolveColumnWidths(
            ui,
            columnDefinitions,
            cells,
            columnCount,
            availableWidth,
            spacing.X);
        var rowHeights = ResolveRowHeights(
            ui,
            rowDefinitions,
            cells,
            rowCount,
            availableHeight,
            spacing.Y,
            constraint.Height is not null);
        var gridStart = ui.GetCursorPos();
        var gridEndY = gridStart.Y + availableHeight;
        var currentY = gridStart.Y;

        for (var row = 0; row < rowCount; row++)
        {
            var minimumRowBottom = currentY + rowHeights[row];
            if (row < rowCount - 1)
            {
                minimumRowBottom += spacing.Y;
            }

            var rowBottom = minimumRowBottom;
            var currentX = gridStart.X;
            for (var column = 0; column < columnCount; column++)
            {
                var cellEntries = cells
                    .Where(cell => cell.Row == row && cell.Column == column)
                    .OrderBy(cell => cell.DeclarationIndex)
                    .ToArray();
                foreach (var cell in cellEntries)
                {
                    var columnSpan = TryGetInt32(cell.Entry, "Grid.ColumnSpan", out var configuredSpan)
                        ? Math.Clamp(configuredSpan, 1, columnCount - column)
                        : 1;
                    var cellWidth = 0f;
                    for (var spanIndex = 0; spanIndex < columnSpan; spanIndex++)
                    {
                        cellWidth += columnWidths[column + spanIndex];
                    }

                    cellWidth += spacing.X * (columnSpan - 1);
                    ui.SetCursorPos(new UiVector2(currentX, currentY));
                    RenderEntry(
                        ui,
                        cell.Entry,
                        decorations,
                        effectiveOpacity,
                        new LayoutConstraint(
                            MathF.Max(1f, cellWidth),
                            rowHeights[row] > 0f ? rowHeights[row] : null),
                        ref hasActiveAnimations);
                    rowBottom = MathF.Max(rowBottom, ui.GetCursorPosY());
                }

                currentX += columnWidths[column] + spacing.X;
            }

            var expandsToContent = row < rowDefinitions.Count
                ? rowDefinitions[row].Unit == LengthUnit.Auto
                : constraint.Height is null;
            currentY = expandsToContent ? rowBottom : minimumRowBottom;
            if (expandsToContent && constraint.Height is not null && row + 1 < rowCount)
            {
                RebalanceRemainingStarRows(
                    rowDefinitions,
                    rowHeights,
                    row + 1,
                    rowCount,
                    MathF.Max(0f, gridEndY - currentY),
                    spacing.Y);
            }
        }

        ui.SetCursorPos(new UiVector2(gridStart.X, currentY));
    }

    private static float[] ResolveColumnWidths(
        UiImmediateContext ui,
        IReadOnlyList<LengthValue> definitions,
        IReadOnlyList<GridCell> cells,
        int columnCount,
        float availableWidth,
        float spacing)
    {
        var widths = new float[columnCount];
        var contentWidth = MathF.Max(
            columnCount,
            availableWidth - (spacing * Math.Max(0, columnCount - 1)));
        var fixedWidth = 0f;
        var starWeight = 0f;

        for (var index = 0; index < columnCount; index++)
        {
            if (index >= definitions.Count)
            {
                starWeight += 1f;
                continue;
            }

            var definition = definitions[index];
            switch (definition.Unit)
            {
                case LengthUnit.Pixel:
                    widths[index] = MathF.Max(1f, ToFloat(definition.Value));
                    fixedWidth += widths[index];
                    break;
                case LengthUnit.Auto:
                    widths[index] = MathF.Max(
                        1f,
                        cells
                            .Where(cell => cell.Column == index)
                            .Select(cell => EstimateEntryWidth(ui, cell.Entry))
                            .DefaultIfEmpty(1f)
                            .Max());
                    fixedWidth += widths[index];
                    break;
                case LengthUnit.Star:
                    starWeight += MathF.Max(0f, ToFloat(definition.Value));
                    break;
            }
        }

        var flexibleWidth = MathF.Max(0f, contentWidth - fixedWidth);
        for (var index = 0; index < columnCount; index++)
        {
            if (widths[index] > 0f)
            {
                continue;
            }

            var weight = index < definitions.Count && definitions[index].Unit == LengthUnit.Star
                ? MathF.Max(0f, ToFloat(definitions[index].Value))
                : 1f;
            widths[index] = starWeight > 0f
                ? MathF.Max(1f, flexibleWidth * weight / starWeight)
                : MathF.Max(1f, flexibleWidth / columnCount);
        }

        return widths;
    }

    private static float[] ResolveRowHeights(
        UiImmediateContext ui,
        IReadOnlyList<LengthValue> definitions,
        IReadOnlyList<GridCell> cells,
        int rowCount,
        float availableHeight,
        float spacing,
        bool implicitRowsFillAvailable)
    {
        var heights = new float[rowCount];
        var contentHeight = MathF.Max(
            0f,
            availableHeight - (spacing * Math.Max(0, rowCount - 1)));
        var fixedHeight = 0f;
        var starWeight = 0f;

        for (var index = 0; index < rowCount; index++)
        {
            if (index >= definitions.Count)
            {
                if (implicitRowsFillAvailable)
                {
                    starWeight += 1f;
                }
                else
                {
                    heights[index] = MathF.Max(
                        1f,
                        cells
                            .Where(cell => cell.Row == index)
                            .Select(cell => EstimateEntryHeight(ui, cell.Entry))
                            .DefaultIfEmpty(1f)
                            .Max());
                    fixedHeight += heights[index];
                }

                continue;
            }

            var definition = definitions[index];
            if (definition.Unit == LengthUnit.Pixel)
            {
                heights[index] = MathF.Max(1f, ToFloat(definition.Value));
                fixedHeight += heights[index];
            }
            else if (definition.Unit == LengthUnit.Auto)
            {
                heights[index] = MathF.Max(
                    1f,
                    cells
                        .Where(cell => cell.Row == index)
                        .Select(cell => EstimateEntryHeight(ui, cell.Entry))
                        .DefaultIfEmpty(1f)
                        .Max());
                fixedHeight += heights[index];
            }
            else if (definition.Unit == LengthUnit.Star)
            {
                starWeight += MathF.Max(0f, ToFloat(definition.Value));
            }
        }

        var flexibleHeight = MathF.Max(0f, contentHeight - fixedHeight);
        for (var index = 0; index < rowCount; index++)
        {
            if (index < definitions.Count && definitions[index].Unit != LengthUnit.Star
                || index >= definitions.Count && !implicitRowsFillAvailable)
            {
                continue;
            }

            var weight = index < definitions.Count
                ? MathF.Max(0f, ToFloat(definitions[index].Value))
                : 1f;
            heights[index] = starWeight > 0f
                ? MathF.Max(1f, flexibleHeight * weight / starWeight)
                : 0f;
        }

        return heights;
    }

    private static void RebalanceRemainingStarRows(
        IReadOnlyList<LengthValue> definitions,
        float[] heights,
        int startRow,
        int rowCount,
        float availableHeight,
        float spacing)
    {
        var fixedHeight = spacing * Math.Max(0, rowCount - startRow - 1);
        var starWeight = 0f;
        for (var index = startRow; index < rowCount; index++)
        {
            if (index < definitions.Count && definitions[index].Unit != LengthUnit.Star)
            {
                fixedHeight += heights[index];
                continue;
            }

            starWeight += index < definitions.Count
                ? MathF.Max(0f, ToFloat(definitions[index].Value))
                : 1f;
        }

        var flexibleHeight = MathF.Max(0f, availableHeight - fixedHeight);
        for (var index = startRow; index < rowCount; index++)
        {
            if (index < definitions.Count && definitions[index].Unit != LengthUnit.Star)
            {
                continue;
            }

            var weight = index < definitions.Count
                ? MathF.Max(0f, ToFloat(definitions[index].Value))
                : 1f;
            heights[index] = starWeight > 0f
                ? MathF.Max(1f, flexibleHeight * weight / starWeight)
                : 0f;
        }
    }

    private static float EstimateEntryHeight(UiImmediateContext ui, VirtualEntry entry)
    {
        var margin = GetThickness(entry, "Margin");
        var marginHeight = ToFloat(margin.Top + margin.Bottom);
        if (TryGetSingle(entry, PropertyKeys.Height, out var explicitHeight) && explicitHeight > 0f)
        {
            return explicitHeight + marginHeight;
        }

        if (entry.Type == VirtualControlTypes.Text)
        {
            var text = GetString(entry, PropertyKeys.Text);
            var textHeight = TryGetSingle(entry, "FontSize", out var fontSize) && fontSize > 0f
                ? fontSize
                : ui.CalcTextSize(text).Y;
            return MathF.Max(1f, textHeight + marginHeight);
        }

        if (entry.Type == VirtualControlTypes.Input)
        {
            var padding = GetThickness(entry, "Padding");
            var textHeight = ui.CalcTextSize(GetString(entry, "Content")).Y;
            var paddingHeight = padding.Top > 0 || padding.Bottom > 0
                ? ToFloat(padding.Top + padding.Bottom)
                : 8f;
            return MathF.Max(1f, textHeight + paddingHeight + marginHeight);
        }

        if (entry.Type != VirtualControlTypes.Div || entry.Children.Count == 0)
        {
            return MathF.Max(1f, marginHeight);
        }

        var paddingValue = GetThickness(entry, "Padding");
        var paddingHeightValue = ToFloat(paddingValue.Top + paddingValue.Bottom);
        var spacingProperty = string.Equals(entry.Kind, DivTypes.Grid, StringComparison.Ordinal)
            ? PropertyKeys.RowSpacing
            : PropertyKeys.Spacing;
        var spacingValue = TryGetSingle(entry, spacingProperty, out var configuredSpacing)
            ? configuredSpacing
            : string.Equals(entry.Kind, DivTypes.Grid, StringComparison.Ordinal)
                ? 0f
                : ui.GetItemSpacing().Y;
        float contentHeight;
        if (string.Equals(entry.Kind, DivTypes.Row, StringComparison.Ordinal))
        {
            contentHeight = entry.Children.Max(child => EstimateEntryHeight(ui, child));
        }
        else if (string.Equals(entry.Kind, DivTypes.Grid, StringComparison.Ordinal))
        {
            var columnCount = Math.Max(1, GetColumnDefinitions(entry).Count);
            var rowCount = Math.Max(1, GetRowDefinitions(entry).Count);
            var gridCells = BuildGridCells(entry, columnCount, rowCount);
            contentHeight = gridCells
                .GroupBy(cell => cell.Row)
                .Sum(group => group.Max(cell => EstimateEntryHeight(ui, cell.Entry)));
            contentHeight += spacingValue * Math.Max(0, gridCells.Select(cell => cell.Row).Distinct().Count() - 1);
        }
        else
        {
            contentHeight = entry.Children.Sum(child => EstimateEntryHeight(ui, child));
            contentHeight += spacingValue * Math.Max(0, entry.Children.Count - 1);
        }

        return MathF.Max(1f, contentHeight + paddingHeightValue + marginHeight);
    }

    private static float EstimateEntryWidth(UiImmediateContext ui, VirtualEntry entry)
    {
        var margin = GetThickness(entry, "Margin");
        var marginWidth = ToFloat(margin.Left + margin.Right);
        if (TryGetSingle(entry, PropertyKeys.Width, out var explicitWidth) && explicitWidth > 0f)
        {
            return explicitWidth + marginWidth;
        }

        var text = entry.Type == VirtualControlTypes.Text
            ? GetString(entry, PropertyKeys.Text)
            : GetString(entry, "Content");
        var measured = string.IsNullOrEmpty(text) ? 1f : ui.CalcTextSize(text).X;
        if (entry.Type == VirtualControlTypes.Input)
        {
            var padding = GetThickness(entry, "Padding");
            measured += padding.Left > 0 || padding.Right > 0
                ? ToFloat(padding.Left + padding.Right)
                : 16f;
        }

        return MathF.Max(1f, measured + marginWidth);
    }

    private static List<GridCell> BuildGridCells(
        VirtualEntry entry,
        int columnCount,
        int rowCount)
    {
        var cells = new List<GridCell>(entry.Children.Count);

        for (var index = 0; index < entry.Children.Count; index++)
        {
            var child = entry.Children[index];
            var hasExplicitRow = TryGetInt32(child, "Grid.Row", out var row);
            var hasExplicitColumn = TryGetInt32(child, "Grid.Column", out var column);

            if (!hasExplicitRow)
            {
                row = 0;
            }

            if (!hasExplicitColumn)
            {
                column = 0;
            }

            cells.Add(new GridCell(
                Math.Clamp(row, 0, rowCount - 1),
                Math.Clamp(column, 0, columnCount - 1),
                index,
                child));
        }

        return cells;
    }

    private static void RenderInput(
        UiImmediateContext ui,
        VirtualEntry entry,
        float effectiveOpacity,
        LayoutConstraint constraint)
    {
        var label = GetString(entry, "Content");
        var widgetLabel = $"{label}##{entry.Id}";
        var size = GetSize(entry);

        switch (entry.Kind)
        {
            case InputTypes.Checkbox:
            case InputTypes.Toggle:
            case InputTypes.Radio:
            {
                var value = GetBoolean(entry, PropertyKeys.IsChecked);
                if (ui.Checkbox(widgetLabel, ref value))
                {
                    InvokeValueEvent(entry, VirtualEventKind.CheckChanged, value);
                }

                return;
            }
            case InputTypes.Text:
            case InputTypes.Password:
            {
                var inputWidth = size.X > 0f ? size.X : constraint.Width ?? 0f;
                if (inputWidth > 0f)
                {
                    ui.SetNextItemWidth(inputWidth);
                }

                var value = GetString(entry, PropertyKeys.Text);
                if (ui.InputText(widgetLabel, ref value, 4096))
                {
                    InvokeValueEvent(entry, VirtualEventKind.TextChanged, value);
                }

                return;
            }
            default:
                RenderButton(ui, entry, label, widgetLabel, size, effectiveOpacity, constraint);
                return;
        }
    }

    private static void RenderText(UiImmediateContext ui, VirtualEntry entry, LayoutConstraint constraint)
    {
        var text = GetString(entry, PropertyKeys.Text);
        var originalCursorX = ui.GetCursorPosX();
        var aligned = false;
        if (TryGetHorizontalAlignment(entry, "HorizontalAlignment", out var alignment)
            && alignment != UiItemHorizontalAlign.Left)
        {
            var cursor = ui.GetCursorPos();
            var availableWidth = constraint.Width ?? ui.GetContentRegionAvail().X;
            var textWidth = ui.CalcTextSize(text).X;
            var offset = alignment == UiItemHorizontalAlign.Right
                ? availableWidth - textWidth
                : (availableWidth - textWidth) / 2f;
            ui.SetCursorPosX(cursor.X + MathF.Max(0f, offset));
            aligned = true;
        }

        ui.Text(text);
        if (aligned)
        {
            ui.SetCursorPosX(originalCursorX);
        }
    }

    private static void RenderButton(
        UiImmediateContext ui,
        VirtualEntry entry,
        string label,
        string widgetLabel,
        UiVector2 requestedSize,
        float effectiveOpacity,
        LayoutConstraint constraint)
    {
        var padding = GetThickness(entry, "Padding");
        var horizontalPadding = padding.Left > 0 || padding.Right > 0
            ? ToFloat(padding.Left + padding.Right)
            : 16f;
        var verticalPadding = padding.Top > 0 || padding.Bottom > 0
            ? ToFloat(padding.Top + padding.Bottom)
            : 8f;
        var textSize = ui.CalcTextSize(label);
        var width = requestedSize.X;
        if (width <= 0f)
        {
            width = constraint.Width ?? (textSize.X + horizontalPadding);
        }

        var height = requestedSize.Y > 0f
            ? requestedSize.Y
            : textSize.Y + verticalPadding;
        var clicked = ui.InvisibleButton(widgetLabel, new UiVector2(MathF.Max(1f, width), MathF.Max(1f, height)));
        var itemMin = ui.GetItemRectMin();
        var itemMax = ui.GetItemRectMax();
        var rect = new UiRect(
            itemMin.X,
            itemMin.Y,
            MathF.Max(0f, itemMax.X - itemMin.X),
            MathF.Max(0f, itemMax.Y - itemMin.Y));
        var drawList = ui.GetWindowDrawList();

        UiColor background;
        if (TryGetColor(entry, PropertyKeys.Background, out var backgroundColor))
        {
            background = WithOpacity(ToUiColor(backgroundColor), effectiveOpacity);
        }
        else
        {
            background = WithOpacity(ui.GetColorU32(
                ui.IsItemActive()
                    ? UiStyleColor.ButtonActive
                    : ui.IsItemHovered()
                        ? UiStyleColor.ButtonHovered
                        : UiStyleColor.Button), effectiveOpacity);
        }

        drawList.AddRectFilled(rect, background);

        var borderThickness = GetThickness(entry, "BorderThickness");
        var borderWidth = ToFloat(Math.Max(
            Math.Max(borderThickness.Left, borderThickness.Top),
            Math.Max(borderThickness.Right, borderThickness.Bottom)));
        if (borderWidth > 0f)
        {
            var border = TryGetColor(entry, "BorderBrush", out var borderColor)
                ? WithOpacity(ToUiColor(borderColor), effectiveOpacity)
                : WithOpacity(ui.GetColorU32(UiStyleColor.ButtonBorder), effectiveOpacity);
            drawList.AddRect(rect, border, 0f, borderWidth);
        }

        var left = ToFloat(padding.Left > 0 ? padding.Left : 8);
        var top = ToFloat(padding.Top > 0 ? padding.Top : 4);
        var right = ToFloat(padding.Right > 0 ? padding.Right : 8);
        var bottom = ToFloat(padding.Bottom > 0 ? padding.Bottom : 4);
        var textLeft = itemMin.X + left;
        var textTop = itemMin.Y + top;
        var textRect = new UiRect(
            textLeft,
            textTop,
            MathF.Max(0f, itemMax.X - right - textLeft),
            MathF.Max(0f, itemMax.Y - bottom - textTop));
        var horizontalAlignment = TryGetHorizontalAlignment(
            entry,
            "HorizontalContentAlignment",
            out var configuredAlignment)
                ? configuredAlignment
                : UiItemHorizontalAlign.Center;
        ui.DrawTextAligned(
            textRect,
            label,
            ui.GetColorU32(UiStyleColor.ButtonText),
            horizontalAlignment,
            UiItemVerticalAlign.Center,
            null,
            true);

        if (clicked)
        {
            InvokeActionEvent(entry, VirtualEventKind.Click);
        }
    }

    private static EntryStyleScope PushEntryStyle(
        UiImmediateContext ui,
        VirtualEntry entry,
        float localOpacity,
        float effectiveOpacity)
    {
        var fontSizePushed = TryGetSingle(entry, "FontSize", out var fontSize) && fontSize > 0f;
        if (fontSizePushed)
        {
            ui.PushFontSize(fontSize);
        }

        var colorCount = 0;
        var hasForeground = TryGetColor(entry, PropertyKeys.Foreground, out var foreground);
        if (hasForeground || localOpacity < 0.9999f)
        {
            PushTextStyleColor(ui, UiStyleColor.Text, hasForeground, foreground, localOpacity, effectiveOpacity);
            PushTextStyleColor(ui, UiStyleColor.ButtonText, hasForeground, foreground, localOpacity, effectiveOpacity);
            PushTextStyleColor(ui, UiStyleColor.CheckboxText, hasForeground, foreground, localOpacity, effectiveOpacity);
            PushTextStyleColor(ui, UiStyleColor.RadioButtonText, hasForeground, foreground, localOpacity, effectiveOpacity);
            PushTextStyleColor(ui, UiStyleColor.InputText, hasForeground, foreground, localOpacity, effectiveOpacity);
            colorCount += 5;
        }

        if (entry.Type == VirtualControlTypes.Input
            && TryGetColor(entry, PropertyKeys.Background, out var background))
        {
            ui.PushStyleColor(
                GetInputBackgroundStyle(entry.Kind),
                WithOpacity(ToUiColor(background), effectiveOpacity));
            colorCount++;
        }

        var styleVarCount = 0;
        if (entry.Type == VirtualControlTypes.Input)
        {
            var padding = GetThickness(entry, "Padding");
            if (padding.Left > 0 || padding.Top > 0 || padding.Right > 0 || padding.Bottom > 0)
            {
                ui.PushStyleVar(
                    UiStyleVar.FramePadding,
                    new UiVector2(
                        ToFloat(Math.Max(padding.Left, padding.Right)),
                        ToFloat(Math.Max(padding.Top, padding.Bottom))));
                styleVarCount++;
            }
        }

        return new EntryStyleScope(fontSizePushed, colorCount, styleVarCount);
    }

    private static void PushTextStyleColor(
        UiImmediateContext ui,
        UiStyleColor styleColor,
        bool hasForeground,
        ColorValue foreground,
        float localOpacity,
        float effectiveOpacity)
    {
        var color = hasForeground
            ? WithOpacity(ToUiColor(foreground), effectiveOpacity)
            : WithOpacity(ui.GetColorU32(styleColor), localOpacity);
        ui.PushStyleColor(styleColor, color);
    }

    private static UiStyleColor GetInputBackgroundStyle(string kind)
    {
        return kind switch
        {
            InputTypes.Checkbox or InputTypes.Toggle => UiStyleColor.CheckboxBg,
            InputTypes.Radio => UiStyleColor.RadioButtonBg,
            InputTypes.Text or InputTypes.Password => UiStyleColor.InputBg,
            _ => UiStyleColor.Button
        };
    }

    private static bool PushSpacing(UiImmediateContext ui, VirtualEntry entry)
    {
        var spacing = ui.GetItemSpacing();
        var horizontal = spacing.X;
        var vertical = spacing.Y;
        var hasOverride = false;

        if (string.Equals(entry.Kind, DivTypes.Grid, StringComparison.Ordinal))
        {
            horizontal = 0f;
            vertical = 0f;
            hasOverride = true;

            if (TryGetSingle(entry, PropertyKeys.ColumnSpacing, out var columnSpacing))
            {
                horizontal = columnSpacing;
            }

            if (TryGetSingle(entry, PropertyKeys.RowSpacing, out var rowSpacing))
            {
                vertical = rowSpacing;
            }
        }
        else if (TryGetSingle(entry, PropertyKeys.Spacing, out var linearSpacing))
        {
            if (string.Equals(entry.Kind, DivTypes.Row, StringComparison.Ordinal))
            {
                horizontal = linearSpacing;
            }
            else
            {
                vertical = linearSpacing;
            }

            hasOverride = true;
        }

        if (hasOverride)
        {
            ui.PushStyleVar(UiStyleVar.ItemSpacing, new UiVector2(horizontal, vertical));
        }

        return hasOverride;
    }

    private static void ApplyTopPadding(UiImmediateContext ui, double padding)
    {
        if (padding <= 0)
        {
            return;
        }

        var cursor = ui.GetCursorPos();
        ui.SetCursorPos(new UiVector2(cursor.X, cursor.Y + ToFloat(padding)));
    }

    private static void ApplyBottomPadding(UiImmediateContext ui, double padding)
    {
        if (padding <= 0)
        {
            return;
        }

        var cursor = ui.GetCursorPos();
        ui.SetCursorPos(new UiVector2(cursor.X, cursor.Y + ToFloat(padding)));
    }

    private void RenderChildren(
        UiImmediateContext ui,
        VirtualEntry entry,
        List<PanelDecoration>? decorations,
        float effectiveOpacity,
        LayoutConstraint constraint,
        ref bool hasActiveAnimations)
    {
        foreach (var child in entry.Children)
        {
            RenderEntry(ui, child, decorations, effectiveOpacity, constraint, ref hasActiveAnimations);
        }
    }

    private void RenderVerticalChildren(
        UiImmediateContext ui,
        VirtualEntry entry,
        List<PanelDecoration>? decorations,
        float effectiveOpacity,
        LayoutConstraint constraint,
        ref bool hasActiveAnimations)
    {
        var contentBottom = constraint.Height is float height
            ? ui.GetCursorPosY() + height
            : (float?)null;
        var spacing = ui.GetItemSpacing().Y;
        var hasExplicitSpacing = TryGetSingle(entry, PropertyKeys.Spacing, out _);

        for (var index = 0; index < entry.Children.Count; index++)
        {
            var child = entry.Children[index];
            float? childHeight = null;
            if (contentBottom is float bottom && IsFlexibleVerticalChild(child))
            {
                var reservedHeight = 0f;
                for (var following = index + 1; following < entry.Children.Count; following++)
                {
                    reservedHeight += EstimateEntryHeight(ui, entry.Children[following]) + spacing;
                }

                childHeight = MathF.Max(
                    0f,
                    bottom - ui.GetCursorPosY() - reservedHeight - (hasExplicitSpacing ? 0f : spacing));
            }

            RenderEntry(
                ui,
                child,
                decorations,
                effectiveOpacity,
                new LayoutConstraint(constraint.Width, childHeight),
                ref hasActiveAnimations);
        }

        if (hasExplicitSpacing && entry.Children.Count > 0 && spacing > 0f)
        {
            var cursor = ui.GetCursorPos();
            ui.SetCursorPos(new UiVector2(cursor.X, cursor.Y - spacing));
        }
    }

    private static bool IsFlexibleVerticalChild(VirtualEntry entry)
    {
        if (TryGetSingle(entry, PropertyKeys.Height, out var explicitHeight) && explicitHeight > 0f)
        {
            return false;
        }

        if (entry.Type != VirtualControlTypes.Div)
        {
            return false;
        }

        if (string.Equals(entry.Kind, DivTypes.Scroll, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(entry.Kind, DivTypes.Grid, StringComparison.Ordinal)
            && GetRowDefinitions(entry).Any(definition => definition.Unit == LengthUnit.Star);
    }

    private void BeginInputFrame(UiImmediateContext ui)
    {
        var newlyQueued = _inputEvents?.Drain() ?? [];
        DuxelInputEvent[] events;
        if (_deferredInputEvents.Count == 0)
        {
            events = newlyQueued;
        }
        else
        {
            events = new DuxelInputEvent[_deferredInputEvents.Count + newlyQueued.Length];
            _deferredInputEvents.CopyTo(events, 0);
            newlyQueued.CopyTo(events, _deferredInputEvents.Count);
            _deferredInputEvents.Clear();
        }

        LastInputEvents = events;
        if (events.Length == 0 || _scrollRegions.Count == 0)
        {
            return;
        }

        var scrollStep = ui.GetFrameHeight() * 3f;
        var wheelDirection = 0f;
        var deferNuriInput = false;
        foreach (var inputEvent in events)
        {
            if (!inputEvent.CapturedByNuri)
            {
                continue;
            }

            if (deferNuriInput)
            {
                _deferredInputEvents.Add(inputEvent);
                continue;
            }

            switch (inputEvent.Kind)
            {
                case DuxelInputEventKind.Wheel:
                    var direction = MathF.Sign(inputEvent.Delta.Y);
                    if (direction != 0f
                        && wheelDirection != 0f
                        && direction != wheelDirection)
                    {
                        // Preserve a visible frame for each rapid direction run. Applying
                        // equal down/up samples in one projection would be semantically
                        // ordered but visually indistinguishable from dropping both.
                        deferNuriInput = true;
                        _deferredInputEvents.Add(inputEvent);
                        break;
                    }

                    if (direction != 0f)
                    {
                        wheelDirection = direction;
                    }

                    ApplyWheel(inputEvent, scrollStep);
                    break;
                case DuxelInputEventKind.PointerDown when inputEvent.Code == 0:
                    BeginScrollDrag(inputEvent.Position);
                    break;
                case DuxelInputEventKind.PointerMove:
                    UpdateScrollDrag(inputEvent.Position);
                    break;
                case DuxelInputEventKind.PointerUp when inputEvent.Code == 0:
                    foreach (var state in _scrollRegions.Values)
                    {
                        state.IsDragging = false;
                    }
                    break;
            }
        }
    }

    private void ApplyWheel(DuxelInputEvent inputEvent, float scrollStep)
    {
        if (MathF.Abs(inputEvent.Delta.Y) <= 0.001f)
        {
            return;
        }

        var candidates = _scrollRegions.Values
            .Where(state => state.LastSeenFrame == _frameNumber
                && Contains(state.Bounds, inputEvent.Position)
                && state.MaxOffset > 0f)
            .OrderBy(state => state.Bounds.Width * state.Bounds.Height)
            .ThenByDescending(state => state.Order);

        foreach (var state in candidates)
        {
            var direction = -MathF.Sign(inputEvent.Delta.Y);
            var canMove = direction > 0f
                ? state.Offset < state.MaxOffset
                : state.Offset > 0f;
            if (!canMove)
            {
                continue;
            }

            var baseImpulse = -inputEvent.Delta.Y * scrollStep * ScrollImpulsePerStep;
            var sameDirection = MathF.Sign(state.Velocity) == MathF.Sign(baseImpulse);
            var acceleration = sameDirection
                ? 1f + MathF.Min(1.5f, MathF.Abs(state.Velocity) / MathF.Max(1f, scrollStep * ScrollImpulsePerStep))
                : 1f;
            var maximumSpeed = scrollStep * MaximumScrollSpeedInSteps;
            state.Velocity = Math.Clamp(
                state.Velocity + (baseImpulse * acceleration),
                -maximumSpeed,
                maximumSpeed);
            break;
        }
    }

    private float GetFrameDelta(UiImmediateContext ui)
    {
        var now = ui.GetTime();
        var elapsed = _lastFrameTime > 0d ? now - _lastFrameTime : 1d / 60d;
        _lastFrameTime = now;
        if (elapsed <= 0d || elapsed > 0.1d)
        {
            return 1f / 60f;
        }

        return Math.Clamp((float)elapsed, 1f / 240f, 1f / 20f);
    }

    private void AdvanceScrollPhysics(float deltaTime)
    {
        HasActiveScrollMotion = false;
        var damping = MathF.Exp(-ScrollFrictionPerSecond * deltaTime);
        foreach (var state in _scrollRegions.Values)
        {
            if (state.IsDragging || MathF.Abs(state.Velocity) <= ScrollStopVelocity)
            {
                state.Velocity = 0f;
                continue;
            }

            var intendedOffset = state.Offset + (state.Velocity * deltaTime);
            var nextOffset = Math.Clamp(intendedOffset, 0f, state.MaxOffset);
            state.Offset = nextOffset;
            if (MathF.Abs(nextOffset - intendedOffset) > 0.001f)
            {
                state.Velocity = 0f;
                continue;
            }

            state.Velocity *= damping;
            if (MathF.Abs(state.Velocity) > ScrollStopVelocity)
            {
                HasActiveScrollMotion = true;
            }
            else
            {
                state.Velocity = 0f;
            }
        }
    }

    private void BeginScrollDrag(UiVector2 position)
    {
        var state = _scrollRegions.Values
            .Where(candidate => candidate.LastSeenFrame == _frameNumber
                && candidate.MaxOffset > 0f
                && Contains(candidate.ScrollbarTrack, position))
            .OrderBy(candidate => candidate.Bounds.Width * candidate.Bounds.Height)
            .ThenByDescending(candidate => candidate.Order)
            .FirstOrDefault();
        if (state is null)
        {
            return;
        }

        state.IsDragging = true;
        state.Velocity = 0f;
        if (Contains(state.ScrollbarHandle, position))
        {
            state.DragOffsetY = position.Y - state.ScrollbarHandle.Y;
            return;
        }

        state.DragOffsetY = state.ScrollbarHandle.Height * 0.5f;
        UpdateScrollDrag(position);
    }

    private void UpdateScrollDrag(UiVector2 position)
    {
        foreach (var state in _scrollRegions.Values.Where(candidate => candidate.IsDragging))
        {
            var travel = MathF.Max(0f, state.ScrollbarTrack.Height - state.ScrollbarHandle.Height);
            if (travel <= 0f)
            {
                state.Offset = 0f;
                continue;
            }

            var handleY = Math.Clamp(
                position.Y - state.DragOffsetY,
                state.ScrollbarTrack.Y,
                state.ScrollbarTrack.Y + travel);
            state.Offset = ((handleY - state.ScrollbarTrack.Y) / travel) * state.MaxOffset;
        }
    }

    private void PublishScrollRegions()
    {
        if (_inputEvents is null)
        {
            return;
        }

        var active = _scrollRegions
            .Where(pair => pair.Value.LastSeenFrame == _frameNumber)
            .Select(pair => new DuxelScrollRegionSnapshot(
                pair.Key,
                pair.Value.Bounds,
                pair.Value.ScrollbarTrack,
                pair.Value.ScrollbarHandle,
                pair.Value.Offset,
                pair.Value.MaxOffset,
                pair.Value.Order))
            .ToArray();
        _inputEvents.PublishScrollRegions(active);

        foreach (var stale in _scrollRegions
            .Where(pair => pair.Value.LastSeenFrame != _frameNumber)
            .Select(pair => pair.Key)
            .ToArray())
        {
            _scrollRegions.Remove(stale);
        }
    }

    private void PushPanelClip(UiRect clipRect)
    {
        if (_panelClipRects.TryPeek(out var parentClip))
        {
            clipRect = Intersect(parentClip, clipRect);
        }

        _panelClipRects.Push(clipRect);
    }

    private static UiRect Intersect(UiRect left, UiRect right)
    {
        var x = MathF.Max(left.X, right.X);
        var y = MathF.Max(left.Y, right.Y);
        var rightEdge = MathF.Min(left.X + left.Width, right.X + right.Width);
        var bottomEdge = MathF.Min(left.Y + left.Height, right.Y + right.Height);
        return new UiRect(
            x,
            y,
            MathF.Max(0f, rightEdge - x),
            MathF.Max(0f, bottomEdge - y));
    }

    private static bool Contains(UiRect bounds, UiVector2 position)
    {
        return position.X >= bounds.X
            && position.X <= bounds.X + bounds.Width
            && position.Y >= bounds.Y
            && position.Y <= bounds.Y + bounds.Height;
    }

    private static float ResolveOpacity(
        UiImmediateContext ui,
        VirtualEntry entry,
        ref bool hasActiveAnimations)
    {
        var target = TryGetSingle(entry, "Opacity", out var configuredOpacity)
            ? Math.Clamp(configuredOpacity, 0f, 1f)
            : 1f;
        if (!entry.Animations.TryGetValue("Opacity", out var rawAnimation)
            || rawAnimation is not AnimationValue animation
            || animation.Duration <= TimeSpan.Zero)
        {
            return target;
        }

        var durationSeconds = MathF.Max(0.0001f, (float)animation.Duration.TotalSeconds);
        var current = ui.AnimateFloat(
            $"Nuri:{entry.Id}:Opacity",
            target,
            durationSeconds,
            ToDuxelEasing(animation.Easing));
        if (MathF.Abs(current - target) > 0.001f)
        {
            hasActiveAnimations = true;
        }

        return Math.Clamp(current, 0f, 1f);
    }

    private static UiAnimationEasing ToDuxelEasing(EasingValue? easing)
    {
        if (easing is null)
        {
            return UiAnimationEasing.Linear;
        }

        return easing.Mode switch
        {
            EasingModeValue.Out => UiAnimationEasing.OutCubic,
            EasingModeValue.InOut => UiAnimationEasing.InOutSine,
            _ => UiAnimationEasing.Linear
        };
    }

    private static bool HasPanelDecoration(VirtualEntry entry)
    {
        return TryGetColor(entry, PropertyKeys.Background, out _) || HasVisibleBorder(entry);
    }

    private static bool ContainsPanelDecoration(VirtualEntry entry)
    {
        if (entry.Type == VirtualControlTypes.Div && HasPanelDecoration(entry))
        {
            return true;
        }

        foreach (var child in entry.Children)
        {
            if (ContainsPanelDecoration(child))
            {
                return true;
            }
        }

        return false;
    }

    private static void LogUnsupportedFeatures(VirtualEntry entry)
    {
        foreach (var propertyName in UnsupportedPropertyNames)
        {
            if (entry.Properties.ContainsKey(propertyName))
            {
                LogUnsupportedProperty(entry, propertyName);
            }
        }

        foreach (var animation in entry.Animations.Values)
        {
            if (animation is AnimationValue animationValue)
            {
                if (string.Equals(animationValue.PropertyName, "Opacity", StringComparison.Ordinal))
                {
                    if (!IsSupportedOpacityEasing(animationValue.Easing))
                    {
                        LogUnsupportedProperty(
                            entry,
                            $"AnimationEasing:Opacity:{animationValue.Easing!.Kind}:{animationValue.Easing.Mode}");
                    }
                }
                else
                {
                    LogUnsupportedProperty(entry, $"Animation:{animationValue.PropertyName}");
                }
            }
        }

        foreach (var child in entry.Children)
        {
            LogUnsupportedFeatures(child);
        }
    }

    private static bool IsSupportedOpacityEasing(EasingValue? easing)
    {
        return easing is null
            || (easing.Kind == EasingKind.Cubic && easing.Mode == EasingModeValue.Out);
    }

    private static void LogUnsupportedProperty(VirtualEntry entry, string propertyName)
    {
        NuriDiagnostics.LogOnce(
            RuntimeLogKind.UnsupportedProperty,
            $"Duxel:UnsupportedProperty:{entry.Type}:{propertyName}",
            null,
            entry.ComponentId ?? entry.Id,
            $"Duxel property '{propertyName}' is not supported by '{entry.Type}' frame projection.");
    }

    private static PanelDecoration CreatePanelDecoration(
        VirtualEntry entry,
        UiRect rect,
        float effectiveOpacity,
        UiRect? clipRect)
    {
        UiColor? background = TryGetColor(entry, PropertyKeys.Background, out var backgroundColor)
            ? WithOpacity(ToUiColor(backgroundColor), effectiveOpacity)
            : null;
        UiColor? border = TryGetColor(entry, "BorderBrush", out var borderColor)
            ? WithOpacity(ToUiColor(borderColor), effectiveOpacity)
            : null;
        var thickness = GetThickness(entry, "BorderThickness");
        var borderThickness = ToFloat(Math.Max(
            Math.Max(thickness.Left, thickness.Top),
            Math.Max(thickness.Right, thickness.Bottom)));
        var cornerRadius = entry.Properties.TryGetValue("CornerRadius", out var rawRadius)
            && rawRadius is CornerRadiusValue radius
                ? ToFloat(Math.Max(
                    Math.Max(radius.TopLeft, radius.TopRight),
                    Math.Max(radius.BottomRight, radius.BottomLeft)))
                : 0f;

        return new PanelDecoration(rect, background, border, borderThickness, cornerRadius, clipRect);
    }

    private static void DrawPanel(
        UiImmediateContext ui,
        UiDrawListBuilder drawList,
        PanelDecoration decoration)
    {
        var clipRect = decoration.ClipRect;
        if (clipRect is UiRect activeClip)
        {
            drawList.PushClipRect(activeClip, intersectWithCurrentClipRect: true);
        }

        try
        {
            if (decoration.Background is UiColor background)
            {
                if (decoration.CornerRadius > 0f)
                {
                    drawList.AddRectFilledRounded(
                        decoration.Rect,
                        background,
                        ui.WhiteTextureId,
                        decoration.CornerRadius,
                        decoration.ClipRect ?? decoration.Rect);
                }
                else
                {
                    drawList.AddRectFilled(decoration.Rect, background);
                }
            }

            if (decoration.Border is UiColor border && decoration.BorderThickness > 0f)
            {
                drawList.AddRect(
                    decoration.Rect,
                    border,
                    decoration.CornerRadius,
                    decoration.BorderThickness);
            }
        }
        finally
        {
            if (clipRect is not null)
            {
                drawList.PopClipRect();
            }
        }
    }

    private static UiVector2 GetSize(VirtualEntry entry)
    {
        _ = TryGetSingle(entry, PropertyKeys.Width, out var width);
        _ = TryGetSingle(entry, PropertyKeys.Height, out var height);
        return new UiVector2(MathF.Max(0f, width), MathF.Max(0f, height));
    }

    private static IReadOnlyList<LengthValue> GetColumnDefinitions(VirtualEntry entry)
    {
        return entry.Properties.TryGetValue("ColumnDefinitions", out var value)
            && value is IReadOnlyList<LengthValue> definitions
                ? definitions
                : Array.Empty<LengthValue>();
    }

    private static IReadOnlyList<LengthValue> GetRowDefinitions(VirtualEntry entry)
    {
        return entry.Properties.TryGetValue("RowDefinitions", out var value)
            && value is IReadOnlyList<LengthValue> definitions
                ? definitions
                : Array.Empty<LengthValue>();
    }

    private static ThicknessValue GetThickness(VirtualEntry entry, string propertyName)
    {
        return entry.Properties.TryGetValue(propertyName, out var value)
            && value is ThicknessValue thickness
                ? thickness
                : default;
    }

    private static bool TryGetColor(VirtualEntry entry, string propertyName, out ColorValue color)
    {
        if (entry.Properties.TryGetValue(propertyName, out var value)
            && value is BrushValue.Solid solid)
        {
            color = solid.Color;
            return true;
        }

        color = default;
        return false;
    }

    private static bool TryGetHorizontalAlignment(
        VirtualEntry entry,
        string propertyName,
        out UiItemHorizontalAlign alignment)
    {
        if (entry.Properties.TryGetValue(propertyName, out var value)
            && value is HorizontalAlignmentValue configured)
        {
            alignment = configured.Kind switch
            {
                LayoutAlignmentKind.Center => UiItemHorizontalAlign.Center,
                LayoutAlignmentKind.End => UiItemHorizontalAlign.Right,
                _ => UiItemHorizontalAlign.Left
            };
            return true;
        }

        alignment = UiItemHorizontalAlign.Left;
        return false;
    }

    private static bool TryGetSingle(VirtualEntry entry, string propertyName, out float value)
    {
        if (entry.Properties.TryGetValue(propertyName, out var rawValue)
            && rawValue is IConvertible convertible)
        {
            try
            {
                value = Convert.ToSingle(convertible, System.Globalization.CultureInfo.InvariantCulture);
                return float.IsFinite(value);
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
            {
            }
        }

        value = 0f;
        return false;
    }

    private static bool TryGetInt32(VirtualEntry entry, string propertyName, out int value)
    {
        if (entry.Properties.TryGetValue(propertyName, out var rawValue)
            && rawValue is IConvertible convertible)
        {
            try
            {
                value = Convert.ToInt32(convertible, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
            {
            }
        }

        value = 0;
        return false;
    }

    private static bool HasVisibleBorder(VirtualEntry entry)
    {
        var thickness = GetThickness(entry, "BorderThickness");
        return thickness.Left > 0 || thickness.Top > 0 || thickness.Right > 0 || thickness.Bottom > 0;
    }

    private static UiColor ToUiColor(ColorValue color)
    {
        return new UiColor(color.R, color.G, color.B, color.A);
    }

    private static UiColor WithOpacity(UiColor color, float opacity)
    {
        var rgba = color.Rgba;
        var alpha = (byte)(rgba >> 24);
        var scaledAlpha = (byte)Math.Clamp(
            (int)MathF.Round(alpha * Math.Clamp(opacity, 0f, 1f)),
            0,
            byte.MaxValue);
        return new UiColor((rgba & 0x00FFFFFFu) | ((uint)scaledAlpha << 24));
    }

    private static float ToFloat(double value)
    {
        if (double.IsNaN(value))
        {
            return 0f;
        }

        return (float)Math.Clamp(value, float.MinValue, float.MaxValue);
    }

    private static string GetString(VirtualEntry entry, string propertyName)
    {
        return entry.Properties.TryGetValue(propertyName, out var value)
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            : string.Empty;
    }

    private static bool GetBoolean(VirtualEntry entry, string propertyName)
    {
        return entry.Properties.TryGetValue(propertyName, out var value)
            && value is bool boolean
            && boolean;
    }

    private static void InvokeActionEvent(VirtualEntry entry, VirtualEventKind kind)
    {
        foreach (var evt in entry.Events.Values)
        {
            if (evt is VirtualEvent virtualEvent
                && virtualEvent.Kind == kind
                && virtualEvent.Handler is Action action)
            {
                action();
                return;
            }
        }

        if (entry.Events.TryGetValue(EventKeys.Click, out var handler) && handler is Action fallback)
        {
            fallback();
        }
    }

    private static void InvokeValueEvent<T>(VirtualEntry entry, VirtualEventKind kind, T value)
    {
        var invoked = new HashSet<Delegate>();
        foreach (var evt in entry.Events.Values)
        {
            if (evt is VirtualEvent virtualEvent
                && virtualEvent.Kind == kind
                && virtualEvent.Handler is Action<T> action
                && invoked.Add(action))
            {
                action(value);
            }
        }
    }

    private readonly record struct EntryStyleScope(bool FontSizePushed, int ColorCount, int StyleVarCount);

    private readonly record struct LayoutConstraint(float? Width, float? Height)
    {
        public static LayoutConstraint Unconstrained => new(null, null);

        public LayoutConstraint Inset(ThicknessValue thickness)
        {
            return new LayoutConstraint(
                Width is float width
                    ? MathF.Max(0f, width - ToFloat(thickness.Left + thickness.Right))
                    : null,
                Height is float height
                    ? MathF.Max(0f, height - ToFloat(thickness.Top + thickness.Bottom))
                    : null);
        }

        public LayoutConstraint WithExplicitSize(UiVector2 size)
        {
            return new LayoutConstraint(
                size.X > 0f ? size.X : Width,
                size.Y > 0f ? size.Y : Height);
        }
    }

    private static readonly string[] UnsupportedPropertyNames =
    {
        "FontFamily",
        "FontWeight",
        "Grid.RowSpan",
        "Rotate",
        "ScaleX",
        "ScaleY",
        "TranslateX",
        "TranslateY",
        "VerticalAlignment",
        "VerticalContentAlignment"
    };

    private readonly record struct GridCell(int Row, int Column, int DeclarationIndex, VirtualEntry Entry);

    private readonly record struct PanelDecoration(
        UiRect Rect,
        UiColor? Background,
        UiColor? Border,
        float BorderThickness,
        float CornerRadius,
        UiRect? ClipRect);

    private sealed class ScrollRegionState
    {
        public UiRect Bounds { get; set; }

        public float Offset { get; set; }

        public float MaxOffset { get; set; }

        public float Velocity { get; set; }

        public UiRect ScrollbarTrack { get; set; }

        public UiRect ScrollbarHandle { get; set; }

        public bool IsDragging { get; set; }

        public float DragOffsetY { get; set; }

        public int Order { get; set; }

        public long LastSeenFrame { get; set; }
    }
}
