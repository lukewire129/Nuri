using System;
using Nuri.UI.Controls;
using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
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

        public static Div Grid(params IElement[] children)
        {
            return new Div(DivTypes.Grid, children);
        }

        public static Div Div(RowHeights rowHeights, params IElement[] children)
        {
            return Grid(children)
                .Columns()
                .Rows(rowHeights.Lengths);
        }

        public static Div Div(ColumnWidths columnWidths, params IElement[] children)
        {
            return Grid(children)
                .Rows()
                .Columns(columnWidths.Lengths);
        }

        public static Div Div(RowHeights rowHeights, ColumnWidths columnWidths, params IElement[] children)
        {
            return Grid(children)
                .Rows(rowHeights.Lengths)
                .Columns(columnWidths.Lengths);
        }

        public static Div Grid(RowHeights rowHeights, params IElement[] children)
        {
            return Grid(children)
                .Columns()
                .Rows(rowHeights.Lengths);
        }

        public static Div Grid(ColumnWidths columnWidths, params IElement[] children)
        {
            return Grid(children)
                .Rows()
                .Columns(columnWidths.Lengths);
        }

        public static Div Grid(RowHeights rowHeights, ColumnWidths columnWidths, params IElement[] children)
        {
            return Grid(children)
                .Rows(rowHeights.Lengths)
                .Columns(columnWidths.Lengths);
        }

        public static ImageElement Image()
        {
            return new ImageElement();
        }

        public static ImageElement Image(string source)
        {
            return new ImageElement(source);
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

        public static Input Input(string kind, object content, Action handler)
        {
            return new Input(kind, content).OnClick(handler);
        }

        public static Input Button()
        {
            return new Input(InputTypes.Button);
        }

        public static Input Button(object content)
        {
            return new Input(InputTypes.Button, content);
        }

        public static Input Button(object content, Action handler)
        {
            return Button(content).OnClick(handler);
        }

        public static Input CheckBox()
        {
            return new Input(InputTypes.Checkbox);
        }

        public static Input CheckBox(object content)
        {
            return new Input(InputTypes.Checkbox, content);
        }

        public static Input CheckBox(Action<bool> handler)
        {
            return CheckBox().OnCheckChanged(handler);
        }

        public static Input CheckBox(object content, Action<bool> handler)
        {
            return CheckBox(content).OnCheckChanged(handler);
        }

        public static Input RadioButton()
        {
            return new Input(InputTypes.Radio);
        }

        public static Input RadioButton(object content)
        {
            return new Input(InputTypes.Radio, content);
        }

        public static Input RadioButton(Action<bool> handler)
        {
            return RadioButton().OnCheckChanged(handler);
        }

        public static Input RadioButton(object content, Action<bool> handler)
        {
            return RadioButton(content).OnCheckChanged(handler);
        }

        public static Input TextBox()
        {
            return new Input(InputTypes.Text);
        }

        public static Input TextBox(string text)
        {
            return TextBox().TextValue(text);
        }

        public static Input TextBox(Action<string> handler)
        {
            return TextBox().OnTextChanged(handler);
        }

        public static Input TextBox(string text, Action<string> handler)
        {
            return TextBox(text).OnTextChanged(handler);
        }

        public static Input PasswordBox()
        {
            return new Input(InputTypes.Password);
        }

        public static Input ToggleButton()
        {
            return new Input(InputTypes.Toggle);
        }

        public static Input ToggleButton(object content)
        {
            return new Input(InputTypes.Toggle, content);
        }

        public static Input ToggleButton(object content, Action<bool> handler)
        {
            return ToggleButton(content).OnCheckChanged(handler);
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

        public static LengthValue Auto => LengthValue.Auto();

        public static LengthValue Star => LengthValue.Star();

        public static LengthValue Stars(double value)
        {
            return LengthValue.Star(value);
        }

        public static RowHeights Rows(params LengthValue[] heights)
        {
            return new RowHeights { Lengths = heights };
        }

        public static RowHeights Rows(string definitions)
        {
            return Rows(GridLengthParser.Parse(definitions, nameof(definitions)));
        }

        public static ColumnWidths Columns(params LengthValue[] widths)
        {
            return new ColumnWidths { Lengths = widths };
        }

        public static ColumnWidths Columns(string definitions)
        {
            return Columns(GridLengthParser.Parse(definitions, nameof(definitions)));
        }

        public static LengthValue Pixels(double value)
        {
            return LengthValue.Pixels(value);
        }
    }

    public struct RowHeights
    {
        internal LengthValue[] Lengths;
    }

    public struct ColumnWidths
    {
        internal LengthValue[] Lengths;
    }
}
