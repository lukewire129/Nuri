using System.Collections.Generic;
using Nuri.UI.Controls;
using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    public sealed class Div : Panel, IDiv
    {
        public Div() : this(DivTypes.Column)
        {
        }

        public Div(string kind, params IElement[] children) : base(VirtualControlTypes.Div, children)
        {
            Kind = kind;
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

        public Div Columns(params LengthValue[] widths)
        {
            SetLengthList("ColumnDefinitions", widths);
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

    public sealed class Text : Visual
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
