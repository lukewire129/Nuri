using System;
using Nuri.Constants;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using Nuri.UI.Virtualization;
using Nuri.UI.Values;
using WpfBorder = System.Windows.Controls.Border;
using WpfContentControl = System.Windows.Controls.ContentControl;
using WpfPanel = System.Windows.Controls.Panel;
using WpfPath = System.Windows.Shapes.Path;
using WpfShape = System.Windows.Shapes.Shape;

namespace Nuri.WPF
{
    internal static class WpfPropertyMapper
    {
        public static bool TrySetProperty(FrameworkElement element, string propertyName, object? value)
        {
            switch (propertyName)
            {
                case PropertyKeys.VirtualizedItemsSource:
                    if (element is WpfVirtualizedItemsHost virtualizedHost && value is IVirtualizedItemsSource source)
                        virtualizedHost.SetSource(source);
                    return element is WpfVirtualizedItemsHost;
                case PropertyKeys.Width:
                    element.Width = ToDouble(value);
                    return true;
                case PropertyKeys.Height:
                    element.Height = ToDouble(value);
                    return true;
                case "Margin":
                    element.Margin = (Thickness)value!;
                    return true;
                case "Rotate":
                    WpfTransformMapper.GetRotate(element).Angle = ToDouble(value);
                    return true;
                case "ScaleX":
                    WpfTransformMapper.GetScale(element).ScaleX = ToDouble(value);
                    return true;
                case "ScaleY":
                    WpfTransformMapper.GetScale(element).ScaleY = ToDouble(value);
                    return true;
                case "TranslateX":
                    WpfTransformMapper.GetTranslate(element).X = ToDouble(value);
                    return true;
                case "TranslateY":
                    WpfTransformMapper.GetTranslate(element).Y = ToDouble(value);
                    return true;
                case PropertyKeys.Name:
                    element.Name = (string)value!;
                    return true;
                case "Tag":
                    element.Tag = value;
                    return true;
                case "Cursor":
                    element.Cursor = (Cursor?)value;
                    return true;
                case "Effect":
                    element.Effect = (Effect?)value;
                    return true;
                case "HorizontalAlignment":
                    element.HorizontalAlignment = (HorizontalAlignment)value!;
                    return true;
                case "VerticalAlignment":
                    element.VerticalAlignment = (VerticalAlignment)value!;
                    return true;
                case PropertyKeys.Background:
                    return TrySetBackground(element, value);
                case PropertyKeys.Foreground:
                    return TrySetForeground(element, value);
                case "FontSize":
                    return TrySetFontSize(element, value);
                case "FontWeight":
                    return TrySetFontWeight(element, value);
                case "FontFamily":
                    return TrySetFontFamily(element, value);
                case "Padding":
                    return TrySetPadding(element, value);
                case "BorderBrush":
                    return TrySetBorderBrush(element, value);
                case "BorderThickness":
                    return TrySetBorderThickness(element, value);
                case "CornerRadius":
                    return TrySetCornerRadius(element, value);
                case PropertyKeys.Text:
                    return TrySetText(element, value);
                case PropertyKeys.TextOverflow:
                    return TrySetTextOverflow(element, value);
                case PropertyKeys.IsChecked:
                    return TrySetIsChecked(element, value);
                case "Content":
                    return TrySetContent(element, value);
                case "Source":
                    return TrySetSource(element, value);
                case "Stretch":
                    return TrySetStretch(element, value);
                case "Orientation":
                    return TrySetOrientation(element, value);
                case PropertyKeys.Spacing:
                    return TrySetSpacing(element, value);
                case PropertyKeys.JustifyContent:
                    return TrySetJustifyContent(element, value);
                case PropertyKeys.Grow:
                    WpfDistributedStackPanel.SetGrow(element, ToDouble(value));
                    return true;
                case PropertyKeys.RowSpacing:
                    return TrySetRowSpacing(element, value);
                case PropertyKeys.ColumnSpacing:
                    return TrySetColumnSpacing(element, value);
                case "HorizontalContentAlignment":
                    return TrySetHorizontalContentAlignment(element, value);
                case "VerticalContentAlignment":
                    return TrySetVerticalContentAlignment(element, value);
                case "Data":
                    return TrySetData(element, value);
                case "Stroke":
                    return TrySetStroke(element, value);
                case "StrokeThickness":
                    return TrySetStrokeThickness(element, value);
                case "Fill":
                    return TrySetFill(element, value);
                case PropertyKeys.AutoFocus:
                    return TrySetAutoFocus(element, value);
                case PropertyKeys.BringIntoView:
                    return TrySetBringIntoView(element, value);
                default:
                    return false;
            }
        }

        public static bool TryResetProperty(FrameworkElement element, string propertyName)
        {
            switch (propertyName)
            {
                case PropertyKeys.Width:
                    element.ClearValue(FrameworkElement.WidthProperty);
                    return true;
                case PropertyKeys.Height:
                    element.ClearValue(FrameworkElement.HeightProperty);
                    return true;
                case "Margin":
                    element.ClearValue(FrameworkElement.MarginProperty);
                    return true;
                case "Rotate":
                    WpfTransformMapper.GetRotate(element).ClearValue(RotateTransform.AngleProperty);
                    return true;
                case "ScaleX":
                    WpfTransformMapper.GetScale(element).ClearValue(ScaleTransform.ScaleXProperty);
                    return true;
                case "ScaleY":
                    WpfTransformMapper.GetScale(element).ClearValue(ScaleTransform.ScaleYProperty);
                    return true;
                case "TranslateX":
                    WpfTransformMapper.GetTranslate(element).ClearValue(TranslateTransform.XProperty);
                    return true;
                case "TranslateY":
                    WpfTransformMapper.GetTranslate(element).ClearValue(TranslateTransform.YProperty);
                    return true;
                case PropertyKeys.Name:
                    element.ClearValue(FrameworkElement.NameProperty);
                    return true;
                case "Tag":
                    element.ClearValue(FrameworkElement.TagProperty);
                    return true;
                case "Cursor":
                    element.ClearValue(FrameworkElement.CursorProperty);
                    return true;
                case "Effect":
                    element.ClearValue(UIElement.EffectProperty);
                    return true;
                case "HorizontalAlignment":
                    element.ClearValue(FrameworkElement.HorizontalAlignmentProperty);
                    return true;
                case "VerticalAlignment":
                    element.ClearValue(FrameworkElement.VerticalAlignmentProperty);
                    return true;
                case PropertyKeys.Background:
                    return TryClearBackground(element);
                case PropertyKeys.Foreground:
                    return TryClearForeground(element);
                case "FontSize":
                    return TryClearFontSize(element);
                case "FontWeight":
                    return TryClearFontWeight(element);
                case "FontFamily":
                    return TryClearFontFamily(element);
                case "Padding":
                    return TryClearPadding(element);
                case "BorderBrush":
                    return TryClearBorderBrush(element);
                case "BorderThickness":
                    return TryClearBorderThickness(element);
                case "CornerRadius":
                    return TryClearCornerRadius(element);
                case PropertyKeys.Text:
                    return TryClearText(element);
                case PropertyKeys.TextOverflow:
                    return TryClearTextOverflow(element);
                case PropertyKeys.IsChecked:
                    return TryClearIsChecked(element);
                case "Content":
                    return TryClearContent(element);
                case "Source":
                    return TryClearSource(element);
                case "Stretch":
                    return TryClearStretch(element);
                case "Orientation":
                    return TryClearOrientation(element);
                case PropertyKeys.Spacing:
                    return TryClearSpacing(element);
                case PropertyKeys.JustifyContent:
                    return TryClearJustifyContent(element);
                case PropertyKeys.Grow:
                    element.ClearValue(WpfDistributedStackPanel.GrowProperty);
                    return true;
                case PropertyKeys.RowSpacing:
                    return TryClearRowSpacing(element);
                case PropertyKeys.ColumnSpacing:
                    return TryClearColumnSpacing(element);
                case "HorizontalContentAlignment":
                    return TryClearHorizontalContentAlignment(element);
                case "VerticalContentAlignment":
                    return TryClearVerticalContentAlignment(element);
                case "Data":
                    return TryClearData(element);
                case "Stroke":
                    return TryClearStroke(element);
                case "StrokeThickness":
                    return TryClearStrokeThickness(element);
                case "Fill":
                    return TryClearFill(element);
                case PropertyKeys.AutoFocus:
                    return true;
                case PropertyKeys.BringIntoView:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TrySetBackground(FrameworkElement element, object? value)
        {
            var brush = (Brush?)value;
            if (element is Control control)
                control.Background = brush;
            else if (element is WpfPanel panel)
                panel.Background = brush;
            else if (element is WpfBorder border)
                border.Background = brush;
            else
                return false;

            return true;
        }

        private static bool TryClearBackground(FrameworkElement element)
        {
            if (element is Control control)
                control.ClearValue(Control.BackgroundProperty);
            else if (element is WpfPanel panel)
                panel.ClearValue(WpfPanel.BackgroundProperty);
            else if (element is WpfBorder border)
                border.ClearValue(WpfBorder.BackgroundProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetForeground(FrameworkElement element, object? value)
        {
            var brush = (Brush?)value;
            if (element is Control control)
                control.Foreground = brush;
            else if (element is TextBlock textBlock)
                textBlock.Foreground = brush;
            else
                return false;

            return true;
        }

        private static bool TryClearForeground(FrameworkElement element)
        {
            if (element is Control control)
                control.ClearValue(Control.ForegroundProperty);
            else if (element is TextBlock textBlock)
                textBlock.ClearValue(TextBlock.ForegroundProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetTextOverflow(FrameworkElement element, object? value)
        {
            if (element is not TextBlock textBlock || value is not TextOverflowValue overflow)
                return false;

            switch (overflow.Kind)
            {
                case TextOverflowKind.Ellipsis:
                    textBlock.TextWrapping = TextWrapping.NoWrap;
                    textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
                    break;
                case TextOverflowKind.Wrap:
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    textBlock.TextTrimming = TextTrimming.None;
                    break;
                default:
                    textBlock.TextWrapping = TextWrapping.NoWrap;
                    textBlock.TextTrimming = TextTrimming.None;
                    break;
            }

            return true;
        }

        private static bool TryClearTextOverflow(FrameworkElement element)
        {
            if (element is not TextBlock textBlock)
                return false;

            textBlock.ClearValue(TextBlock.TextWrappingProperty);
            textBlock.ClearValue(TextBlock.TextTrimmingProperty);
            return true;
        }

        private static bool TrySetFontSize(FrameworkElement element, object? value)
        {
            if (element is Control control)
                control.FontSize = ToDouble(value);
            else if (element is TextBlock textBlock)
                textBlock.FontSize = ToDouble(value);
            else
                return false;

            return true;
        }

        private static bool TryClearFontSize(FrameworkElement element)
        {
            if (element is Control control)
                control.ClearValue(Control.FontSizeProperty);
            else if (element is TextBlock textBlock)
                textBlock.ClearValue(TextBlock.FontSizeProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetFontWeight(FrameworkElement element, object? value)
        {
            if (element is Control control)
                control.FontWeight = (FontWeight)value!;
            else if (element is TextBlock textBlock)
                textBlock.FontWeight = (FontWeight)value!;
            else
                return false;

            return true;
        }

        private static bool TryClearFontWeight(FrameworkElement element)
        {
            if (element is Control control)
                control.ClearValue(Control.FontWeightProperty);
            else if (element is TextBlock textBlock)
                textBlock.ClearValue(TextBlock.FontWeightProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetFontFamily(FrameworkElement element, object? value)
        {
            var fontFamily = value is string familyName ? new FontFamily(familyName) : (FontFamily)value!;
            if (element is Control control)
                control.FontFamily = fontFamily;
            else if (element is TextBlock textBlock)
                textBlock.FontFamily = fontFamily;
            else
                return false;

            return true;
        }

        private static bool TryClearFontFamily(FrameworkElement element)
        {
            if (element is Control control)
                control.ClearValue(Control.FontFamilyProperty);
            else if (element is TextBlock textBlock)
                textBlock.ClearValue(TextBlock.FontFamilyProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetPadding(FrameworkElement element, object? value)
        {
            var padding = (Thickness)value!;
            if (element is Control control)
                control.Padding = padding;
            else if (element is WpfBorder border)
                border.Padding = padding;
            else if (element is TextBlock textBlock)
                textBlock.Padding = padding;
            else
                return false;

            return true;
        }

        private static bool TryClearPadding(FrameworkElement element)
        {
            if (element is Control control)
                control.ClearValue(Control.PaddingProperty);
            else if (element is WpfBorder border)
                border.ClearValue(WpfBorder.PaddingProperty);
            else if (element is TextBlock textBlock)
                textBlock.ClearValue(TextBlock.PaddingProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetBorderBrush(FrameworkElement element, object? value)
        {
            var brush = (Brush?)value;
            if (element is Control control)
                control.BorderBrush = brush;
            else if (element is WpfBorder border)
                border.BorderBrush = brush;
            else
                return false;

            return true;
        }

        private static bool TryClearBorderBrush(FrameworkElement element)
        {
            if (element is Control control)
                control.ClearValue(Control.BorderBrushProperty);
            else if (element is WpfBorder border)
                border.ClearValue(WpfBorder.BorderBrushProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetBorderThickness(FrameworkElement element, object? value)
        {
            var thickness = (Thickness)value!;
            if (element is Control control)
                control.BorderThickness = thickness;
            else if (element is WpfBorder border)
                border.BorderThickness = thickness;
            else
                return false;

            return true;
        }

        private static bool TryClearBorderThickness(FrameworkElement element)
        {
            if (element is Control control)
                control.ClearValue(Control.BorderThicknessProperty);
            else if (element is WpfBorder border)
                border.ClearValue(WpfBorder.BorderThicknessProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetCornerRadius(FrameworkElement element, object? value)
        {
            if (element is not WpfBorder border)
                return false;

            border.CornerRadius = (CornerRadius)value!;
            return true;
        }

        private static bool TryClearCornerRadius(FrameworkElement element)
        {
            if (element is not WpfBorder border)
                return false;

            border.ClearValue(WpfBorder.CornerRadiusProperty);
            return true;
        }

        private static bool TrySetText(FrameworkElement element, object? value)
        {
            if (element is TextBlock textBlock)
                textBlock.Text = (string?)value;
            else if (element is TextBox textBox)
            {
                var text = (string?)value ?? string.Empty;
                if (!string.Equals(textBox.Text, text, StringComparison.Ordinal))
                {
                    textBox.SetSuppressChangeEvents(true);
                    try
                    {
                        textBox.Text = text;
                    }
                    finally
                    {
                        textBox.SetSuppressChangeEvents(false);
                    }
                }
            }
            else
                return false;

            return true;
        }

        private static bool TryClearText(FrameworkElement element)
        {
            if (element is TextBlock textBlock)
                textBlock.ClearValue(TextBlock.TextProperty);
            else if (element is TextBox textBox)
                textBox.ClearValue(TextBox.TextProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetIsChecked(FrameworkElement element, object? value)
        {
            if (element is not ToggleButton toggleButton)
                return false;

            var nextValue = value is bool isChecked && isChecked;
            if (toggleButton.IsChecked != nextValue)
            {
                toggleButton.SetSuppressChangeEvents(true);
                try
                {
                    toggleButton.IsChecked = nextValue;
                }
                finally
                {
                    toggleButton.SetSuppressChangeEvents(false);
                }
            }

            return true;
        }

        private static bool TryClearIsChecked(FrameworkElement element)
        {
            if (element is not ToggleButton toggleButton)
                return false;

            toggleButton.ClearValue(ToggleButton.IsCheckedProperty);
            return true;
        }

        private static bool TrySetContent(FrameworkElement element, object? value)
        {
            if (element is not WpfContentControl contentControl)
                return false;

            contentControl.Content = value;
            return true;
        }

        private static bool TryClearContent(FrameworkElement element)
        {
            if (element is not WpfContentControl contentControl)
                return false;

            contentControl.ClearValue(WpfContentControl.ContentProperty);
            return true;
        }

        private static bool TrySetSource(FrameworkElement element, object? value)
        {
            if (element is not Image image)
                return false;

            image.Source = ToImageSource(value);
            return true;
        }

        private static bool TryClearSource(FrameworkElement element)
        {
            if (element is not Image image)
                return false;

            image.ClearValue(Image.SourceProperty);
            return true;
        }

        private static bool TrySetStretch(FrameworkElement element, object? value)
        {
            if (element is Image image)
                image.Stretch = (Stretch)value!;
            else if (element is WpfShape shape)
                shape.Stretch = (Stretch)value!;
            else
                return false;

            return true;
        }

        private static bool TryClearStretch(FrameworkElement element)
        {
            if (element is Image image)
                image.ClearValue(Image.StretchProperty);
            else if (element is WpfShape shape)
                shape.ClearValue(WpfShape.StretchProperty);
            else
                return false;

            return true;
        }

        private static bool TrySetOrientation(FrameworkElement element, object? value)
        {
            if (element is WpfDistributedStackPanel distributedPanel)
            {
                distributedPanel.Orientation = (Orientation)value!;
                return true;
            }

            if (element is StackPanel stackPanel)
            {
                stackPanel.Orientation = (Orientation)value!;
                return true;
            }

            return false;
        }

        private static bool TryClearOrientation(FrameworkElement element)
        {
            if (element is WpfDistributedStackPanel distributedPanel)
            {
                distributedPanel.Orientation = Orientation.Vertical;
                return true;
            }

            if (element is not StackPanel stackPanel)
                return false;

            stackPanel.ClearValue(StackPanel.OrientationProperty);
            return true;
        }

        private static bool TrySetSpacing(FrameworkElement element, object? value)
        {
            if (element is not WpfDistributedStackPanel panel)
                return false;

            panel.Spacing = ToDouble(value);
            return true;
        }

        private static bool TryClearSpacing(FrameworkElement element)
        {
            if (element is not WpfDistributedStackPanel panel)
                return false;

            panel.Spacing = 0;
            return true;
        }

        private static bool TrySetJustifyContent(FrameworkElement element, object? value)
        {
            if (element is not WpfDistributedStackPanel panel || value is not Nuri.UI.Values.ContentDistribution distribution)
                return false;

            panel.JustifyContent = distribution;
            return true;
        }

        private static bool TryClearJustifyContent(FrameworkElement element)
        {
            if (element is not WpfDistributedStackPanel panel)
                return false;

            panel.JustifyContent = Nuri.UI.Values.ContentDistribution.Start;
            return true;
        }

        private static bool TrySetRowSpacing(FrameworkElement element, object? value)
        {
            if (element is not NuriGrid grid)
                return false;

            grid.RowSpacing = ToDouble(value);
            return true;
        }

        private static bool TryClearRowSpacing(FrameworkElement element)
        {
            if (element is not NuriGrid grid)
                return false;

            grid.RowSpacing = 0;
            return true;
        }

        private static bool TrySetColumnSpacing(FrameworkElement element, object? value)
        {
            if (element is not NuriGrid grid)
                return false;

            grid.ColumnSpacing = ToDouble(value);
            return true;
        }

        private static bool TryClearColumnSpacing(FrameworkElement element)
        {
            if (element is not NuriGrid grid)
                return false;

            grid.ColumnSpacing = 0;
            return true;
        }

        private static bool TrySetHorizontalContentAlignment(FrameworkElement element, object? value)
        {
            if (element is not Control control)
                return false;
            control.HorizontalContentAlignment = (HorizontalAlignment)value!;
            return true;
        }

        private static bool TryClearHorizontalContentAlignment(FrameworkElement element)
        {
            if (element is not Control control)
                return false;

            control.ClearValue(Control.HorizontalContentAlignmentProperty);
            return true;
        }

        private static bool TrySetVerticalContentAlignment(FrameworkElement element, object? value)
        {
            if (element is not Control control)
                return false;
            control.VerticalContentAlignment = (VerticalAlignment)value!;
            return true;
        }

        private static bool TryClearVerticalContentAlignment(FrameworkElement element)
        {
            if (element is not Control control)
                return false;

            control.ClearValue(Control.VerticalContentAlignmentProperty);
            return true;
        }

        private static bool TrySetData(FrameworkElement element, object? value)
        {
            if (element is not WpfPath path)
                return false;
            path.Data = (Geometry?)value;
            return true;
        }

        private static bool TryClearData(FrameworkElement element)
        {
            if (element is not WpfPath path)
                return false;

            path.ClearValue(WpfPath.DataProperty);
            return true;
        }

        private static bool TrySetStroke(FrameworkElement element, object? value)
        {
            if (element is not WpfShape shape)
                return false;
            shape.Stroke = (Brush?)value;
            return true;
        }

        private static bool TryClearStroke(FrameworkElement element)
        {
            if (element is not WpfShape shape)
                return false;

            shape.ClearValue(WpfShape.StrokeProperty);
            return true;
        }

        private static bool TrySetStrokeThickness(FrameworkElement element, object? value)
        {
            if (element is not WpfShape shape)
                return false;
            shape.StrokeThickness = ToDouble(value);
            return true;
        }

        private static bool TryClearStrokeThickness(FrameworkElement element)
        {
            if (element is not WpfShape shape)
                return false;

            shape.ClearValue(WpfShape.StrokeThicknessProperty);
            return true;
        }

        private static bool TrySetFill(FrameworkElement element, object? value)
        {
            if (element is not WpfShape shape)
                return false;
            shape.Fill = (Brush?)value;
            return true;
        }

        private static bool TrySetAutoFocus(FrameworkElement element, object? value)
        {
            if (value is not bool autoFocus || !autoFocus)
                return true;

            element.Loaded += FocusWhenLoaded;
            return true;
        }

        private static bool TrySetBringIntoView(FrameworkElement element, object? value)
        {
            if (value is not bool bringIntoView || !bringIntoView)
                return true;

            if (!element.IsLoaded)
                element.Loaded += BringIntoViewWhenLoaded;
            else
                element.Dispatcher.BeginInvoke((Action)element.BringIntoView);

            return true;
        }

        private static void FocusWhenLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            element.Loaded -= FocusWhenLoaded;
            element.Dispatcher.BeginInvoke((Action)(() => element.Focus()));
        }

        private static void BringIntoViewWhenLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            element.Loaded -= BringIntoViewWhenLoaded;
            element.Dispatcher.BeginInvoke((Action)element.BringIntoView);
        }

        private static bool TryClearFill(FrameworkElement element)
        {
            if (element is not WpfShape shape)
                return false;

            shape.ClearValue(WpfShape.FillProperty);
            return true;
        }

        private static double ToDouble(object? value)
        {
            return Convert.ToDouble(value);
        }

        private static ImageSource? ToImageSource(object? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case ImageSource imageSource:
                    return imageSource;
                case string sourcePath:
                    return CreateBitmapImage(sourcePath);
                default:
                    return (ImageSource?)value;
            }
        }

        private static BitmapImage CreateBitmapImage(string sourcePath)
        {
            var imageUri = System.IO.Path.IsPathRooted(sourcePath)
                ? new Uri(sourcePath, UriKind.Absolute)
                : new Uri($"pack://application:,,,/{sourcePath}", UriKind.RelativeOrAbsolute);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = imageUri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
    }
}
