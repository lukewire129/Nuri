using System;
using System.Collections.Generic;
using Nuri.Constants;
using Nuri.UI.Controls;
using Nuri.UI.Events;
using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    /// <summary>
    /// Fluent extension methods that configure layout, appearance, and events on Nuri elements.
    /// Every method returns the same element so calls can be chained.
    /// </summary>
    public static class ElementExtensions
    {
        private static readonly HashSet<string> DefaultTransitionProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            PropertyKeys.Background,
            PropertyKeys.Foreground,
            "Margin",
            "Opacity",
            "Rotate",
            "ScaleX",
            "ScaleY",
            "TranslateX",
            "TranslateY"
        };

        /// <summary>
        /// Assigns a stable, sibling-unique key used for reconciliation and hook identity in dynamic lists.
        /// </summary>
        /// <param name="node">The element to key.</param>
        /// <param name="key">The key value.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Key<T>(this T node, string key) where T : IElement
        {
            node.Key = key;
            return node;
        }

        /// <summary>
        /// Applies a named YAML style to the element.
        /// </summary>
        /// <param name="node">The element to style.</param>
        /// <param name="styleName">The style name defined in a loaded style document.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Style<T>(this T node, string styleName) where T : IElement
        {
            if (string.IsNullOrWhiteSpace(styleName))
                throw new ArgumentException("Style name cannot be empty.", nameof(styleName));

            node.StyleName = styleName;
            return node;
        }

        /// <summary>
        /// Assigns a compatibility fallback name. Prefer <see cref="Key{T}(T,string)"/> for new keyed lists.
        /// </summary>
        /// <param name="node">The element to name.</param>
        /// <param name="name">The name value.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Name<T>(this T node, string name) where T : IElement
        {
            node.Name = name;
            node.SetProperty(PropertyKeys.Name, name);
            return node;
        }

        /// <summary>
        /// Sets the explicit width of the element in logical pixels.
        /// </summary>
        /// <param name="node">The element to size.</param>
        /// <param name="value">The width.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Width<T>(this T node, double value) where T : IElement
        {
            node.SetProperty(PropertyKeys.Width, value);
            return node;
        }

        /// <summary>
        /// Sets the explicit height of the element in logical pixels.
        /// </summary>
        /// <param name="node">The element to size.</param>
        /// <param name="value">The height.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Height<T>(this T node, double value) where T : IElement
        {
            node.SetProperty(PropertyKeys.Height, value);
            return node;
        }

        /// <summary>
        /// Sets the minimum width constraint. Must be finite and non-negative.
        /// </summary>
        /// <param name="node">The element to constrain.</param>
        /// <param name="value">The minimum width.</param>
        /// <returns>The same element for chaining.</returns>
        public static T MinWidth<T>(this T node, double value) where T : IElement
        {
            ValidateSizeConstraint(value, nameof(value));
            node.SetProperty(PropertyKeys.MinWidth, value);
            return node;
        }

        /// <summary>
        /// Sets the minimum height constraint. Must be finite and non-negative.
        /// </summary>
        /// <param name="node">The element to constrain.</param>
        /// <param name="value">The minimum height.</param>
        /// <returns>The same element for chaining.</returns>
        public static T MinHeight<T>(this T node, double value) where T : IElement
        {
            ValidateSizeConstraint(value, nameof(value));
            node.SetProperty(PropertyKeys.MinHeight, value);
            return node;
        }

        /// <summary>
        /// Sets the maximum width constraint. Must be finite and non-negative.
        /// </summary>
        /// <param name="node">The element to constrain.</param>
        /// <param name="value">The maximum width.</param>
        /// <returns>The same element for chaining.</returns>
        public static T MaxWidth<T>(this T node, double value) where T : IElement
        {
            ValidateSizeConstraint(value, nameof(value));
            node.SetProperty(PropertyKeys.MaxWidth, value);
            return node;
        }

        /// <summary>
        /// Sets the maximum height constraint. Must be finite and non-negative.
        /// </summary>
        /// <param name="node">The element to constrain.</param>
        /// <param name="value">The maximum height.</param>
        /// <returns>The same element for chaining.</returns>
        public static T MaxHeight<T>(this T node, double value) where T : IElement
        {
            ValidateSizeConstraint(value, nameof(value));
            node.SetProperty(PropertyKeys.MaxHeight, value);
            return node;
        }

        /// <summary>
        /// Sets the element opacity (0 = transparent, 1 = opaque).
        /// </summary>
        /// <param name="node">The element to affect.</param>
        /// <param name="value">The opacity value.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Opacity<T>(this T node, double value) where T : IElement
        {
            node.SetProperty("Opacity", value);
            return node;
        }

        /// <summary>
        /// Sets the rotation transform in degrees around the element center.
        /// </summary>
        /// <param name="node">The element to rotate.</param>
        /// <param name="degrees">Rotation in degrees.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Rotate<T>(this T node, double degrees) where T : IElement
        {
            node.SetProperty("Rotate", degrees);
            return node;
        }

        /// <summary>
        /// Sets both the X and Y translation transforms in logical pixels.
        /// </summary>
        /// <param name="node">The element to translate.</param>
        /// <param name="x">Horizontal translation.</param>
        /// <param name="y">Vertical translation.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Translate<T>(this T node, double x, double y) where T : IElement
        {
            return node.TranslateX(x).TranslateY(y);
        }

        /// <summary>
        /// Sets the X translation transform in logical pixels.
        /// </summary>
        /// <param name="node">The element to translate.</param>
        /// <param name="x">Horizontal translation.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TranslateX<T>(this T node, double x) where T : IElement
        {
            node.SetProperty("TranslateX", x);
            return node;
        }

        /// <summary>
        /// Sets the Y translation transform in logical pixels.
        /// </summary>
        /// <param name="node">The element to translate.</param>
        /// <param name="y">Vertical translation.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TranslateY<T>(this T node, double y) where T : IElement
        {
            node.SetProperty("TranslateY", y);
            return node;
        }

        /// <summary>
        /// Positions the element at the given X and Y coordinates within an <see cref="DivTypes.Absolute"/> layout.
        /// </summary>
        /// <param name="node">The element to position.</param>
        /// <param name="x">The layout X coordinate.</param>
        /// <param name="y">The layout Y coordinate.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Position<T>(this T node, double x, double y) where T : IElement
        {
            return node.PositionX(x).PositionY(y);
        }

        /// <summary>
        /// Sets the X coordinate of the element within an <see cref="DivTypes.Absolute"/> layout.
        /// </summary>
        /// <param name="node">The element to position.</param>
        /// <param name="x">The layout X coordinate.</param>
        /// <returns>The same element for chaining.</returns>
        public static T PositionX<T>(this T node, double x) where T : IElement
        {
            node.SetProperty(PropertyKeys.PositionX, x);
            return node;
        }

        /// <summary>
        /// Sets the Y coordinate of the element within an <see cref="DivTypes.Absolute"/> layout.
        /// </summary>
        /// <param name="node">The element to position.</param>
        /// <param name="y">The layout Y coordinate.</param>
        /// <returns>The same element for chaining.</returns>
        public static T PositionY<T>(this T node, double y) where T : IElement
        {
            node.SetProperty(PropertyKeys.PositionY, y);
            return node;
        }

        /// <summary>
        /// Sets the viewport offset (the content coordinate projected at the viewport origin). Valid only on a <see cref="DivTypes.Viewport"/> layout.
        /// </summary>
        /// <param name="node">The viewport element.</param>
        /// <param name="x">The offset X.</param>
        /// <param name="y">The offset Y.</param>
        /// <returns>The same element for chaining.</returns>
        public static T ViewportOffset<T>(this T node, double x, double y) where T : IDiv
        {
            EnsureViewportLayout(node, PropertyKeys.ViewportOffsetX);
            ValidateFinite(x, nameof(x));
            ValidateFinite(y, nameof(y));
            node.SetProperty(PropertyKeys.ViewportOffsetX, x);
            node.SetProperty(PropertyKeys.ViewportOffsetY, y);
            return node;
        }

        /// <summary>
        /// Sets the viewport offset X. Valid only on a <see cref="DivTypes.Viewport"/> layout.
        /// </summary>
        /// <param name="node">The viewport element.</param>
        /// <param name="x">The offset X.</param>
        /// <returns>The same element for chaining.</returns>
        public static T ViewportOffsetX<T>(this T node, double x) where T : IDiv
        {
            EnsureViewportLayout(node, PropertyKeys.ViewportOffsetX);
            ValidateFinite(x, nameof(x));
            node.SetProperty(PropertyKeys.ViewportOffsetX, x);
            return node;
        }

        /// <summary>
        /// Sets the viewport offset Y. Valid only on a <see cref="DivTypes.Viewport"/> layout.
        /// </summary>
        /// <param name="node">The viewport element.</param>
        /// <param name="y">The offset Y.</param>
        /// <returns>The same element for chaining.</returns>
        public static T ViewportOffsetY<T>(this T node, double y) where T : IDiv
        {
            EnsureViewportLayout(node, PropertyKeys.ViewportOffsetY);
            ValidateFinite(y, nameof(y));
            node.SetProperty(PropertyKeys.ViewportOffsetY, y);
            return node;
        }

        /// <summary>
        /// Sets the viewport zoom (finite scale greater than zero). A content point projects as <c>(content - offset) * zoom</c>.
        /// </summary>
        /// <param name="node">The viewport element.</param>
        /// <param name="zoom">The zoom factor (finite and greater than zero).</param>
        /// <returns>The same element for chaining.</returns>
        public static T ViewportZoom<T>(this T node, double zoom) where T : IDiv
        {
            EnsureViewportLayout(node, PropertyKeys.ViewportZoom);
            if (double.IsNaN(zoom) || double.IsInfinity(zoom) || zoom <= 0)
                throw new ArgumentOutOfRangeException(nameof(zoom), zoom, "Viewport zoom must be finite and greater than zero.");

            node.SetProperty(PropertyKeys.ViewportZoom, zoom);
            return node;
        }

        /// <summary>
        /// Sets a uniform scale transform (equal X and Y).
        /// </summary>
        /// <param name="node">The element to scale.</param>
        /// <param name="value">The scale factor.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Scale<T>(this T node, double value) where T : IElement
        {
            return node.Scale(value, value);
        }

        /// <summary>
        /// Sets the X and Y scale transforms.
        /// </summary>
        /// <param name="node">The element to scale.</param>
        /// <param name="x">The horizontal scale factor.</param>
        /// <param name="y">The vertical scale factor.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Scale<T>(this T node, double x, double y) where T : IElement
        {
            return node.ScaleX(x).ScaleY(y);
        }

        /// <summary>
        /// Sets the X scale transform.
        /// </summary>
        /// <param name="node">The element to scale.</param>
        /// <param name="x">The horizontal scale factor.</param>
        /// <returns>The same element for chaining.</returns>
        public static T ScaleX<T>(this T node, double x) where T : IElement
        {
            node.SetProperty("ScaleX", x);
            return node;
        }

        /// <summary>
        /// Sets the Y scale transform.
        /// </summary>
        /// <param name="node">The element to scale.</param>
        /// <param name="y">The vertical scale factor.</param>
        /// <returns>The same element for chaining.</returns>
        public static T ScaleY<T>(this T node, double y) where T : IElement
        {
            node.SetProperty("ScaleY", y);
            return node;
        }

        /// <summary>
        /// Sets both the width and height of the element in logical pixels.
        /// </summary>
        /// <param name="node">The element to size.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Size<T>(this T node, double width, double height) where T : IElement
        {
            node.Width(width);
            node.Height(height);
            return node;
        }

        /// <summary>
        /// Sets a uniform margin around the element.
        /// </summary>
        /// <param name="node">The element to margin.</param>
        /// <param name="value">The uniform margin in logical pixels.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Margin<T>(this T node, double value) where T : IElement
        {
            node.SetProperty ("Margin", ThicknessValue.Uniform (value));
            return node;
        }

        /// <summary>
        /// Sets the left, top, right, and bottom margin around the element.
        /// </summary>
        /// <param name="node">The element to margin.</param>
        /// <param name="left">Left margin.</param>
        /// <param name="top">Top margin.</param>
        /// <param name="right">Right margin.</param>
        /// <param name="bottom">Bottom margin.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Margin<T>(this T node, double left=0, double top = 0, double right = 0, double bottom =0) where T : IElement
        {
            node.SetProperty("Margin", new ThicknessValue(left, top, right, bottom));
            return node;
        }

        /// <summary>
        /// Sets the solid-color background from a <see cref="ColorValue"/>.
        /// </summary>
        /// <param name="node">The element to paint.</param>
        /// <param name="color">The background color.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Background<T>(this T node, ColorValue color) where T : IElement
        {
            node.SetProperty(PropertyKeys.Background, new BrushValue.Solid(color));
            return node;
        }

        /// <summary>
        /// Sets the solid-color background from a hex color code (for example <c>#FF4081</c>).
        /// </summary>
        /// <param name="node">The element to paint.</param>
        /// <param name="colorCode">The hex color code.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Background<T>(this T node, string colorCode) where T : IElement
        {
            node.SetProperty(PropertyKeys.Background, new BrushValue.Solid(ColorValue.FromHex(colorCode)));
            return node;
        }

        /// <summary>
        /// Sets the background brush.
        /// </summary>
        /// <param name="node">The element to paint.</param>
        /// <param name="brush">The brush value.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Background<T>(this T node, BrushValue brush) where T : IElement
        {
            node.SetProperty(PropertyKeys.Background, brush);
            return node;
        }

        /// <summary>
        /// Sets the solid foreground (text) color from a <see cref="ColorValue"/>.
        /// </summary>
        /// <param name="node">The element to color.</param>
        /// <param name="color">The foreground color.</param>
        /// <returns>The same element for chaining.</returns>
        public static T FontColor<T>(this T node, ColorValue color) where T : IElement
        {
            node.SetProperty(PropertyKeys.Foreground, new BrushValue.Solid(color));
            return node;
        }

        /// <summary>
        /// Sets the solid foreground (text) color from a hex color code.
        /// </summary>
        /// <param name="node">The element to color.</param>
        /// <param name="colorCode">The hex color code.</param>
        /// <returns>The same element for chaining.</returns>
        public static T FontColor<T>(this T node, string colorCode) where T : IElement
        {
            node.SetProperty(PropertyKeys.Foreground, new BrushValue.Solid(ColorValue.FromHex(colorCode)));
            return node;
        }

        /// <summary>
        /// Sets the font size in logical pixels.
        /// </summary>
        /// <param name="node">The element to size.</param>
        /// <param name="size">The font size.</param>
        /// <returns>The same element for chaining.</returns>
        public static T FontSize<T>(this T node, double size) where T : IElement
        {
            node.SetProperty("FontSize", size);
            return node;
        }

        /// <summary>
        /// Sets the font family by source name.
        /// </summary>
        /// <param name="node">The element to configure.</param>
        /// <param name="source">The font family source.</param>
        /// <returns>The same element for chaining.</returns>
        public static T FontFamily<T>(this T node, string source) where T : IElement
        {
            node.SetProperty("FontFamily", new FontFamilyValue(source));
            return node;
        }

        /// <summary>
        /// Sets the font weight.
        /// </summary>
        /// <param name="node">The element to configure.</param>
        /// <param name="weight">The font weight value.</param>
        /// <returns>The same element for chaining.</returns>
        public static T FontWeight<T>(this T node, FontWeightValue weight) where T : IElement
        {
            node.SetProperty("FontWeight", weight);
            return node;
        }

        /// <summary>
        /// Sets how text overflows when it does not fit.
        /// </summary>
        /// <param name="node">The text element.</param>
        /// <param name="value">The text overflow behavior.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextOverflow<T>(this T node, TextOverflowValue value) where T : IText
        {
            node.SetProperty(PropertyKeys.TextOverflow, value);
            return node;
        }

        /// <summary>
        /// Sets the cursor shown when hovering the element.
        /// </summary>
        /// <param name="node">The element to configure.</param>
        /// <param name="cursor">The cursor value.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Cursor<T>(this T node, CursorValue cursor) where T : IElement
        {
            node.SetProperty("Cursor", cursor);
            return node;
        }

        /// <summary>
        /// Sets the bitmap scaling mode used when rendering images.
        /// </summary>
        /// <param name="node">The element to configure.</param>
        /// <param name="value">The image scaling mode.</param>
        /// <returns>The same element for chaining.</returns>
        public static T BitmapScalingMode<T>(this T node, ImageScalingModeValue value) where T : IElement
        {
            node.SetProperty("RenderOptions.BitmapScalingMode", value);
            return node;
        }

        /// <summary>
        /// Sets a uniform padding inside the element.
        /// </summary>
        /// <param name="node">The element to pad.</param>
        /// <param name="value">The uniform padding in logical pixels.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Padding<T>(this T node, double value) where T : IElement
        {
            node.SetProperty("Padding", ThicknessValue.Uniform(value));
            return node;
        }

        /// <summary>
        /// Sets the left, top, right, and bottom padding inside the element.
        /// </summary>
        /// <param name="node">The element to pad.</param>
        /// <param name="left">Left padding.</param>
        /// <param name="top">Top padding.</param>
        /// <param name="right">Right padding.</param>
        /// <param name="bottom">Bottom padding.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Padding<T>(this T node, double left, double top, double right, double bottom) where T : IElement
        {
            node.SetProperty("Padding", new ThicknessValue(left, top, right, bottom));
            return node;
        }

        /// <summary>
        /// Sets the spacing between children. For a grid this sets both row and column spacing; for a row/column layout it sets the linear spacing.
        /// </summary>
        /// <param name="node">The container element.</param>
        /// <param name="value">The spacing in logical pixels (finite and non-negative).</param>
        /// <returns>The same element for chaining.</returns>
        public static T Spacing<T>(this T node, double value) where T : IDiv
        {
            ValidateSpacing(value);

            if (node.Kind == DivTypes.Grid)
            {
                node.SetProperty(PropertyKeys.RowSpacing, value);
                node.SetProperty(PropertyKeys.ColumnSpacing, value);
                return node;
            }

            EnsureSpacingLayout(node);

            node.SetProperty(PropertyKeys.Spacing, value);
            return node;
        }

        /// <summary>
        /// Sets the spacing between grid rows. Valid only on a grid layout.
        /// </summary>
        /// <param name="node">The grid element.</param>
        /// <param name="value">The row spacing (finite and non-negative).</param>
        /// <returns>The same element for chaining.</returns>
        public static T RowSpacing<T>(this T node, double value) where T : IDiv
        {
            EnsureGridLayout(node, PropertyKeys.RowSpacing);
            ValidateSpacing(value);
            node.SetProperty(PropertyKeys.RowSpacing, value);
            return node;
        }

        /// <summary>
        /// Sets the spacing between grid columns. Valid only on a grid layout.
        /// </summary>
        /// <param name="node">The grid element.</param>
        /// <param name="value">The column spacing (finite and non-negative).</param>
        /// <returns>The same element for chaining.</returns>
        public static T ColumnSpacing<T>(this T node, double value) where T : IDiv
        {
            EnsureGridLayout(node, PropertyKeys.ColumnSpacing);
            ValidateSpacing(value);
            node.SetProperty(PropertyKeys.ColumnSpacing, value);
            return node;
        }

        /// <summary>
        /// Sets the grow weight used to distribute free space in a linear (row/column) layout. Must be finite and non-negative.
        /// </summary>
        /// <param name="node">The element to grow.</param>
        /// <param name="weight">The grow weight (defaults to 1).</param>
        /// <returns>The same element for chaining.</returns>
        public static T Grow<T>(this T node, double weight = 1) where T : IElement
        {
            if (double.IsNaN(weight) || double.IsInfinity(weight) || weight < 0)
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "Grow weight must be a finite, non-negative value.");

            node.SetProperty(PropertyKeys.Grow, weight);
            return node;
        }

        /// <summary>
        /// Sets how children are justified along the main axis of a row or column layout.
        /// </summary>
        /// <param name="node">The container element.</param>
        /// <param name="distribution">The content distribution mode.</param>
        /// <returns>The same element for chaining.</returns>
        public static T JustifyContent<T>(this T node, ContentDistribution distribution) where T : IDiv
        {
            EnsureLinearLayout(node, PropertyKeys.JustifyContent);
            node.SetProperty(PropertyKeys.JustifyContent, distribution);
            return node;
        }

        /// <summary>
        /// Justifies children with space between them. Equivalent to <see cref="JustifyContent{T}"/> using <see cref="ContentDistribution.SpaceBetween"/>.
        /// </summary>
        /// <param name="node">The container element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T SpaceBetween<T>(this T node) where T : IDiv
        {
            return node.JustifyContent(ContentDistribution.SpaceBetween);
        }

        /// <summary>
        /// Justifies children with space around them. Equivalent to <see cref="JustifyContent{T}"/> using <see cref="ContentDistribution.SpaceAround"/>.
        /// </summary>
        /// <param name="node">The container element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T SpaceAround<T>(this T node) where T : IDiv
        {
            return node.JustifyContent(ContentDistribution.SpaceAround);
        }

        /// <summary>
        /// Justifies children with even space between and around them. Equivalent to <see cref="JustifyContent{T}"/> using <see cref="ContentDistribution.SpaceEvenly"/>.
        /// </summary>
        /// <param name="node">The container element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T SpaceEvenly<T>(this T node) where T : IDiv
        {
            return node.JustifyContent(ContentDistribution.SpaceEvenly);
        }

        private static void EnsureLinearLayout(IDiv node, string propertyName)
        {
            if (string.IsNullOrEmpty(node.Kind) || node.Kind == DivTypes.Column || node.Kind == DivTypes.Row)
                return;

            throw new InvalidOperationException($"{propertyName} is supported only by Row and Column Div layouts, not '{node.Kind}'.");
        }

        private static void EnsureSpacingLayout(IDiv node)
        {
            if (string.IsNullOrEmpty(node.Kind)
                || node.Kind == DivTypes.Column
                || node.Kind == DivTypes.Row)
                return;

            throw new InvalidOperationException($"{PropertyKeys.Spacing} is supported only by Row and Column Div layouts, not '{node.Kind}'.");
        }

        private static void EnsureGridLayout(IDiv node, string propertyName)
        {
            if (node.Kind == DivTypes.Grid)
                return;

            throw new InvalidOperationException($"{propertyName} is supported only by Grid layouts, not '{node.Kind}'.");
        }

        private static void EnsureViewportLayout(IDiv node, string propertyName)
        {
            if (node.Kind == DivTypes.Viewport)
                return;

            throw new InvalidOperationException($"{propertyName} is supported only by Viewport layouts, not '{node.Kind}'.");
        }

        private static void ValidateSizeConstraint(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(parameterName, value, "Size constraints must be finite and non-negative.");
        }

        private static void ValidateFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, value, "Viewport offset must be finite.");
        }

        private static void ValidateSpacing(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Spacing must be a finite, non-negative value.");
        }

        /// <summary>
        /// Sets a uniform corner radius on the element.
        /// </summary>
        /// <param name="node">The element to round.</param>
        /// <param name="value">The uniform radius in logical pixels.</param>
        /// <returns>The same element for chaining.</returns>
        public static T CornerRadius<T>(this T node, double value) where T : IDiv
        {
            node.SetProperty("CornerRadius", CornerRadiusValue.Uniform(value));
            return node;
        }

        /// <summary>
        /// Sets the per-corner radius (left, top, right, bottom) on the element.
        /// </summary>
        /// <param name="node">The element to round.</param>
        /// <param name="left">Left radius.</param>
        /// <param name="top">Top radius.</param>
        /// <param name="right">Right radius.</param>
        /// <param name="bottom">Bottom radius.</param>
        /// <returns>The same element for chaining.</returns>
        public static T CornerRadius<T>(this T node, double left, double top, double right, double bottom) where T : IDiv
        {
            node.SetProperty("CornerRadius", new CornerRadiusValue(left, top, right, bottom));
            return node;
        }

        /// <summary>
        /// Sets the border brush and ensures a default border thickness so the border is visible.
        /// </summary>
        /// <param name="node">The element to outline.</param>
        /// <param name="brush">The border brush.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Brush<T>(this T node, BrushValue brush) where T : IVisual
        {
            node.SetProperty("BorderBrush", brush);
            EnsureBorderThickness(node);
            return node;
        }

        /// <summary>
        /// Sets the solid-color border brush and ensures a default border thickness.
        /// </summary>
        /// <param name="node">The element to outline.</param>
        /// <param name="color">The border color.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Brush<T>(this T node, ColorValue color) where T : IVisual
        {
            node.SetProperty("BorderBrush", new BrushValue.Solid(color));
            EnsureBorderThickness(node);
            return node;
        }

        /// <summary>
        /// Sets the solid-color border brush from a hex code and ensures a default border thickness.
        /// </summary>
        /// <param name="node">The element to outline.</param>
        /// <param name="colorCode">The hex color code.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Brush<T>(this T node, string colorCode) where T : IVisual
        {
            node.SetProperty("BorderBrush", new BrushValue.Solid(ColorValue.FromHex(colorCode)));
            EnsureBorderThickness(node);
            return node;
        }

        /// <summary>
        /// Sets a uniform border thickness.
        /// </summary>
        /// <param name="node">The element to outline.</param>
        /// <param name="value">The uniform thickness in logical pixels.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Thickness<T>(this T node, double value) where T : IVisual
        {
            node.SetProperty("BorderThickness", ThicknessValue.Uniform(value));
            return node;
        }

        /// <summary>
        /// Sets the left, top, right, and bottom border thickness.
        /// </summary>
        /// <param name="node">The element to outline.</param>
        /// <param name="left">Left thickness.</param>
        /// <param name="top">Top thickness.</param>
        /// <param name="right">Right thickness.</param>
        /// <param name="bottom">Bottom thickness.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Thickness<T>(this T node, double left = 0, double top = 0, double right = 0, double bottom = 0) where T : IVisual
        {
            node.SetProperty("BorderThickness", new ThicknessValue(left, top, right, bottom));
            return node;
        }

        /// <summary>
        /// Groups radio inputs so only one in the group can be selected at a time.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <param name="groupName">The group name.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Group<T>(this T node, string groupName) where T : IInput
        {
            node.SetProperty("GroupName", groupName);
            return node;
        }

        /// <summary>
        /// Sets the content of a content-bearing element to a plain object (for example button text).
        /// </summary>
        /// <param name="node">The content element.</param>
        /// <param name="content">The content value.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Content<T>(this T node, object content) where T : IContent
        {
            node.SetProperty("Content", content);
            return node;
        }

        /// <summary>
        /// Sets the content of a content-bearing element to a nested Nuri element.
        /// </summary>
        /// <param name="node">The content element.</param>
        /// <param name="element">The nested element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Content<T>(this T node, IElement element) where T : IContent
        {
            ElementTree<IElement, AnimationValue>.SetContent(node, element);
            return node;
        }

        /// <summary>
        /// Sets the text value of an input element.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <param name="text">The text value.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextValue<T>(this T node, string text) where T : IInput
        {
            node.SetProperty(PropertyKeys.Text, text);
            return node;
        }

        /// <summary>
        /// Sets the checked state of a checkable input (check box, radio, or toggle).
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <param name="value">The checked state.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Checked<T>(this T node, bool value) where T : IInput
        {
            node.SetProperty(PropertyKeys.IsChecked, value);
            return node;
        }

        /// <summary>
        /// Requests that the element receive focus when it is first mounted.
        /// </summary>
        /// <param name="node">The element to focus.</param>
        /// <returns>The same element for chaining.</returns>
        public static T AutoFocus<T>(this T node) where T : IElement
        {
            node.SetProperty(PropertyKeys.AutoFocus, true);
            return node;
        }

        /// <summary>
        /// Requests that the element be brought into the visible viewport.
        /// </summary>
        /// <param name="node">The element to reveal.</param>
        /// <returns>The same element for chaining.</returns>
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

        /// <summary>
        /// Places the element in the given grid row (zero-based) when used inside a grid layout.
        /// </summary>
        /// <param name="node">The child element.</param>
        /// <param name="value">The grid row index.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Row<T>(this T node, int value) where T : IElement
        {
            node.SetProperty("Grid.Row", value);
            return node;
        }

        /// <summary>
        /// Places the element in the given grid column (zero-based) when used inside a grid layout.
        /// </summary>
        /// <param name="node">The child element.</param>
        /// <param name="value">The grid column index.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Column<T>(this T node, int value) where T : IElement
        {
            node.SetProperty("Grid.Column", value);
            return node;
        }

        /// <summary>
        /// Spans the element across the given number of grid rows.
        /// </summary>
        /// <param name="node">The child element.</param>
        /// <param name="value">The row span count.</param>
        /// <returns>The same element for chaining.</returns>
        public static T RowSpan<T>(this T node, int value) where T : IElement
        {
            node.SetProperty("Grid.RowSpan", value);
            return node;
        }

        /// <summary>
        /// Spans the element across the given number of grid columns.
        /// </summary>
        /// <param name="node">The child element.</param>
        /// <param name="value">The column span count.</param>
        /// <returns>The same element for chaining.</returns>
        public static T ColumnSpan<T>(this T node, int value) where T : IElement
        {
            node.SetProperty("Grid.ColumnSpan", value);
            return node;
        }

        /// <summary>
        /// Aligns the element to the start (left) of its layout slot.
        /// </summary>
        /// <param name="node">The element to align.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Start<T>(this T node) where T : IElement
        {
            node.SetProperty("HorizontalAlignment", HorizontalAlignmentValue.Start);
            return node;
        }

        /// <summary>
        /// Horizontally centers the element in its layout slot.
        /// </summary>
        /// <param name="node">The element to align.</param>
        /// <returns>The same element for chaining.</returns>
        public static T HCenter<T>(this T node) where T : IElement
        {
            node.SetProperty("HorizontalAlignment", HorizontalAlignmentValue.Center);
            return node;
        }

        /// <summary>
        /// Aligns the element to the end (right) of its layout slot.
        /// </summary>
        /// <param name="node">The element to align.</param>
        /// <returns>The same element for chaining.</returns>
        public static T End<T>(this T node) where T : IElement
        {
            node.SetProperty("HorizontalAlignment", HorizontalAlignmentValue.End);
            return node;
        }

        /// <summary>
        /// Aligns the element to the top of its layout slot.
        /// </summary>
        /// <param name="node">The element to align.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Top<T>(this T node) where T : IElement
        {
            node.SetProperty("VerticalAlignment", VerticalAlignmentValue.Start);
            return node;
        }

        /// <summary>
        /// Vertically centers the element in its layout slot.
        /// </summary>
        /// <param name="node">The element to align.</param>
        /// <returns>The same element for chaining.</returns>
        public static T VCenter<T>(this T node) where T : IElement
        {
            node.SetProperty("VerticalAlignment", VerticalAlignmentValue.Center);
            return node;
        }

        /// <summary>
        /// Aligns the element to the bottom of its layout slot.
        /// </summary>
        /// <param name="node">The element to align.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Bottom<T>(this T node) where T : IElement
        {
            node.SetProperty("VerticalAlignment", VerticalAlignmentValue.End);
            return node;
        }

        /// <summary>
        /// Centers the element both horizontally and vertically in its layout slot.
        /// </summary>
        /// <param name="node">The element to align.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Center<T>(this T node) where T : IElement
        {
            node.HCenter();
            node.VCenter();
            return node;
        }

        /// <summary>
        /// Horizontally aligns the content of an input element to the start.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextStart<T>(this T node) where T : IInput
        {
            node.SetProperty("HorizontalContentAlignment", HorizontalAlignmentValue.Start);
            return node;
        }

        /// <summary>
        /// Horizontally centers the content of an input element.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextHCenter<T>(this T node) where T : IInput
        {
            node.SetProperty("HorizontalContentAlignment", HorizontalAlignmentValue.Center);
            return node;
        }

        /// <summary>
        /// Horizontally aligns the content of an input element to the end.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextEnd<T>(this T node) where T : IInput
        {
            node.SetProperty("HorizontalContentAlignment", HorizontalAlignmentValue.End);
            return node;
        }

        /// <summary>
        /// Aligns the content of an input element to the top.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextTop<T>(this T node) where T : IInput
        {
            node.SetProperty("VerticalContentAlignment", VerticalAlignmentValue.Start);
            return node;
        }

        /// <summary>
        /// Vertically centers the content of an input element.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextVCenter<T>(this T node) where T : IInput
        {
            node.SetProperty("VerticalContentAlignment", VerticalAlignmentValue.Center);
            return node;
        }

        /// <summary>
        /// Aligns the content of an input element to the bottom.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextBottom<T>(this T node) where T : IInput
        {
            node.SetProperty("VerticalContentAlignment", VerticalAlignmentValue.End);
            return node;
        }

        /// <summary>
        /// Centers the content of an input element both horizontally and vertically.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <returns>The same element for chaining.</returns>
        public static T TextCenter<T>(this T node) where T : IInput
        {
            node.TextHCenter();
            node.TextVCenter();
            return node;
        }

        /// <summary>
        /// Attaches a click handler. For inputs this is the click event; for other elements it is the left mouse button down event.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked on click.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnClick<T>(this T node, Action handler) where T : IElement
        {
            var eventName = node is IInput ? EventKeys.Click : EventKeys.MouseLeftButtonDown;
            node.AddVirtualEvent(eventName, new VirtualEvent(VirtualEventKind.Click, handler));
            return node;
        }

        /// <summary>
        /// Attaches a handler invoked when the input text changes.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <param name="handler">The handler invoked with the new text.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnTextChanged<T>(this T node, Action<string> handler) where T : IInput
        {
            node.AddVirtualEvent(EventKeys.TextChanged, new VirtualEvent(VirtualEventKind.TextChanged, handler));
            return node;
        }

        /// <summary>
        /// Attaches a handler invoked when the element content changes.
        /// </summary>
        /// <param name="node">The content element.</param>
        /// <param name="handler">The handler invoked with the new content.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnContentChanged<T>(this T node, Action<object> handler) where T : IContent
        {
            node.AddVirtualEvent(EventKeys.ContentChanged, new VirtualEvent(VirtualEventKind.ContentChanged, handler));
            return node;
        }

        /// <summary>
        /// Attaches a handler invoked when the checked state of a checkable input changes.
        /// </summary>
        /// <param name="node">The input element.</param>
        /// <param name="handler">The handler invoked with the new checked state.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnCheckChanged<T>(this T node, Action<bool> handler) where T : IInput
        {
            node.AddVirtualEvent(EventKeys.Checked, new VirtualEvent(VirtualEventKind.CheckChanged, handler));
            node.AddVirtualEvent(EventKeys.Unchecked, new VirtualEvent(VirtualEventKind.CheckChanged, handler));
            return node;
        }

        /// <summary>
        /// Attaches a handler invoked when the pointer enters (true) or leaves (false) the element.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the hovered state.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnHover<T>(this T node, Action<bool> handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.MouseEnter, new VirtualEvent(VirtualEventKind.HoverChanged, handler));
            node.AddVirtualEvent(EventKeys.MouseLeave, new VirtualEvent(VirtualEventKind.HoverChanged, handler));
            return node;
        }

        /// <summary>
        /// Attaches a left mouse button down handler with optional event routing.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked on mouse down.</param>
        /// <param name="routing">The event routing mode (bubble by default).</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnMouseDown<T>(
            this T node,
            Action handler,
            EventRouting routing = EventRouting.Bubble) where T : IElement
        {
            node.AddVirtualEvent(
                EventKeys.MouseLeftButtonDown,
                new VirtualEvent(VirtualEventKind.PointerDown, handler, routing: routing));
            return node;
        }

        /// <summary>
        /// Attaches a left mouse button up handler with optional event routing.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked on mouse up.</param>
        /// <param name="routing">The event routing mode (bubble by default).</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnMouseUp<T>(
            this T node,
            Action handler,
            EventRouting routing = EventRouting.Bubble) where T : IElement
        {
            node.AddVirtualEvent(
                EventKeys.MouseLeftButtonUp,
                new VirtualEvent(VirtualEventKind.PointerUp, handler, routing: routing));
            return node;
        }

        /// <summary>
        /// Attaches a primary pointer down handler with optional routing and pointer capture.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the pointer event.</param>
        /// <param name="routing">The event routing mode (bubble by default).</param>
        /// <param name="capturePointer">Whether to capture the pointer on down.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnPointerDown<T>(
            this T node,
            Action<PointerEvent> handler,
            EventRouting routing = EventRouting.Bubble,
            bool capturePointer = false) where T : IElement
        {
            return node.OnPointerDown(handler, PointerButton.Primary, routing, capturePointer);
        }

        /// <summary>
        /// Attaches a pointer down handler for a specific button with optional routing and pointer capture.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the pointer event.</param>
        /// <param name="button">The pointer button to listen for.</param>
        /// <param name="routing">The event routing mode (bubble by default).</param>
        /// <param name="capturePointer">Whether to capture the pointer on down.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnPointerDown<T>(
            this T node,
            Action<PointerEvent> handler,
            PointerButton button,
            EventRouting routing = EventRouting.Bubble,
            bool capturePointer = false) where T : IElement
        {
            node.AddVirtualEvent(
                GetPointerButtonEventKey(button, isDown: true),
                new VirtualEvent(VirtualEventKind.PointerDown, handler, capturePointer, routing, button));
            return node;
        }

        /// <summary>
        /// Attaches a pointer move handler with optional event routing.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the pointer event.</param>
        /// <param name="routing">The event routing mode (bubble by default).</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnPointerMove<T>(
            this T node,
            Action<PointerEvent> handler,
            EventRouting routing = EventRouting.Bubble) where T : IElement
        {
            node.AddVirtualEvent(
                EventKeys.MouseMove,
                new VirtualEvent(VirtualEventKind.PointerMove, handler, routing: routing));
            return node;
        }

        /// <summary>
        /// Attaches a primary pointer up handler with optional routing and pointer capture release.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the pointer event.</param>
        /// <param name="routing">The event routing mode (bubble by default).</param>
        /// <param name="releasePointerCapture">Whether to release a captured pointer on up.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnPointerUp<T>(
            this T node,
            Action<PointerEvent> handler,
            EventRouting routing = EventRouting.Bubble,
            bool releasePointerCapture = false) where T : IElement
        {
            return node.OnPointerUp(handler, PointerButton.Primary, routing, releasePointerCapture);
        }

        /// <summary>
        /// Attaches a pointer up handler for a specific button with optional routing and pointer capture release.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the pointer event.</param>
        /// <param name="button">The pointer button to listen for.</param>
        /// <param name="routing">The event routing mode (bubble by default).</param>
        /// <param name="releasePointerCapture">Whether to release a captured pointer on up.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnPointerUp<T>(
            this T node,
            Action<PointerEvent> handler,
            PointerButton button,
            EventRouting routing = EventRouting.Bubble,
            bool releasePointerCapture = false) where T : IElement
        {
            node.AddVirtualEvent(
                GetPointerButtonEventKey(button, isDown: false),
                new VirtualEvent(VirtualEventKind.PointerUp, handler, releasePointerCapture, routing, button));
            return node;
        }

        /// <summary>
        /// Attaches a pointer wheel handler with optional event routing.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the pointer wheel event.</param>
        /// <param name="routing">The event routing mode (bubble by default).</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnPointerWheel<T>(
            this T node,
            Action<PointerWheelEvent> handler,
            EventRouting routing = EventRouting.Bubble) where T : IElement
        {
            node.AddVirtualEvent(
                EventKeys.MouseWheel,
                new VirtualEvent(VirtualEventKind.PointerWheel, handler, routing: routing));
            return node;
        }

        private static string GetPointerButtonEventKey(PointerButton button, bool isDown)
        {
            return button switch
            {
                PointerButton.Primary => isDown ? EventKeys.MouseLeftButtonDown : EventKeys.MouseLeftButtonUp,
                PointerButton.Secondary => isDown ? EventKeys.MouseRightButtonDown : EventKeys.MouseRightButtonUp,
                _ => throw new NotSupportedException("Unsupported pointer button.")
            };
        }

        /// <summary>
        /// Attaches a key down handler.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the key.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnKeyDown<T>(this T node, Action<KeyboardKey> handler) where T : IElement
        {
            var virtualEvent = new VirtualEvent(VirtualEventKind.KeyDown, handler);
            node.AddVirtualEvent(EventKeys.PreviewKeyDown, virtualEvent);
            node.AddVirtualEvent(EventKeys.KeyDown, virtualEvent);
            return node;
        }

        /// <summary>
        /// Attaches a key up handler.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the key.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnKeyUp<T>(this T node, Action<KeyboardKey> handler) where T : IElement
        {
            var virtualEvent = new VirtualEvent(VirtualEventKind.KeyUp, handler);
            node.AddVirtualEvent(EventKeys.PreviewKeyUp, virtualEvent);
            node.AddVirtualEvent(EventKeys.KeyUp, virtualEvent);
            return node;
        }

        /// <summary>
        /// Attaches a focus-changed handler invoked when the element gains (true) or loses (false) focus.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked with the focused state.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnFocus<T>(this T node, Action<bool> handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.GotFocus, new VirtualEvent(VirtualEventKind.FocusChanged, handler));
            node.AddVirtualEvent(EventKeys.LostFocus, new VirtualEvent(VirtualEventKind.FocusChanged, handler));
            return node;
        }

        /// <summary>
        /// Attaches a handler invoked when the element is loaded into the visual tree.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked on load.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnLoaded<T>(this T node, Action handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.Loaded, new VirtualEvent(VirtualEventKind.Loaded, handler));
            return node;
        }

        /// <summary>
        /// Attaches a handler invoked when the element is removed from the visual tree.
        /// </summary>
        /// <param name="node">The element to wire.</param>
        /// <param name="handler">The handler invoked on unload.</param>
        /// <returns>The same element for chaining.</returns>
        public static T OnUnloaded<T>(this T node, Action handler) where T : IElement
        {
            node.AddVirtualEvent(EventKeys.Unloaded, new VirtualEvent(VirtualEventKind.Unloaded, handler));
            return node;
        }

        /// <summary>
        /// Adds a transition of the given property over the specified duration with optional easing.
        /// </summary>
        /// <param name="node">The element to animate.</param>
        /// <param name="property">The property name to animate.</param>
        /// <param name="milliseconds">The transition duration in milliseconds.</param>
        /// <param name="easing">Optional easing mode.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Transitions<T>(this T node, string property, int milliseconds, EasingValue? easing = null) where T : IElement
        {
            AddTransition(node, property, TimeSpan.FromMilliseconds(milliseconds), easing);

            return node;
        }

        /// <summary>
        /// Adds transitions for all default animatable properties over the given duration in milliseconds.
        /// </summary>
        /// <param name="node">The element to animate.</param>
        /// <param name="milliseconds">The transition duration in milliseconds.</param>
        /// <param name="easing">Optional easing mode.</param>
        /// <returns>The same element for chaining.</returns>
        public static T Transition<T>(this T node, int milliseconds, EasingValue? easing = null) where T : IElement
        {
            return node.Transition(TimeSpan.FromMilliseconds(milliseconds), easing);
        }

        /// <summary>
        /// Adds transitions for all default animatable properties over the given duration.
        /// </summary>
        /// <param name="node">The element to animate.</param>
        /// <param name="duration">The transition duration.</param>
        /// <param name="easing">Optional easing mode.</param>
        /// <returns>The same element for chaining.</returns>
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
