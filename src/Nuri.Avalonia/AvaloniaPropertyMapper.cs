using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Nuri.UI.Values;
using AvaloniaThickness = Avalonia.Thickness;
using AvaloniaCornerRadius = Avalonia.CornerRadius;

namespace Nuri.Avalonia
{
    internal static class AvaloniaPropertyMapper
    {
        public static bool TrySetProperty(Control control, string propertyName, object? value)
        {
            switch (propertyName)
            {
                case "Width":
                    control.Width = ToDouble(value);
                    return true;
                case "Height":
                    control.Height = ToDouble(value);
                    return true;
                case "Opacity":
                    control.Opacity = ToDouble(value);
                    return true;
                case "Margin":
                    control.Margin = ToThickness(value);
                    return true;
                case "Padding":
                    return TrySetPadding(control, value);
                case "Background":
                    return TrySetBackground(control, value);
                case "Foreground":
                    return TrySetForeground(control, value);
                case "BorderBrush":
                    return TrySetBorderBrush(control, value);
                case "BorderThickness":
                    return TrySetBorderThickness(control, value);
                case "CornerRadius":
                    return TrySetCornerRadius(control, value);
                case "Text":
                    return TrySetText(control, value);
                case "Content":
                    return TrySetContent(control, value);
                case "FontSize":
                    return TrySetFontSize(control, value);
                case "FontWeight":
                    return TrySetFontWeight(control, value);
                case "HorizontalAlignment":
                    control.HorizontalAlignment = MapHorizontalAlignment(value);
                    return true;
                case "VerticalAlignment":
                    control.VerticalAlignment = MapVerticalAlignment(value);
                    return true;
                case "HorizontalContentAlignment":
                    return TrySetHorizontalContentAlignment(control, value);
                case "VerticalContentAlignment":
                    return TrySetVerticalContentAlignment(control, value);
                case "Grid.Row":
                    Grid.SetRow(control, Convert.ToInt32(value));
                    return true;
                case "Grid.Column":
                    Grid.SetColumn(control, Convert.ToInt32(value));
                    return true;
                case "RowDefinitions":
                    return TrySetRowDefinitions(control, value);
                case "ColumnDefinitions":
                    return TrySetColumnDefinitions(control, value);
                default:
                    return false;
            }
        }

        public static bool TryResetProperty(Control control, string propertyName)
        {
            switch (propertyName)
            {
                case "Width":
                    control.ClearValue(Layoutable.WidthProperty);
                    return true;
                case "Height":
                    control.ClearValue(Layoutable.HeightProperty);
                    return true;
                case "Opacity":
                    control.ClearValue(Visual.OpacityProperty);
                    return true;
                case "Margin":
                    control.ClearValue(Layoutable.MarginProperty);
                    return true;
                case "Text" when control is TextBlock textBlock:
                    textBlock.Text = null;
                    return true;
                case "Text" when control is TextBox textBox:
                    textBox.Text = string.Empty;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TrySetPadding(Control control, object? value)
        {
            var thickness = ToThickness(value);
            if (control is Decorator decorator)
            {
                decorator.Padding = thickness;
                return true;
            }

            if (control is TemplatedControl templatedControl)
            {
                templatedControl.Padding = thickness;
                return true;
            }

            return false;
        }

        private static bool TrySetBackground(Control control, object? value)
        {
            var brush = ToBrush(value);
            if (control is Panel panel)
            {
                panel.Background = brush;
                return true;
            }

            if (control is Border border)
            {
                border.Background = brush;
                return true;
            }

            if (control is TemplatedControl templatedControl)
            {
                templatedControl.Background = brush;
                return true;
            }

            return false;
        }

        private static bool TrySetForeground(Control control, object? value)
        {
            var brush = ToBrush(value);
            if (control is TextBlock textBlock)
            {
                textBlock.Foreground = brush;
                return true;
            }

            if (control is TemplatedControl templatedControl)
            {
                templatedControl.Foreground = brush;
                return true;
            }

            return false;
        }

        private static bool TrySetBorderBrush(Control control, object? value)
        {
            var brush = ToBrush(value);
            if (control is Border border)
            {
                border.BorderBrush = brush;
                return true;
            }

            if (control is TemplatedControl templatedControl)
            {
                templatedControl.BorderBrush = brush;
                return true;
            }

            return false;
        }

        private static bool TrySetBorderThickness(Control control, object? value)
        {
            var thickness = ToThickness(value);
            if (control is Border border)
            {
                border.BorderThickness = thickness;
                return true;
            }

            if (control is TemplatedControl templatedControl)
            {
                templatedControl.BorderThickness = thickness;
                return true;
            }

            return false;
        }

        private static bool TrySetCornerRadius(Control control, object? value)
        {
            var radius = ToCornerRadius(value);
            if (control is Border border)
            {
                border.CornerRadius = radius;
                return true;
            }

            if (control is TemplatedControl templatedControl)
            {
                templatedControl.CornerRadius = radius;
                return true;
            }

            return false;
        }

        private static bool TrySetText(Control control, object? value)
        {
            var text = value as string ?? string.Empty;
            if (control is TextBlock textBlock)
            {
                textBlock.Text = text;
                return true;
            }

            if (control is TextBox textBox)
            {
                if (!string.Equals(textBox.Text, text, StringComparison.Ordinal))
                    textBox.Text = text;
                return true;
            }

            return false;
        }

        private static bool TrySetContent(Control control, object? value)
        {
            if (control is ContentControl contentControl)
            {
                contentControl.Content = value;
                return true;
            }

            return false;
        }

        private static bool TrySetFontSize(Control control, object? value)
        {
            if (control is TextBlock textBlock)
            {
                textBlock.FontSize = ToDouble(value);
                return true;
            }

            if (control is TemplatedControl templatedControl)
            {
                templatedControl.FontSize = ToDouble(value);
                return true;
            }

            return false;
        }

        private static bool TrySetFontWeight(Control control, object? value)
        {
            var fontWeight = value is FontWeightValue weight && weight.OpenTypeWeight >= 700 ? FontWeight.Bold : FontWeight.Normal;
            if (control is TextBlock textBlock)
            {
                textBlock.FontWeight = fontWeight;
                return true;
            }

            if (control is TemplatedControl templatedControl)
            {
                templatedControl.FontWeight = fontWeight;
                return true;
            }

            return false;
        }

        private static bool TrySetHorizontalContentAlignment(Control control, object? value)
        {
            if (control is ContentControl contentControl)
            {
                contentControl.HorizontalContentAlignment = MapHorizontalAlignment(value);
                return true;
            }

            return false;
        }

        private static bool TrySetVerticalContentAlignment(Control control, object? value)
        {
            if (control is ContentControl contentControl)
            {
                contentControl.VerticalContentAlignment = MapVerticalAlignment(value);
                return true;
            }

            return false;
        }

        private static bool TrySetRowDefinitions(Control control, object? value)
        {
            if (control is not Grid grid || value is not IEnumerable<LengthValue> rows)
                return false;

            grid.RowDefinitions.Clear();
            foreach (var row in rows)
                grid.RowDefinitions.Add(new RowDefinition(ToGridLength(row)));
            return true;
        }

        private static bool TrySetColumnDefinitions(Control control, object? value)
        {
            if (control is not Grid grid || value is not IEnumerable<LengthValue> columns)
                return false;

            grid.ColumnDefinitions.Clear();
            foreach (var column in columns)
                grid.ColumnDefinitions.Add(new ColumnDefinition(ToGridLength(column)));
            return true;
        }

        private static AvaloniaThickness ToThickness(object? value)
        {
            if (value is ThicknessValue thickness)
                return new AvaloniaThickness(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

            var uniform = ToDouble(value);
            return new AvaloniaThickness(uniform);
        }

        private static AvaloniaCornerRadius ToCornerRadius(object? value)
        {
            if (value is CornerRadiusValue radius)
                return new AvaloniaCornerRadius(radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft);

            return new AvaloniaCornerRadius(ToDouble(value));
        }

        private static IBrush? ToBrush(object? value)
        {
            if (value is BrushValue.Solid solid)
                return new SolidColorBrush(new Color(solid.Color.A, solid.Color.R, solid.Color.G, solid.Color.B));

            return value as IBrush;
        }

        private static GridLength ToGridLength(LengthValue value)
        {
            return value.Unit switch
            {
                LengthUnit.Auto => GridLength.Auto,
                LengthUnit.Star => new GridLength(value.Value, GridUnitType.Star),
                _ => new GridLength(value.Value, GridUnitType.Pixel)
            };
        }

        private static HorizontalAlignment MapHorizontalAlignment(object? value)
        {
            return value?.ToString() switch
            {
                "Start" => HorizontalAlignment.Left,
                "Center" => HorizontalAlignment.Center,
                "End" => HorizontalAlignment.Right,
                "Stretch" => HorizontalAlignment.Stretch,
                _ => HorizontalAlignment.Stretch
            };
        }

        private static VerticalAlignment MapVerticalAlignment(object? value)
        {
            return value?.ToString() switch
            {
                "Start" => VerticalAlignment.Top,
                "Center" => VerticalAlignment.Center,
                "End" => VerticalAlignment.Bottom,
                "Stretch" => VerticalAlignment.Stretch,
                _ => VerticalAlignment.Stretch
            };
        }

        private static double ToDouble(object? value)
        {
            return value == null ? 0 : Convert.ToDouble(value);
        }
    }
}
