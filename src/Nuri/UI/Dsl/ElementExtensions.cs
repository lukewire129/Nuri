using System;
using System.Collections.Generic;
using Nuri.Constants;
using Nuri.UI.Events;
using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    public static class ElementExtensions
    {
        private static readonly HashSet<string> DefaultTransitionProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            PropertyKeys.Background,
            PropertyKeys.Foreground,
            "Margin",
            "Opacity",
            "Rotate"
        };

        public static T Key<T>(this T node, string key) where T : IElement
        {
            node.Key = key;
            return node;
        }

        public static T Name<T>(this T node, string name) where T : IElement
        {
            node.Name = name;
            node.SetProperty(PropertyKeys.Name, name);
            return node;
        }

        public static T Width<T>(this T node, double value) where T : IElement
        {
            node.SetProperty(PropertyKeys.Width, value);
            return node;
        }

        public static T Height<T>(this T node, double value) where T : IElement
        {
            node.SetProperty(PropertyKeys.Height, value);
            return node;
        }

        public static T Opacity<T>(this T node, double value) where T : IElement
        {
            node.SetProperty("Opacity", value);
            return node;
        }

        public static T Size<T>(this T node, double width, double height) where T : IElement
        {
            node.Width(width);
            node.Height(height);
            return node;
        }
        public static T Margin<T>(this T node, double value) where T : IElement
        {
            node.SetProperty ("Margin", ThicknessValue.Uniform (value));
            return node;
        }
        public static T Margin<T>(this T node, double left=0, double top = 0, double right = 0, double bottom =0) where T : IElement
        {
            node.SetProperty("Margin", new ThicknessValue(left, top, right, bottom));
            return node;
        }

        public static T Background<T>(this T node, ColorValue color) where T : IElement
        {
            node.SetProperty(PropertyKeys.Background, new BrushValue.Solid(color));
            return node;
        }

        public static T Background<T>(this T node, string colorCode) where T : IElement
        {
            node.SetProperty(PropertyKeys.Background, new BrushValue.Solid(ColorValue.FromHex(colorCode)));
            return node;
        }

        public static T Background<T>(this T node, BrushValue brush) where T : IElement
        {
            node.SetProperty(PropertyKeys.Background, brush);
            return node;
        }

        public static T FontColor<T>(this T node, ColorValue color) where T : IElement
        {
            node.SetProperty(PropertyKeys.Foreground, new BrushValue.Solid(color));
            return node;
        }

        public static T FontColor<T>(this T node, string colorCode) where T : IElement
        {
            node.SetProperty(PropertyKeys.Foreground, new BrushValue.Solid(ColorValue.FromHex(colorCode)));
            return node;
        }

        public static T FontSize<T>(this T node, double size) where T : IElement
        {
            node.SetProperty("FontSize", size);
            return node;
        }

        public static T FontFamily<T>(this T node, string source) where T : IElement
        {
            node.SetProperty("FontFamily", new FontFamilyValue(source));
            return node;
        }

        public static T FontWeight<T>(this T node, FontWeightValue weight) where T : IElement
        {
            node.SetProperty("FontWeight", weight);
            return node;
        }

        public static T Cursor<T>(this T node, CursorValue cursor) where T : IElement
        {
            node.SetProperty("Cursor", cursor);
            return node;
        }

        public static T BitmapScalingMode<T>(this T node, ImageScalingModeValue value) where T : IElement
        {
            node.SetProperty("RenderOptions.BitmapScalingMode", value);
            return node;
        }

        public static T Padding<T>(this T node, double value) where T : IElement
        {
            node.SetProperty("Padding", ThicknessValue.Uniform(value));
            return node;
        }

        public static T Padding<T>(this T node, double left, double top, double right, double bottom) where T : IElement
        {
            node.SetProperty("Padding", new ThicknessValue(left, top, right, bottom));
            return node;
        }

        public static T CornerRadius<T>(this T node, double value) where T : IDiv
        {
            node.SetProperty("CornerRadius", CornerRadiusValue.Uniform(value));
            return node;
        }

        public static T CornerRadius<T>(this T node, double left, double top, double right, double bottom) where T : IDiv
        {
            node.SetProperty("CornerRadius", new CornerRadiusValue(left, top, right, bottom));
            return node;
        }

        public static T Brush<T>(this T node, BrushValue brush) where T : IVisual
        {
            node.SetProperty("BorderBrush", brush);
            EnsureBorderThickness(node);
            return node;
        }

        public static T Brush<T>(this T node, ColorValue color) where T : IVisual
        {
            node.SetProperty("BorderBrush", new BrushValue.Solid(color));
            EnsureBorderThickness(node);
            return node;
        }

        public static T Brush<T>(this T node, string colorCode) where T : IVisual
        {
            node.SetProperty("BorderBrush", new BrushValue.Solid(ColorValue.FromHex(colorCode)));
            EnsureBorderThickness(node);
            return node;
        }

        public static T Thickness<T>(this T node, double value) where T : IVisual
        {
            node.SetProperty("BorderThickness", ThicknessValue.Uniform(value));
            return node;
        }

        public static T Thickness<T>(this T node, double left = 0, double top = 0, double right = 0, double bottom = 0) where T : IVisual
        {
            node.SetProperty("BorderThickness", new ThicknessValue(left, top, right, bottom));
            return node;
        }

        public static T Group<T>(this T node, string groupName) where T : IInput
        {
            node.SetProperty("GroupName", groupName);
            return node;
        }

        public static T Content<T>(this T node, object content) where T : IContent
        {
            node.SetProperty("Content", content);
            return node;
        }

        public static T Content<T>(this T node, IElement element) where T : IContent
        {
            ElementTree<IElement, AnimationValue>.SetContent(node, element);
            return node;
        }

        public static T TextValue<T>(this T node, string text) where T : IInput
        {
            node.SetProperty(PropertyKeys.Text, text);
            return node;
        }

        public static T Checked<T>(this T node, bool value) where T : IInput
        {
            node.SetProperty(PropertyKeys.IsChecked, value);
            return node;
        }

        public static T AutoFocus<T>(this T node) where T : IElement
        {
            node.SetProperty(PropertyKeys.AutoFocus, true);
            return node;
        }

        public static T BringIntoView<T>(this T node) where T : IElement
        {
            node.SetProperty(PropertyKeys.BringIntoView, true);
            return node;
        }

        private static void EnsureBorderThickness(IVisual node)
        {
            if (!node.TryGetValue("BorderThickness", out _))
                node.Thickness(1);
        }

        public static T Row<T>(this T node, int value) where T : IElement
        {
            node.SetProperty("Grid.Row", value);
            return node;
        }

        public static T Column<T>(this T node, int value) where T : IElement
        {
            node.SetProperty("Grid.Column", value);
            return node;
        }

        public static T RowSpan<T>(this T node, int value) where T : IElement
        {
            node.SetProperty("Grid.RowSpan", value);
            return node;
        }

        public static T ColumnSpan<T>(this T node, int value) where T : IElement
        {
            node.SetProperty("Grid.ColumnSpan", value);
            return node;
        }

        public static T Start<T>(this T node) where T : IElement
        {
            node.SetProperty("HorizontalAlignment", HorizontalAlignmentValue.Start);
            return node;
        }

        public static T HCenter<T>(this T node) where T : IElement
        {
            node.SetProperty("HorizontalAlignment", HorizontalAlignmentValue.Center);
            return node;
        }

        public static T End<T>(this T node) where T : IElement
        {
            node.SetProperty("HorizontalAlignment", HorizontalAlignmentValue.End);
            return node;
        }

        public static T Top<T>(this T node) where T : IElement
        {
            node.SetProperty("VerticalAlignment", VerticalAlignmentValue.Start);
            return node;
        }

        public static T VCenter<T>(this T node) where T : IElement
        {
            node.SetProperty("VerticalAlignment", VerticalAlignmentValue.Center);
            return node;
        }

        public static T Bottom<T>(this T node) where T : IElement
        {
            node.SetProperty("VerticalAlignment", VerticalAlignmentValue.End);
            return node;
        }

        public static T Center<T>(this T node) where T : IElement
        {
            node.HCenter();
            node.VCenter();
            return node;
        }

        public static T TextStart<T>(this T node) where T : IInput
        {
            node.SetProperty("HorizontalContentAlignment", HorizontalAlignmentValue.Start);
            return node;
        }

        public static T TextHCenter<T>(this T node) where T : IInput
        {
            node.SetProperty("HorizontalContentAlignment", HorizontalAlignmentValue.Center);
            return node;
        }

        public static T TextEnd<T>(this T node) where T : IInput
        {
            node.SetProperty("HorizontalContentAlignment", HorizontalAlignmentValue.End);
            return node;
        }

        public static T TextTop<T>(this T node) where T : IInput
        {
            node.SetProperty("VerticalContentAlignment", VerticalAlignmentValue.Start);
            return node;
        }

        public static T TextVCenter<T>(this T node) where T : IInput
        {
            node.SetProperty("VerticalContentAlignment", VerticalAlignmentValue.Center);
            return node;
        }

        public static T TextBottom<T>(this T node) where T : IInput
        {
            node.SetProperty("VerticalContentAlignment", VerticalAlignmentValue.End);
            return node;
        }

        public static T TextCenter<T>(this T node) where T : IInput
        {
            node.TextHCenter();
            node.TextVCenter();
            return node;
        }

        public static T OnClick<T>(this T node, Action handler) where T : IElement
        {
            var eventName = node is IInput ? EventKeys.Click : EventKeys.MouseLeftButtonDown;
            node.AddVirtualEvent(eventName, new VirtualEvent(VirtualEventKind.Click, handler));
            return node;
        }

        public static T OnTextChanged<T>(this T node, Action<string> handler) where T : IInput
        {
            node.AddVirtualEvent(EventKeys.TextChanged, new VirtualEvent(VirtualEventKind.TextChanged, handler));
            return node;
        }

        public static T OnContentChanged<T>(this T node, Action<object> handler) where T : IContent
        {
            node.AddVirtualEvent(EventKeys.ContentChanged, new VirtualEvent(VirtualEventKind.ContentChanged, handler));
            return node;
        }

        public static T OnCheckChanged<T>(this T node, Action<bool> handler) where T : IInput
        {
            node.AddVirtualEvent(EventKeys.Checked, new VirtualEvent(VirtualEventKind.CheckChanged, handler));
            node.AddVirtualEvent(EventKeys.Unchecked, new VirtualEvent(VirtualEventKind.CheckChanged, handler));
            return node;
        }

        public static T OnHover<T>(this T node, Action<bool> handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.MouseEnter, new VirtualEvent(VirtualEventKind.HoverChanged, handler));
            node.AddVirtualEvent(EventKeys.MouseLeave, new VirtualEvent(VirtualEventKind.HoverChanged, handler));
            return node;
        }

        public static T OnMouseDown<T>(this T node, Action handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.MouseLeftButtonDown, new VirtualEvent(VirtualEventKind.MouseDown, handler));
            return node;
        }

        public static T OnMouseUp<T>(this T node, Action handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.MouseLeftButtonUp, new VirtualEvent(VirtualEventKind.MouseUp, handler));
            return node;
        }

        public static T OnKeyDown<T>(this T node, Action<KeyboardKey> handler) where T : IElement
        {
            var virtualEvent = new VirtualEvent(VirtualEventKind.KeyDown, handler);
            node.AddVirtualEvent(EventKeys.PreviewKeyDown, virtualEvent);
            node.AddVirtualEvent(EventKeys.KeyDown, virtualEvent);
            return node;
        }

        public static T OnKeyUp<T>(this T node, Action<KeyboardKey> handler) where T : IElement
        {
            var virtualEvent = new VirtualEvent(VirtualEventKind.KeyUp, handler);
            node.AddVirtualEvent(EventKeys.PreviewKeyUp, virtualEvent);
            node.AddVirtualEvent(EventKeys.KeyUp, virtualEvent);
            return node;
        }

        public static T OnFocus<T>(this T node, Action<bool> handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.GotFocus, new VirtualEvent(VirtualEventKind.FocusChanged, handler));
            node.AddVirtualEvent(EventKeys.LostFocus, new VirtualEvent(VirtualEventKind.FocusChanged, handler));
            return node;
        }

        public static T OnLoaded<T>(this T node, Action handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.Loaded, new VirtualEvent(VirtualEventKind.Loaded, handler));
            return node;
        }

        public static T OnUnloaded<T>(this T node, Action handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.Unloaded, new VirtualEvent(VirtualEventKind.Unloaded, handler));
            return node;
        }

        public static T Transitions<T>(this T node, string property, int milliseconds, EasingValue? easing = null) where T : IElement
        {
            AddTransition(node, property, TimeSpan.FromMilliseconds(milliseconds), easing);

            return node;
        }

        public static T Transition<T>(this T node, int milliseconds, EasingValue? easing = null) where T : IElement
        {
            return node.Transition(TimeSpan.FromMilliseconds(milliseconds), easing);
        }

        public static T Transition<T>(this T node, TimeSpan duration, EasingValue? easing = null) where T : IElement
        {
            foreach (var property in node.Properties)
            {
                if (!DefaultTransitionProperties.Contains(property.Key))
                    continue;

                AddTransition(node, property.Key, property.Value, duration, easing);
            }

            return node;
        }

        private static void AddTransition<T>(T node, string property, TimeSpan duration, EasingValue? easing) where T : IElement
        {
            if (node.Properties.TryGetValue(property, out var value))
                AddTransition(node, property, value, duration, easing);
        }

        private static void AddTransition<T>(T node, string property, object? value, TimeSpan duration, EasingValue? easing) where T : IElement
        {
            node.AddAnimation(property, new AnimationValue(property, value, duration, easing));
        }
    }
}
