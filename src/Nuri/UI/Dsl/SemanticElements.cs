using System.Collections.Generic;
using System.Linq;
using Nuri.UI.Controls;
using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    /// <summary>
    /// A layout container that arranges children according to its layout <c>Kind</c> (column, row, grid, scroll, absolute, or viewport).
    /// </summary>
    public sealed class Div : Panel, IDiv
    {
        private bool _autoFlowApplied;

        public Div() : this(DivTypes.Column)
        {
        }

        public Div(string kind, params IElement[] children) : base(VirtualControlTypes.Div)
        {
            Kind = kind;
            AddChildren(children);
        }

        public override void AddChildren(IElement[] children)
        {
            if (Kind == DivTypes.Scroll || Kind == DivTypes.Viewport)
            {
                var addedChildren = children.Count(child => child != null);
                if (Children.Count + addedChildren > 1)
                    throw new System.InvalidOperationException($"{Kind} Div supports at most one child. Wrap multiple elements in a single content Div.");
            }

            base.AddChildren(children);
        }

        /// <summary>
        /// Appends additional row definitions to this grid container.
        /// </summary>
        /// <param name="heights">Row length values to append.</param>
        /// <returns>The same <see cref="Div"/> for chaining.</returns>
        public Div RowDefinition(params LengthValue[] heights)
        {
            var rows = GetLengthList("RowDefinitions");
            foreach (var height in heights)
                rows.Add(height);

            return this;
        }

        /// <summary>
        /// Appends additional column definitions to this grid container.
        /// </summary>
        /// <param name="widths">Column length values to append.</param>
        /// <returns>The same <see cref="Div"/> for chaining.</returns>
        public Div ColumnDefinition(params LengthValue[] widths)
        {
            var columns = GetLengthList("ColumnDefinitions");
            foreach (var width in widths)
                columns.Add(width);

            return this;
        }

        /// <summary>
        /// Replaces the row definitions of this grid container.
        /// </summary>
        /// <param name="heights">Row length values.</param>
        /// <returns>The same <see cref="Div"/> for chaining.</returns>
        public Div Rows(params LengthValue[] heights)
        {
            SetLengthList("RowDefinitions", heights);
            return this;
        }

        /// <summary>
        /// Replaces the row definitions by parsing a grid-length definition string (for example <c>"Auto, *, 100"</c>).
        /// </summary>
        /// <param name="definitions">A comma-separated grid-length definition string.</param>
        /// <returns>The same <see cref="Div"/> for chaining.</returns>
        public Div Rows(string definitions)
        {
            return Rows(GridLengthParser.Parse(definitions, nameof(definitions)));
        }

        /// <summary>
        /// Replaces the column definitions of this grid container.
        /// </summary>
        /// <param name="widths">Column length values.</param>
        /// <returns>The same <see cref="Div"/> for chaining.</returns>
        public Div Columns(params LengthValue[] widths)
        {
            SetLengthList("ColumnDefinitions", widths);
            return this;
        }

        /// <summary>
        /// Replaces the column definitions by parsing a grid-length definition string (for example <c>"Auto, *, 100"</c>).
        /// </summary>
        /// <param name="definitions">A comma-separated grid-length definition string.</param>
        /// <returns>The same <see cref="Div"/> for chaining.</returns>
        public Div Columns(string definitions)
        {
            return Columns(GridLengthParser.Parse(definitions, nameof(definitions)));
        }

        /// <summary>
        /// Automatically places children into the defined grid columns and rows, filling row by row. Throws if explicit placement is already set or if the grid lacks enough rows.
        /// </summary>
        /// <returns>The same <see cref="Div"/> for chaining.</returns>
        public Div AutoFlow()
        {
            if (Kind != DivTypes.Grid)
                throw new System.InvalidOperationException($"AutoFlow is supported only by Grid layouts, not '{Kind}'.");

            var columns = GetRequiredLengthList("ColumnDefinitions", "AutoFlow requires at least one explicit Grid column.");
            if (!_autoFlowApplied)
                EnsureChildrenDoNotHaveExplicitPlacement();

            var requiredRowCount = Children.Count == 0 ? 0 : (Children.Count + columns.Count - 1) / columns.Count;
            var rows = GetLengthList("RowDefinitions");
            if (rows.Count > 0 && rows.Count < requiredRowCount)
                throw new System.InvalidOperationException($"AutoFlow requires {requiredRowCount} rows for {Children.Count} children and {columns.Count} columns, but the Grid defines only {rows.Count} rows.");

            if (rows.Count == 0)
            {
                while (rows.Count < requiredRowCount)
                    rows.Add(LengthValue.Auto());
            }

            for (var index = 0; index < Children.Count; index++)
            {
                Children[index].SetProperty("Grid.Row", index / columns.Count);
                Children[index].SetProperty("Grid.Column", index % columns.Count);
            }

            _autoFlowApplied = true;
            return this;
        }

        private List<LengthValue> GetLengthList(string propertyName)
        {
            if (!Properties.TryGetValue(propertyName, out var value) || value is not List<LengthValue> lengths)
            {
                lengths = new List<LengthValue>();
                Properties[propertyName] = lengths;
            }

            return lengths;
        }

        private List<LengthValue> GetRequiredLengthList(string propertyName, string message)
        {
            if (!Properties.TryGetValue(propertyName, out var value)
                || value is not List<LengthValue> lengths
                || lengths.Count == 0)
                throw new System.InvalidOperationException(message);

            return lengths;
        }

        private void EnsureChildrenDoNotHaveExplicitPlacement()
        {
            foreach (var child in Children)
            {
                if (child.Properties.ContainsKey("Grid.Row")
                    || child.Properties.ContainsKey("Grid.Column")
                    || child.Properties.ContainsKey("Grid.RowSpan")
                    || child.Properties.ContainsKey("Grid.ColumnSpan"))
                    throw new System.InvalidOperationException("AutoFlow cannot be combined with explicit Grid Row, Column, RowSpan, or ColumnSpan placement.");
            }
        }

        private void SetLengthList(string propertyName, LengthValue[] values)
        {
            var lengths = new List<LengthValue>();
            foreach (var value in values)
                lengths.Add(value);

            Properties[propertyName] = lengths;
        }
    }

    /// <summary>
    /// A top-level window container that hosts Nuri content in a renderer window.
    /// </summary>
    public sealed class WindowView : Panel
    {
        public WindowView(params IElement[] children) : base(VirtualControlTypes.Window, children)
        {
        }

        /// <summary>
        /// Sets the window title.
        /// </summary>
        /// <param name="title">The title text.</param>
        /// <returns>The same <see cref="WindowView"/> for chaining.</returns>
        public WindowView WithTitle(string title)
        {
            SetProperty("Title", title);
            return this;
        }

        /// <summary>
        /// Sets the window size in logical pixels.
        /// </summary>
        /// <param name="width">The window width.</param>
        /// <param name="height">The window height.</param>
        /// <returns>The same <see cref="WindowView"/> for chaining.</returns>
        public WindowView WithSize(double width, double height)
        {
            SetProperty("Width", width);
            SetProperty("Height", height);
            return this;
        }
    }

    /// <summary>
    /// Displays an image from a source path or URI.
    /// </summary>
    public sealed class ImageElement : Visual, IImage
    {
        public ImageElement() : base(VirtualControlTypes.Image, ImageTypes.Default)
        {
        }

        /// <summary>
        /// Creates an image element with the given source.
        /// </summary>
        /// <param name="source">Image source path or URI.</param>
        public ImageElement(string source) : this()
        {
            SetProperty("Source", source);
        }
    }

    /// <summary>
    /// An interactive input control whose behavior depends on its input kind (text, button, check box, radio, password, or toggle).
    /// </summary>
    public sealed class Input : Visual, IInput
    {
        public Input() : this(InputTypes.Text)
        {
        }

        /// <summary>
        /// Creates an input element of the specified input kind.
        /// </summary>
        /// <param name="kind">The input kind.</param>
        public Input(string kind) : base(VirtualControlTypes.Input, kind)
        {
        }

        /// <summary>
        /// Creates an input element of the specified input kind with initial content.
        /// </summary>
        /// <param name="kind">The input kind.</param>
        /// <param name="content">Initial content.</param>
        public Input(string kind, object content) : this(kind)
        {
            SetProperty("Content", content);
        }
    }

    /// <summary>
    /// A container that presents a collection of items using the specified items kind.
    /// </summary>
    public sealed class ItemsView : Panel, IItems
    {
        public ItemsView(string kind, params IElement[] children) : base(VirtualControlTypes.Items, children)
        {
            Kind = kind;
        }
    }

    /// <summary>
    /// A container that presents overlay content (for example a popover) above other elements.
    /// </summary>
    public sealed class OverlayView : Panel, IOverlay
    {
        public OverlayView(string kind, params IElement[] children) : base(VirtualControlTypes.Overlay, children)
        {
            Kind = kind;
        }
    }

    /// <summary>
    /// A container that presents a selectable control (for example a dropdown) of the specified select kind.
    /// </summary>
    public sealed class SelectView : Panel, ISelect
    {
        public SelectView(string kind, params IElement[] children) : base(VirtualControlTypes.Select, children)
        {
            Kind = kind;
        }
    }

    /// <summary>
    /// Displays a run of text.
    /// </summary>
    public sealed class Text : Visual, IText
    {
        public Text() : base(VirtualControlTypes.Text)
        {
        }

        /// <summary>
        /// Creates a text element with the given content.
        /// </summary>
        /// <param name="content">The text to display.</param>
        public Text(string content) : this()
        {
            SetProperty("Text", content);
        }
    }
}
