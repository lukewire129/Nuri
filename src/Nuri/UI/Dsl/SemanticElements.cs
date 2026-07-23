using System.Collections.Generic;
using System.Linq;
using Nuri.UI.Controls;
using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
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
            if (Kind == DivTypes.Scroll)
            {
                var addedChildren = children.Count(child => child != null);
                if (Children.Count + addedChildren > 1)
                    throw new System.InvalidOperationException("Scroll Div supports at most one child. Wrap multiple elements in a Column Div.");
            }

            base.AddChildren(children);
        }

        public Div RowDefinition(params LengthValue[] heights)
        {
            var rows = GetLengthList("RowDefinitions");
            foreach (var height in heights)
                rows.Add(height);

            return this;
        }

        public Div ColumnDefinition(params LengthValue[] widths)
        {
            var columns = GetLengthList("ColumnDefinitions");
            foreach (var width in widths)
                columns.Add(width);

            return this;
        }

        public Div Rows(params LengthValue[] heights)
        {
            SetLengthList("RowDefinitions", heights);
            return this;
        }

        public Div Rows(string definitions)
        {
            return Rows(GridLengthParser.Parse(definitions, nameof(definitions)));
        }

        public Div Columns(params LengthValue[] widths)
        {
            SetLengthList("ColumnDefinitions", widths);
            return this;
        }

        public Div Columns(string definitions)
        {
            return Columns(GridLengthParser.Parse(definitions, nameof(definitions)));
        }

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

    public sealed class WindowView : Panel
    {
        public WindowView(params IElement[] children) : base(VirtualControlTypes.Window, children)
        {
        }

        public WindowView WithTitle(string title)
        {
            SetProperty("Title", title);
            return this;
        }

        public WindowView WithSize(double width, double height)
        {
            SetProperty("Width", width);
            SetProperty("Height", height);
            return this;
        }
    }

    public sealed class ImageElement : Visual, IImage
    {
        public ImageElement() : base(VirtualControlTypes.Image, ImageTypes.Default)
        {
        }

        public ImageElement(string source) : this()
        {
            SetProperty("Source", source);
        }
    }

    public sealed class Input : Visual, IInput
    {
        public Input() : this(InputTypes.Text)
        {
        }

        public Input(string kind) : base(VirtualControlTypes.Input, kind)
        {
        }

        public Input(string kind, object content) : this(kind)
        {
            SetProperty("Content", content);
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

    public sealed class Text : Visual, IText
    {
        public Text() : base(VirtualControlTypes.Text)
        {
        }

        public Text(string content) : this()
        {
            SetProperty("Text", content);
        }
    }
}
