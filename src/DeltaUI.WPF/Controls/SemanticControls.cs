using DeltaUI.Core.UI;
using DeltaUI.Core.UI.Controls;
using DeltaUI.Core.UI.Values;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DeltaUI.WPF
{
    public interface IDiv : IVisual
    {
    }

    public interface IImage : IVisual
    {
    }

    public interface IItems : IVisual
    {
    }

    public interface IOverlay : IVisual
    {
    }

    public interface ISelect : IVisual
    {
    }

    public abstract partial class Component
    {
        public static Div Div(params IElement[] children)
        {
            return new Div(DivTypes.Column, children);
        }

        public static Div Div(string kind, params IElement[] children)
        {
            return new Div(kind, children);
        }

        public static Div Div(RowHeights rowHeights, params IElement[] children)
        {
            return new Div(DivTypes.Grid, children)
                .ColumnDefinition(Columns().Lengths)
                .RowDefinition(rowHeights.Lengths);
        }

        public static Div Div(ColumnWidths columnWidths, params IElement[] children)
        {
            return new Div(DivTypes.Grid, children)
                .RowDefinition(Rows().Lengths)
                .ColumnDefinition(columnWidths.Lengths);
        }

        public static Div Div(RowHeights rowHeights, ColumnWidths columnWidths, params IElement[] children)
        {
            return new Div(DivTypes.Grid, children)
                .RowDefinition(rowHeights.Lengths)
                .ColumnDefinition(columnWidths.Lengths);
        }

        public static ImageElement Image()
        {
            return new ImageElement();
        }

        public static ImageElement Image(string sourcePath)
        {
            return new ImageElement(sourcePath);
        }

        public static Input Input()
        {
            return new Input(InputTypes.Text);
        }

        public static Input Input(string kind)
        {
            return new Input(kind);
        }

        public static Input Input(string kind, object content)
        {
            return new Input(kind, content);
        }

        public static Input Input(string kind, object content, RoutedEventHandler handler)
        {
            return new Input(kind, content, handler);
        }

        public static ItemsView Items(params IElement[] children)
        {
            return new ItemsView(ItemsTypes.List, children);
        }

        public static ItemsView Items(string kind, params IElement[] children)
        {
            return new ItemsView(kind, children);
        }

        public static OverlayView Overlay(params IElement[] children)
        {
            return new OverlayView(OverlayTypes.Popover, children);
        }

        public static OverlayView Overlay(string kind, params IElement[] children)
        {
            return new OverlayView(kind, children);
        }

        public static SelectView Select(params IElement[] children)
        {
            return new SelectView(SelectTypes.Dropdown, children);
        }

        public static SelectView Select(string kind, params IElement[] children)
        {
            return new SelectView(kind, children);
        }

        public static Text Text()
        {
            return new Text();
        }

        public static Text Text(string content)
        {
            return new Text(content);
        }

        public static RowHeights Rows(params GridLength[] heights) => new RowHeights { Lengths = heights };

        public static ColumnWidths Columns(params GridLength[] widths) => new ColumnWidths { Lengths = widths };

        public static GridLength Auto => new GridLength(LengthValue.Auto());

        public static GridLength Star = new GridLength(LengthValue.Star());

        public static GridLength Stars(double value) => new GridLength(LengthValue.Star(value));
    }

    public struct RowHeights
    {
        internal GridLength[] Lengths;
    }

    public struct ColumnWidths
    {
        internal GridLength[] Lengths;
    }

    public sealed class Div : Panel, IDiv
    {
        public Div() : this(DivTypes.Column)
        {
        }

        public Div(string kind, params IElement[] children) : base(VirtualControlTypes.Div, children)
        {
            Kind = kind;
        }

        private List<LengthValue> GetRowsDefinitions()
        {
            if (!Properties.TryGetValue("RowDefinitions", out var value) || value is not List<LengthValue> rows)
            {
                rows = new List<LengthValue>();
                Properties["RowDefinitions"] = rows;
            }

            return rows;
        }

        private List<LengthValue> GetColumnsDefinitions()
        {
            if (!Properties.TryGetValue("ColumnDefinitions", out var value) || value is not List<LengthValue> columns)
            {
                columns = new List<LengthValue>();
                Properties["ColumnDefinitions"] = columns;
            }

            return columns;
        }

        public Div RowDefinition(params GridLength[] heights)
        {
            var rows = GetRowsDefinitions();
            foreach (var height in heights)
            {
                rows.Add(height.Value);
            }

            return this;
        }

        public Div ColumnDefinition(params GridLength[] widths)
        {
            var columns = GetColumnsDefinitions();
            foreach (var width in widths)
            {
                columns.Add(width.Value);
            }

            return this;
        }
    }

    public sealed class ImageElement : Visual, IImage
    {
        public ImageElement() : base(VirtualControlTypes.Image, ImageTypes.Default)
        {
        }

        public ImageElement(string sourcePath) : this()
        {
            this.Source(sourcePath);
        }
    }

    public sealed class Input : ContentControl, IInput
    {
        public Input() : this(InputTypes.Text)
        {
        }

        public Input(string kind) : base(VirtualControlTypes.Input, kind)
        {
            this.SetProperty("VerticalContentAlignment", VerticalAlignment.Center);
        }

        public Input(string kind, object content) : this(kind)
        {
            this.Content(content);
        }

        public Input(string kind, object content, RoutedEventHandler handler) : this(kind, content)
        {
            this.AddEvent("Click", handler);
        }
    }

    public sealed class ItemsView : Panel, IItems
    {
        public ItemsView(string kind, params IElement[] children) : base(VirtualControlTypes.Items, children)
        {
            Kind = kind;
        }
    }

    public sealed class OverlayView : Panel, IOverlay
    {
        public OverlayView(string kind, params IElement[] children) : base(VirtualControlTypes.Overlay, children)
        {
            Kind = kind;
        }
    }

    public sealed class SelectView : Panel, ISelect
    {
        public SelectView(string kind, params IElement[] children) : base(VirtualControlTypes.Select, children)
        {
            Kind = kind;
        }
    }

    public sealed class Text : Visual
    {
        public Text() : base(VirtualControlTypes.Text)
        {
        }

        public Text(string content) : this()
        {
            this.SetProperty("Text", content);
        }
    }

    public static partial class SemanticVisualExtensions
    {
        public static T Brush<T>(this T node, System.Windows.Media.Brush brush) where T : IDiv
        {
            node.SetProperty("BorderBrush", ToCoreBrushIfSupported(brush));
            EnsureBorderThickness(node);
            return node;
        }

        public static T Brush<T>(this T node, Color color) where T : IDiv
        {
            node.SetProperty("BorderBrush", new BrushValue.Solid(WpfValueMapper.FromDrawingColor(color)));
            EnsureBorderThickness(node);
            return node;
        }

        public static T Brush<T>(this T node, string colorCode) where T : IDiv
        {
            if (colorCode[0] != '#')
                throw new System.Exception("ColorCode Error");

            node.SetProperty("BorderBrush", new BrushValue.Solid(ColorValue.FromHex(colorCode)));
            EnsureBorderThickness(node);
            return node;
        }

        public static T CornerRadius<T>(this T node, double value = 0.0) where T : IDiv
        {
            node.SetProperty("CornerRadius", CornerRadiusValue.Uniform(value));
            return node;
        }

        public static T CornerRadius<T>(this T node, double left = 0.0, double top = 0.0, double right = 0.0, double bottom = 0.0) where T : IDiv
        {
            node.SetProperty("CornerRadius", new CornerRadiusValue(left, top, right, bottom));
            return node;
        }

        public static T Group<T>(this T node, string groupName) where T : IInput
        {
            node.SetProperty("GroupName", groupName);
            return node;
        }

        public static T Padding<T>(this T node, double value = 0.0) where T : IDiv
        {
            node.SetProperty("Padding", ThicknessValue.Uniform(value));
            return node;
        }

        public static T Padding<T>(this T node, double left = 0.0, double top = 0.0, double right = 0.0, double bottom = 0.0) where T : IDiv
        {
            node.SetProperty("Padding", new ThicknessValue(left, top, right, bottom));
            return node;
        }

        public static T Source<T>(this T node, string sourcePath) where T : IImage
        {
            var bitmap = SetImageSource(sourcePath);
            if (bitmap != null)
                node.SetProperty("Source", bitmap);

            return node;
        }

        public static T Thickness<T>(this T node, double value) where T : IDiv
        {
            node.SetProperty("BorderThickness", ThicknessValue.Uniform(value));
            return node;
        }

        public static T Thickness<T>(this T node, double left = 0.0, double top = 0.0, double right = 0.0, double bottom = 0.0) where T : IDiv
        {
            node.SetProperty("BorderThickness", new ThicknessValue(left, top, right, bottom));
            return node;
        }

        private static void EnsureBorderThickness(IDiv node)
        {
            if (!node.TryGetValue("BorderThickness", out _))
                node.Thickness(1);
        }

        private static BitmapImage? SetImageSource(string path)
        {
            try
            {
                Uri imageUri;
                if (System.IO.Path.IsPathRooted(path))
                {
                    if (!System.IO.File.Exists(path))
                        throw new System.Exception("존재하지 않는 경로의 파일입니다.");

                    imageUri = new Uri(path, UriKind.Absolute);
                }
                else
                {
                    imageUri = new Uri($"pack://application:,,,/{path}", UriKind.RelativeOrAbsolute);
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = imageUri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"이미지를 로드하는 중 오류가 발생했습니다: {ex.Message}");
                return null;
            }
        }

        private static object ToCoreBrushIfSupported(System.Windows.Media.Brush brush)
        {
            return brush is System.Windows.Media.SolidColorBrush solid
                ? new BrushValue.Solid(WpfValueMapper.FromWpfColor(solid.Color))
                : brush;
        }
    }
}
