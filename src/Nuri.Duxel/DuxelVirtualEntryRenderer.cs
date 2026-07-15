using Duxel.Core;
using Nuri.Constants;
using Nuri.UI.Controls;
using Nuri.UI.Events;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.Duxel;

public sealed class DuxelVirtualEntryRenderer
{
    public void Render(UiImmediateContext ui, VirtualEntry entry)
    {
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(entry);

        RenderEntry(ui, entry);
    }

    private static void RenderEntry(UiImmediateContext ui, VirtualEntry entry)
    {
        var styleScope = PushEntryStyle(ui, entry);
        try
        {
            RenderEntryCore(ui, entry);
        }
        finally
        {
            if (styleScope.ColorCount > 0)
            {
                ui.PopStyleColor(styleScope.ColorCount);
            }

            if (styleScope.FontSizePushed)
            {
                ui.PopFontSize();
            }
        }
    }

    private static void RenderEntryCore(UiImmediateContext ui, VirtualEntry entry)
    {
        switch (entry.Type)
        {
            case VirtualControlTypes.Window:
                RenderChildren(ui, entry);
                return;
            case VirtualControlTypes.Div:
                RenderDiv(ui, entry);
                return;
            case VirtualControlTypes.Text:
                ui.Text(GetString(entry, PropertyKeys.Text));
                return;
            case VirtualControlTypes.Input:
                RenderInput(ui, entry);
                return;
            default:
                RenderChildren(ui, entry);
                return;
        }
    }

    private static void RenderDiv(UiImmediateContext ui, VirtualEntry entry)
    {
        var isScroll = string.Equals(entry.Kind, DivTypes.Scroll, StringComparison.Ordinal);
        if (isScroll)
        {
            _ = ui.BeginChild(
                string.IsNullOrWhiteSpace(entry.Id) ? "NuriScroll" : entry.Id,
                GetSize(entry),
                border: HasVisibleBorder(entry));
        }

        try
        {
            RenderDivContent(ui, entry);
        }
        finally
        {
            if (isScroll)
            {
                ui.EndChild();
            }
        }
    }

    private static void RenderDivContent(UiImmediateContext ui, VirtualEntry entry)
    {
        var padding = GetThickness(entry, "Padding");
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
                        RenderChildren(ui, entry);
                    }
                    finally
                    {
                        ui.EndRow();
                    }

                    break;
                case DivTypes.Grid:
                    RenderGrid(ui, entry, ToFloat(padding.Right));
                    break;
                default:
                    RenderChildren(ui, entry);
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

    private static void RenderGrid(UiImmediateContext ui, VirtualEntry entry, float rightPadding)
    {
        var columnDefinitions = GetColumnDefinitions(entry);
        var cells = BuildGridCells(entry, columnDefinitions.Count);
        var columnCount = Math.Max(
            columnDefinitions.Count,
            cells.Count == 0 ? 1 : cells.Max(cell => cell.Column) + 1);

        ui.Columns(columnCount);
        try
        {
            ApplyColumnWidths(ui, columnDefinitions, columnCount, rightPadding);

            var currentSlot = 0;
            foreach (var cellGroup in cells.GroupBy(cell => cell.Row * columnCount + cell.Column).OrderBy(group => group.Key))
            {
                while (currentSlot < cellGroup.Key)
                {
                    ui.NextColumn();
                    currentSlot++;
                }

                foreach (var cell in cellGroup.OrderBy(cell => cell.DeclarationIndex))
                {
                    RenderEntry(ui, cell.Entry);
                }
            }
        }
        finally
        {
            ui.Columns(1);
        }
    }

    private static List<GridCell> BuildGridCells(VirtualEntry entry, int definedColumnCount)
    {
        var cells = new List<GridCell>(entry.Children.Count);
        var fallbackColumnCount = Math.Max(1, definedColumnCount);

        for (var index = 0; index < entry.Children.Count; index++)
        {
            var child = entry.Children[index];
            var hasExplicitRow = TryGetInt32(child, "Grid.Row", out var row);
            var hasExplicitColumn = TryGetInt32(child, "Grid.Column", out var column);

            if (!hasExplicitRow)
            {
                row = index / fallbackColumnCount;
            }

            if (!hasExplicitColumn)
            {
                column = index % fallbackColumnCount;
            }

            cells.Add(new GridCell(Math.Max(0, row), Math.Max(0, column), index, child));
        }

        return cells;
    }

    private static void ApplyColumnWidths(
        UiImmediateContext ui,
        IReadOnlyList<LengthValue> definitions,
        int columnCount,
        float rightPadding)
    {
        if (definitions.Count == 0)
        {
            return;
        }

        var itemSpacing = ui.GetItemSpacing().X;
        var availableWidth = MathF.Max(
            columnCount,
            ui.GetContentRegionAvail().X - rightPadding - (itemSpacing * (columnCount - 1)));
        var fixedWidth = 0f;
        var flexibleWeight = 0f;

        for (var index = 0; index < columnCount; index++)
        {
            var definition = index < definitions.Count ? definitions[index] : LengthValue.Auto();
            if (definition.Unit == LengthUnit.Pixel)
            {
                fixedWidth += MathF.Max(1f, ToFloat(definition.Value));
            }
            else
            {
                flexibleWeight += definition.Unit == LengthUnit.Star
                    ? MathF.Max(0f, ToFloat(definition.Value))
                    : 1f;
            }
        }

        var flexibleWidth = MathF.Max(0f, availableWidth - fixedWidth);
        for (var index = 0; index < columnCount; index++)
        {
            var definition = index < definitions.Count ? definitions[index] : LengthValue.Auto();
            var width = definition.Unit switch
            {
                LengthUnit.Pixel => MathF.Max(1f, ToFloat(definition.Value)),
                LengthUnit.Star when flexibleWeight > 0f =>
                    flexibleWidth * MathF.Max(0f, ToFloat(definition.Value)) / flexibleWeight,
                LengthUnit.Auto when flexibleWeight > 0f => flexibleWidth / flexibleWeight,
                _ => ui.GetColumnWidth(index)
            };

            ui.SetColumnWidth(index, MathF.Max(1f, width));
        }
    }

    private static void RenderInput(UiImmediateContext ui, VirtualEntry entry)
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
                if (size.X > 0f)
                {
                    ui.SetNextItemWidth(size.X);
                }

                var value = GetString(entry, PropertyKeys.Text);
                if (ui.InputText(widgetLabel, ref value, 4096))
                {
                    InvokeValueEvent(entry, VirtualEventKind.TextChanged, value);
                }

                return;
            }
            default:
                if (ui.Button(widgetLabel, size))
                {
                    InvokeActionEvent(entry, VirtualEventKind.Click);
                }

                return;
        }
    }

    private static EntryStyleScope PushEntryStyle(UiImmediateContext ui, VirtualEntry entry)
    {
        var fontSizePushed = TryGetSingle(entry, "FontSize", out var fontSize) && fontSize > 0f;
        if (fontSizePushed)
        {
            ui.PushFontSize(fontSize);
        }

        var colorCount = 0;
        if (TryGetColor(entry, PropertyKeys.Foreground, out var foreground))
        {
            var color = ToUiColor(foreground);
            ui.PushStyleColor(UiStyleColor.Text, color);
            ui.PushStyleColor(UiStyleColor.ButtonText, color);
            ui.PushStyleColor(UiStyleColor.CheckboxText, color);
            ui.PushStyleColor(UiStyleColor.RadioButtonText, color);
            ui.PushStyleColor(UiStyleColor.InputText, color);
            colorCount += 5;
        }

        if (entry.Type == VirtualControlTypes.Input
            && TryGetColor(entry, PropertyKeys.Background, out var background))
        {
            ui.PushStyleColor(GetInputBackgroundStyle(entry.Kind), ToUiColor(background));
            colorCount++;
        }

        return new EntryStyleScope(fontSizePushed, colorCount);
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
            if (TryGetSingle(entry, PropertyKeys.ColumnSpacing, out var columnSpacing))
            {
                horizontal = columnSpacing;
                hasOverride = true;
            }

            if (TryGetSingle(entry, PropertyKeys.RowSpacing, out var rowSpacing))
            {
                vertical = rowSpacing;
                hasOverride = true;
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

    private static void RenderChildren(UiImmediateContext ui, VirtualEntry entry)
    {
        foreach (var child in entry.Children)
        {
            RenderEntry(ui, child);
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

    private readonly record struct EntryStyleScope(bool FontSizePushed, int ColorCount);

    private readonly record struct GridCell(int Row, int Column, int DeclarationIndex, VirtualEntry Entry);
}
