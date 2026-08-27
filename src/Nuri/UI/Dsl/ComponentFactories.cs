using System;
using System.Collections.Generic;
using Nuri.Constants;
using Nuri.UI.Controls;
using Nuri.UI.Values;
using Nuri.UI.Virtualization;

namespace Nuri.UI.Dsl;

public abstract partial class Component
{
    /// <summary>
    /// Creates a <see cref="Div"/> container using the default vertical <see cref="DivTypes.Column"/> layout.
    /// </summary>
    /// <param name="children">Child elements to add to the container.</param>
    /// <returns>A new <see cref="Div"/> laid out as a column.</returns>
    public static Div Div(params IElement[] children)
    {
        return new Div(DivTypes.Column, children);
    }

    /// <summary>
    /// Creates a <see cref="Div"/> container using the specified layout kind (for example <see cref="DivTypes.Row"/>, <see cref="DivTypes.Grid"/>, <see cref="DivTypes.Scroll"/>, <see cref="DivTypes.Absolute"/>, or <see cref="DivTypes.Viewport"/>).
    /// </summary>
    /// <param name="kind">The layout kind for the container.</param>
    /// <param name="children">Child elements to add to the container.</param>
    /// <returns>A new <see cref="Div"/> using the requested layout.</returns>
    public static Div Div(string kind, params IElement[] children)
    {
        return new Div(kind, children);
    }

    /// <summary>
    /// Creates a <see cref="Div"/> using a <see cref="DivTypes.Grid"/> layout with no explicit row or column definitions.
    /// </summary>
    /// <param name="children">Child elements to add to the grid.</param>
    /// <returns>A new grid <see cref="Div"/>.</returns>
    public static Div Grid(params IElement[] children)
    {
        return new Div(DivTypes.Grid, children);
    }

    /// <summary>
    /// Creates a grid <see cref="Div"/> with the given row heights, filling columns automatically from the children.
    /// </summary>
    /// <param name="rowHeights">Row height definitions for the grid.</param>
    /// <param name="children">Child elements to add to the grid.</param>
    /// <returns>A new grid <see cref="Div"/> with the supplied rows.</returns>
    public static Div Div(RowHeights rowHeights, params IElement[] children)
    {
        return Grid(children)
            .Columns()
            .Rows(rowHeights.Lengths);
    }

    /// <summary>
    /// Creates a grid <see cref="Div"/> with the given column widths, filling rows automatically from the children.
    /// </summary>
    /// <param name="columnWidths">Column width definitions for the grid.</param>
    /// <param name="children">Child elements to add to the grid.</param>
    /// <returns>A new grid <see cref="Div"/> with the supplied columns.</returns>
    public static Div Div(ColumnWidths columnWidths, params IElement[] children)
    {
        return Grid(children)
            .Rows()
            .Columns(columnWidths.Lengths);
    }

    /// <summary>
    /// Creates a grid <see cref="Div"/> with the given row heights and column widths.
    /// </summary>
    /// <param name="rowHeights">Row height definitions for the grid.</param>
    /// <param name="columnWidths">Column width definitions for the grid.</param>
    /// <param name="children">Child elements to add to the grid.</param>
    /// <returns>A new grid <see cref="Div"/> with the supplied rows and columns.</returns>
    public static Div Div(RowHeights rowHeights, ColumnWidths columnWidths, params IElement[] children)
    {
        return Grid(children)
            .Rows(rowHeights.Lengths)
            .Columns(columnWidths.Lengths);
    }

    /// <summary>
    /// Creates a <see cref="Div"/> using a horizontal <see cref="DivTypes.Row"/> layout.
    /// </summary>
    /// <param name="children">Child elements to arrange horizontally.</param>
    /// <returns>A new row <see cref="Div"/>.</returns>
    public static Div Row(IElement[] children)
    {
        return Div (DivTypes.Row, children);
    }

    /// <summary>
    /// Creates a <see cref="Div"/> using a vertical <see cref="DivTypes.Column"/> layout.
    /// </summary>
    /// <param name="children">Child elements to arrange vertically.</param>
    /// <returns>A new column <see cref="Div"/>.</returns>
    public static Div Column(IElement[] children)
    {
        return Div (DivTypes.Column, children);
    }

        /// <summary>
        /// Creates a <see cref="Div"/> using an <see cref="DivTypes.Absolute"/> layout, where children are positioned with the <c>Position</c>, <c>PositionX</c>, or <c>PositionY</c> fluent methods in the layout coordinate space.
        /// </summary>
    /// <param name="children">Child elements to position absolutely.</param>
    /// <returns>A new absolute <see cref="Div"/>.</returns>
    public static Div Absolute(params IElement[] children)
    {
        return new Div(DivTypes.Absolute, children);
    }

    /// <summary>
    /// Creates a single-content <see cref="DivTypes.Viewport"/>, a 2D camera over <paramref name="content"/> that supports <c>ViewportOffset</c> and <c>ViewportZoom</c>.
    /// </summary>
    /// <param name="content">The single content element viewed through the viewport.</param>
    /// <returns>A new viewport <see cref="Div"/>.</returns>
    public static Div Viewport(IElement content)
    {
        return new Div(DivTypes.Viewport, content);
    }

    /// <summary>
    /// Creates a grid <see cref="Div"/> with the given row heights, filling columns automatically from the children.
    /// </summary>
    /// <param name="rowHeights">Row height definitions for the grid.</param>
    /// <param name="children">Child elements to add to the grid.</param>
    /// <returns>A new grid <see cref="Div"/> with the supplied rows.</returns>
    public static Div Grid(RowHeights rowHeights, params IElement[] children)
    {
        return Grid(children)
            .Columns()
            .Rows(rowHeights.Lengths);
    }

    /// <summary>
    /// Creates a grid <see cref="Div"/> with the given column widths, filling rows automatically from the children.
    /// </summary>
    /// <param name="columnWidths">Column width definitions for the grid.</param>
    /// <param name="children">Child elements to add to the grid.</param>
    /// <returns>A new grid <see cref="Div"/> with the supplied columns.</returns>
    public static Div Grid(ColumnWidths columnWidths, params IElement[] children)
    {
        return Grid(children)
            .Rows()
            .Columns(columnWidths.Lengths);
    }

    /// <summary>
    /// Creates a grid <see cref="Div"/> with the given row heights and column widths.
    /// </summary>
    /// <param name="rowHeights">Row height definitions for the grid.</param>
    /// <param name="columnWidths">Column width definitions for the grid.</param>
    /// <param name="children">Child elements to add to the grid.</param>
    /// <returns>A new grid <see cref="Div"/> with the supplied rows and columns.</returns>
    public static Div Grid(RowHeights rowHeights, ColumnWidths columnWidths, params IElement[] children)
    {
        return Grid(children)
            .Rows(rowHeights.Lengths)
            .Columns(columnWidths.Lengths);
    }

    /// <summary>
    /// Creates a renderer-owned native control island for an existing native control type. The <paramref name="mount"/> callback runs once to configure the retained native instance.
    /// </summary>
    /// <typeparam name="TNative">The native control type accepted by the renderer (for example <c>FrameworkElement</c> for WPF).</typeparam>
    /// <param name="mount">Callback that configures the native instance; it may return an unmount cleanup delegate.</param>
    /// <returns>A <see cref="NativeElement"/> describing the native island.</returns>
    public static NativeElement Native<TNative>(Action<TNative> mount)
        where TNative : class, new()
    {
        return CreateNative(() => new TNative(), mount, null, null);
    }

    /// <summary>
    /// Creates a renderer-owned native control island that re-projects Nuri state into the native control after each mount and committed render.
    /// </summary>
    /// <typeparam name="TNative">The native control type accepted by the renderer.</typeparam>
    /// <param name="mount">Callback that configures the native instance once.</param>
    /// <param name="render">Callback that projects Nuri state into the native control after mount and each render.</param>
    /// <returns>A <see cref="NativeElement"/> describing the native island.</returns>
    public static NativeElement Native<TNative>(
        Action<TNative> mount,
        Action<TNative> render)
        where TNative : class, new()
    {
        return CreateNative(() => new TNative(), mount, null, render);
    }

    /// <summary>
    /// Creates a renderer-owned native control island where <paramref name="mount"/> may return an unmount cleanup and <paramref name="render"/> projects Nuri state.
    /// </summary>
    /// <typeparam name="TNative">The native control type accepted by the renderer.</typeparam>
    /// <param name="mount">Callback that configures the native instance and may return an unmount cleanup delegate.</param>
    /// <param name="render">Callback that projects Nuri state into the native control after mount and each render.</param>
    /// <returns>A <see cref="NativeElement"/> describing the native island.</returns>
    public static NativeElement Native<TNative>(
        Func<TNative, Action?> mount,
        Action<TNative> render)
        where TNative : class, new()
    {
        return CreateNative(() => new TNative(), null, mount, render);
    }

    /// <summary>
    /// Creates a renderer-owned native control island using a custom factory for the native instance.
    /// </summary>
    /// <typeparam name="TNative">The native control type accepted by the renderer.</typeparam>
    /// <param name="create">Factory that creates the native instance.</param>
    /// <param name="mount">Callback that configures the native instance once.</param>
    /// <param name="render">Optional callback that projects Nuri state into the native control after mount and each render.</param>
    /// <returns>A <see cref="NativeElement"/> describing the native island.</returns>
    public static NativeElement Native<TNative>(
        Func<TNative> create,
        Action<TNative> mount,
        Action<TNative>? render = null)
        where TNative : class
    {
        return CreateNative(create, mount, null, render);
    }

    /// <summary>
    /// Creates a renderer-owned native control island using a custom factory and a mount callback that may return an unmount cleanup.
    /// </summary>
    /// <typeparam name="TNative">The native control type accepted by the renderer.</typeparam>
    /// <param name="create">Factory that creates the native instance.</param>
    /// <param name="mount">Callback that configures the native instance and may return an unmount cleanup delegate.</param>
    /// <param name="render">Callback that projects Nuri state into the native control after mount and each render.</param>
    /// <returns>A <see cref="NativeElement"/> describing the native island.</returns>
    public static NativeElement Native<TNative>(
        Func<TNative> create,
        Func<TNative, Action?> mount,
        Action<TNative> render)
        where TNative : class
    {
        return CreateNative(create, null, mount, render);
    }

    private static NativeElement CreateNative<TNative>(
        Func<TNative> create,
        Action<TNative>? mount,
        Func<TNative, Action?>? mountWithCleanup,
        Action<TNative>? render)
        where TNative : class
    {
        if (create == null)
            throw new ArgumentNullException(nameof(create));

        return new NativeElement(new NativeControlDescriptor(
            typeof(TNative),
            () => create() ?? throw new InvalidOperationException($"Native factory for '{typeof(TNative).FullName}' returned null."),
            mountWithCleanup == null
                ? mount == null ? null : native =>
                {
                    mount((TNative)native);
                    return null;
                }
                : native => mountWithCleanup((TNative)native),
            render == null ? _ => { } : native => render((TNative)native)));
    }

    /// <summary>
    /// Creates an empty <see cref="ImageElement"/>.
    /// </summary>
    /// <returns>A new image element with no source.</returns>
    public static ImageElement Image()
    {
        return new ImageElement();
    }

    /// <summary>
    /// Creates an <see cref="ImageElement"/> displaying the image at <paramref name="source"/>.
    /// </summary>
    /// <param name="source">Image source path or URI.</param>
    /// <returns>A new image element.</returns>
    public static ImageElement Image(string source)
    {
        return new ImageElement(source);
    }

    /// <summary>
    /// Creates a text <see cref="Input"/> element.
    /// </summary>
    /// <returns>A new text input element.</returns>
    public static Input Input()
    {
        return new Input(InputTypes.Text);
    }

    /// <summary>
    /// Creates an <see cref="Input"/> element of the specified input kind.
    /// </summary>
    /// <param name="kind">The input kind (for example <see cref="InputTypes.Text"/>, <see cref="InputTypes.Button"/>, <see cref="InputTypes.Checkbox"/>).</param>
    /// <returns>A new input element.</returns>
    public static Input Input(string kind)
    {
        return new Input(kind);
    }

    /// <summary>
    /// Creates an <see cref="Input"/> element of the specified input kind with initial content.
    /// </summary>
    /// <param name="kind">The input kind.</param>
    /// <param name="content">Initial content for the input.</param>
    /// <returns>A new input element.</returns>
    public static Input Input(string kind, object content)
    {
        return new Input(kind, content);
    }

    /// <summary>
    /// Creates an <see cref="Input"/> element of the specified input kind with content and a click handler.
    /// </summary>
    /// <param name="kind">The input kind.</param>
    /// <param name="content">Initial content for the input.</param>
    /// <param name="handler">Handler invoked when the input is clicked.</param>
    /// <returns>A new input element wired to the click handler.</returns>
    public static Input Input(string kind, object content, Action handler)
    {
        return new Input(kind, content).OnClick(handler);
    }

    /// <summary>
    /// Creates a button <see cref="Input"/> with no content.
    /// </summary>
    /// <returns>A new button input element.</returns>
    public static Input Button()
    {
        return new Input(InputTypes.Button);
    }

    /// <summary>
    /// Creates a button <see cref="Input"/> with the given content.
    /// </summary>
    /// <param name="content">Content displayed on the button.</param>
    /// <returns>A new button input element.</returns>
    public static Input Button(object content)
    {
        return new Input(InputTypes.Button, content);
    }

    /// <summary>
    /// Creates a button <see cref="Input"/> with content and a click handler.
    /// </summary>
    /// <param name="content">Content displayed on the button.</param>
    /// <param name="handler">Handler invoked when the button is clicked.</param>
    /// <returns>A new button input element wired to the click handler.</returns>
    public static Input Button(object content, Action handler)
    {
        return Button(content).OnClick(handler);
    }

    /// <summary>
    /// Creates a check box <see cref="Input"/> with no content.
    /// </summary>
    /// <returns>A new check box input element.</returns>
    public static Input CheckBox()
    {
        return new Input(InputTypes.Checkbox);
    }

    /// <summary>
    /// Creates a check box <see cref="Input"/> with the given content.
    /// </summary>
    /// <param name="content">Content displayed next to the check box.</param>
    /// <returns>A new check box input element.</returns>
    public static Input CheckBox(object content)
    {
        return new Input(InputTypes.Checkbox, content);
    }

    /// <summary>
    /// Creates a check box <see cref="Input"/> with a checked-changed handler.
    /// </summary>
    /// <param name="handler">Handler invoked with the new checked state.</param>
    /// <returns>A new check box input element wired to the handler.</returns>
    public static Input CheckBox(Action<bool> handler)
    {
        return CheckBox().OnCheckChanged(handler);
    }

    /// <summary>
    /// Creates a check box <see cref="Input"/> with content and a checked-changed handler.
    /// </summary>
    /// <param name="content">Content displayed next to the check box.</param>
    /// <param name="handler">Handler invoked with the new checked state.</param>
    /// <returns>A new check box input element wired to the handler.</returns>
    public static Input CheckBox(object content, Action<bool> handler)
    {
        return CheckBox(content).OnCheckChanged(handler);
    }

    /// <summary>
    /// Creates a radio <see cref="Input"/> with no content.
    /// </summary>
    /// <returns>A new radio input element.</returns>
    public static Input RadioButton()
    {
        return new Input(InputTypes.Radio);
    }

    /// <summary>
    /// Creates a radio <see cref="Input"/> with the given content.
    /// </summary>
    /// <param name="content">Content displayed next to the radio button.</param>
    /// <returns>A new radio input element.</returns>
    public static Input RadioButton(object content)
    {
        return new Input(InputTypes.Radio, content);
    }

    /// <summary>
    /// Creates a radio <see cref="Input"/> with a checked-changed handler.
    /// </summary>
    /// <param name="handler">Handler invoked with the new checked state.</param>
    /// <returns>A new radio input element wired to the handler.</returns>
    public static Input RadioButton(Action<bool> handler)
    {
        return RadioButton().OnCheckChanged(handler);
    }

    /// <summary>
    /// Creates a radio <see cref="Input"/> with content and a checked-changed handler.
    /// </summary>
    /// <param name="content">Content displayed next to the radio button.</param>
    /// <param name="handler">Handler invoked with the new checked state.</param>
    /// <returns>A new radio input element wired to the handler.</returns>
    public static Input RadioButton(object content, Action<bool> handler)
    {
        return RadioButton(content).OnCheckChanged(handler);
    }

    /// <summary>
    /// Creates a single-line text box <see cref="Input"/> with vertically centered text.
    /// </summary>
    /// <returns>A new text box input element.</returns>
    public static Input TextBox()
    {
        return new Input(InputTypes.Text)
                    .TextVCenter();
    }

    /// <summary>
    /// Creates a text box <see cref="Input"/> with the given initial text.
    /// </summary>
    /// <param name="text">Initial text value.</param>
    /// <returns>A new text box input element.</returns>
    public static Input TextBox(string text)
    {
        return TextBox().TextValue(text);
    }

    /// <summary>
    /// Creates a text box <see cref="Input"/> with a text-changed handler.
    /// </summary>
    /// <param name="handler">Handler invoked with the new text value.</param>
    /// <returns>A new text box input element wired to the handler.</returns>
    public static Input TextBox(Action<string> handler)
    {
        return TextBox().OnTextChanged(handler);
    }

    /// <summary>
    /// Creates a text box <see cref="Input"/> with initial text and a text-changed handler.
    /// </summary>
    /// <param name="text">Initial text value.</param>
    /// <param name="handler">Handler invoked with the new text value.</param>
    /// <returns>A new text box input element wired to the handler.</returns>
    public static Input TextBox(string text, Action<string> handler)
    {
        return TextBox(text).OnTextChanged(handler);
    }

    /// <summary>
    /// Creates a password <see cref="Input"/> that masks entered text.
    /// </summary>
    /// <returns>A new password input element.</returns>
    public static Input PasswordBox()
    {
        return new Input(InputTypes.Password);
    }

    /// <summary>
    /// Creates a toggle <see cref="Input"/> with no content.
    /// </summary>
    /// <returns>A new toggle button input element.</returns>
    public static Input ToggleButton()
    {
        return new Input(InputTypes.Toggle);
    }

    /// <summary>
    /// Creates a toggle <see cref="Input"/> with the given content.
    /// </summary>
    /// <param name="content">Content displayed on the toggle button.</param>
    /// <returns>A new toggle button input element.</returns>
    public static Input ToggleButton(object content)
    {
        return new Input(InputTypes.Toggle, content);
    }

    /// <summary>
    /// Creates a toggle <see cref="Input"/> with content and a checked-changed handler.
    /// </summary>
    /// <param name="content">Content displayed on the toggle button.</param>
    /// <param name="handler">Handler invoked with the new toggled state.</param>
    /// <returns>A new toggle button input element wired to the handler.</returns>
    public static Input ToggleButton(object content, Action<bool> handler)
    {
        return ToggleButton(content).OnCheckChanged(handler);
    }

    /// <summary>
    /// Creates a list <see cref="ItemsView"/> containing the given children.
    /// </summary>
    /// <param name="children">Child elements to display in the list.</param>
    /// <returns>A new items view.</returns>
    public static ItemsView Items(params IElement[] children)
    {
        return new ItemsView(ItemsTypes.List, children);
    }

    /// <summary>
    /// Creates an <see cref="ItemsView"/> of the specified items kind containing the given children.
    /// </summary>
    /// <param name="kind">The items kind (for example <see cref="ItemsTypes.List"/> or <see cref="ItemsTypes.Virtualized"/>).</param>
    /// <param name="children">Child elements to display.</param>
    /// <returns>A new items view.</returns>
    public static ItemsView Items(string kind, params IElement[] children)
    {
        return new ItemsView(kind, children);
    }

    /// <summary>
    /// Creates a virtualized <see cref="ItemsView"/> that materializes only the items near the viewport using a fixed <paramref name="itemExtent"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source items.</param>
    /// <param name="keySelector">Produces a stable, sibling-unique key for each item.</param>
    /// <param name="itemExtent">Fixed extent (height) of each item in logical pixels.</param>
    /// <param name="itemTemplate">Renders an item into a virtual element.</param>
    /// <param name="comparer">Optional equality comparer used to detect item changes.</param>
    /// <returns>A new virtualized items view.</returns>
    public static ItemsView VirtualizedItems<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        double itemExtent,
        Func<T, IElement> itemTemplate,
        IEqualityComparer<T>? comparer = null)
    {
        var view = new ItemsView(ItemsTypes.Virtualized);
        view.Properties[PropertyKeys.VirtualizedItemsSource] = new VirtualizedItemsSource<T>(
            items,
            keySelector,
            itemExtent,
            false,
            5,
            5,
            0,
            0,
            itemTemplate,
            comparer);
        return view;
    }

    /// <summary>
    /// Creates a virtualized <see cref="ItemsView"/> with a symmetric item buffer and a default item extent.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source items.</param>
    /// <param name="itemTemplate">Renders an item into a virtual element.</param>
    /// <param name="buffer">Number of items buffered before and after the viewport.</param>
    /// <param name="itemExtent">Fixed extent (height) of each item in logical pixels.</param>
    /// <param name="itemKey">Optional stable, sibling-unique key for each item.</param>
    /// <param name="comparer">Optional equality comparer used to detect item changes.</param>
    /// <returns>A new virtualized items view.</returns>
    public static ItemsView VirtualizedItems<T>(
        IReadOnlyList<T> items,
        Func<T, IElement> itemTemplate,
        int buffer = 5,
        double itemExtent = 36,
        Func<T, string>? itemKey = null,
        IEqualityComparer<T>? comparer = null)
    {
        var view = new ItemsView(ItemsTypes.Virtualized);
        view.Properties[PropertyKeys.VirtualizedItemsSource] = new VirtualizedItemsSource<T>(
            items,
            itemKey,
            itemExtent,
            false,
            buffer,
            buffer,
            0,
            0,
            itemTemplate,
            comparer);
        return view;
    }

    /// <summary>
    /// Creates a virtualized <see cref="ItemsView"/> with separate before and after item buffers.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source items.</param>
    /// <param name="itemTemplate">Renders an item into a virtual element.</param>
    /// <param name="bufferBefore">Number of items buffered before the viewport.</param>
    /// <param name="bufferAfter">Number of items buffered after the viewport.</param>
    /// <param name="itemExtent">Fixed extent (height) of each item in logical pixels.</param>
    /// <param name="itemKey">Optional stable, sibling-unique key for each item.</param>
    /// <param name="comparer">Optional equality comparer used to detect item changes.</param>
    /// <returns>A new virtualized items view.</returns>
    public static ItemsView VirtualizedItems<T>(
        IReadOnlyList<T> items,
        Func<T, IElement> itemTemplate,
        int bufferBefore,
        int bufferAfter,
        double itemExtent = 36,
        Func<T, string>? itemKey = null,
        IEqualityComparer<T>? comparer = null)
    {
        var view = new ItemsView(ItemsTypes.Virtualized);
        view.Properties[PropertyKeys.VirtualizedItemsSource] = new VirtualizedItemsSource<T>(
            items,
            itemKey,
            itemExtent,
            false,
            bufferBefore,
            bufferAfter,
            0,
            0,
            itemTemplate,
            comparer);
        return view;
    }

    /// <summary>
    /// Creates a virtualized <see cref="ItemsView"/> that estimates item extents and buffers by pixel distance.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source items.</param>
    /// <param name="itemTemplate">Renders an item into a virtual element.</param>
    /// <param name="estimatedItemExtent">Estimated extent (height) of each item in logical pixels.</param>
    /// <param name="bufferPixels">Pixel distance buffered before and after the viewport.</param>
    /// <param name="itemKey">Optional stable, sibling-unique key for each item.</param>
    /// <param name="comparer">Optional equality comparer used to detect item changes.</param>
    /// <returns>A new virtualized items view.</returns>
    public static ItemsView VirtualizedItems<T>(
        IReadOnlyList<T> items,
        Func<T, IElement> itemTemplate,
        double estimatedItemExtent,
        double bufferPixels = 400,
        Func<T, string>? itemKey = null,
        IEqualityComparer<T>? comparer = null)
    {
        var view = new ItemsView(ItemsTypes.Virtualized);
        view.Properties[PropertyKeys.VirtualizedItemsSource] = new VirtualizedItemsSource<T>(
            items,
            itemKey,
            estimatedItemExtent,
            true,
            0,
            0,
            bufferPixels,
            bufferPixels,
            itemTemplate,
            comparer);
        return view;
    }

    /// <summary>
    /// Creates a popover <see cref="OverlayView"/> containing the given children.
    /// </summary>
    /// <param name="children">Child elements shown in the overlay.</param>
    /// <returns>A new overlay view.</returns>
    public static OverlayView Overlay(params IElement[] children)
    {
        return new OverlayView(OverlayTypes.Popover, children);
    }

    /// <summary>
    /// Creates an <see cref="OverlayView"/> of the specified overlay kind containing the given children.
    /// </summary>
    /// <param name="kind">The overlay kind.</param>
    /// <param name="children">Child elements shown in the overlay.</param>
    /// <returns>A new overlay view.</returns>
    public static OverlayView Overlay(string kind, params IElement[] children)
    {
        return new OverlayView(kind, children);
    }

    /// <summary>
    /// Creates a dropdown <see cref="SelectView"/> containing the given children.
    /// </summary>
    /// <param name="children">Child elements shown in the select.</param>
    /// <returns>A new select view.</returns>
    public static SelectView Select(params IElement[] children)
    {
        return new SelectView(SelectTypes.Dropdown, children);
    }

    /// <summary>
    /// Creates a <see cref="SelectView"/> of the specified select kind containing the given children.
    /// </summary>
    /// <param name="kind">The select kind.</param>
    /// <param name="children">Child elements shown in the select.</param>
    /// <returns>A new select view.</returns>
    public static SelectView Select(string kind, params IElement[] children)
    {
        return new SelectView(kind, children);
    }

    /// <summary>
    /// Creates an empty <see cref="Text"/> element.
    /// </summary>
    /// <returns>A new text element with no content.</returns>
    public static Text Text()
    {
        return new Text();
    }

    /// <summary>
    /// Creates a <see cref="Text"/> element with the given content.
    /// </summary>
    /// <param name="content">The text to display.</param>
    /// <returns>A new text element.</returns>
    public static Text Text(string content)
    {
        return new Text(content);
    }

    /// <summary>
    /// Gets an auto <see cref="LengthValue"/> that sizes to the content.
    /// </summary>
    public static LengthValue Auto => LengthValue.Auto();

    /// <summary>
    /// Gets a star <see cref="LengthValue"/> that shares remaining space equally (<c>1*</c>).
    /// </summary>
    public static LengthValue Star => LengthValue.Star();

    /// <summary>
    /// Creates a star <see cref="LengthValue"/> that shares remaining space by the given weight.
    /// </summary>
    /// <param name="value">The star weight.</param>
    /// <returns>A star length value.</returns>
    public static LengthValue Stars(double value)
    {
        return LengthValue.Star(value);
    }

    /// <summary>
    /// Creates a <see cref="RowHeights"/> wrapper from the given row length values.
    /// </summary>
    /// <param name="heights">Row length values.</param>
    /// <returns>A row heights descriptor.</returns>
    public static RowHeights Rows(params LengthValue[] heights)
    {
        return new RowHeights { Lengths = heights };
    }

    /// <summary>
    /// Creates a <see cref="RowHeights"/> wrapper by parsing a grid-length definition string (for example <c>"Auto, *, 100"</c>).
    /// </summary>
    /// <param name="definitions">A comma-separated grid-length definition string.</param>
    /// <returns>A row heights descriptor.</returns>
    public static RowHeights Rows(string definitions)
    {
        return Rows(GridLengthParser.Parse(definitions, nameof(definitions)));
    }

    /// <summary>
    /// Creates a <see cref="ColumnWidths"/> wrapper from the given column length values.
    /// </summary>
    /// <param name="widths">Column length values.</param>
    /// <returns>A column widths descriptor.</returns>
    public static ColumnWidths Columns(params LengthValue[] widths)
    {
        return new ColumnWidths { Lengths = widths };
    }

    /// <summary>
    /// Creates a <see cref="ColumnWidths"/> wrapper by parsing a grid-length definition string (for example <c>"Auto, *, 100"</c>).
    /// </summary>
    /// <param name="definitions">A comma-separated grid-length definition string.</param>
    /// <returns>A column widths descriptor.</returns>
    public static ColumnWidths Columns(string definitions)
    {
        return Columns(GridLengthParser.Parse(definitions, nameof(definitions)));
    }

    /// <summary>
    /// Creates a fixed pixel <see cref="LengthValue"/>.
    /// </summary>
    /// <param name="value">The size in logical pixels.</param>
    /// <returns>A pixel length value.</returns>
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
