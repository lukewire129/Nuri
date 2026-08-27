namespace Nuri.UI.Controls
{
    /// <summary>
    /// String identifiers for the top-level virtual control types.
    /// </summary>
    public static class VirtualControlTypes
    {
        /// <summary>A layout container.</summary>
        public const string Div = "Div";
        /// <summary>A top-level window.</summary>
        public const string Window = "Window";
        /// <summary>An image element.</summary>
        public const string Image = "Image";
        /// <summary>An interactive input control.</summary>
        public const string Input = "Input";
        /// <summary>A collection of items.</summary>
        public const string Items = "Items";
        /// <summary>A renderer-owned native control island.</summary>
        public const string Native = "Native";
        /// <summary>An overlay container.</summary>
        public const string Overlay = "Overlay";
        /// <summary>A selectable control.</summary>
        public const string Select = "Select";
        /// <summary>A text element.</summary>
        public const string Text = "Text";
    }

    /// <summary>
    /// Layout kinds for a <see cref="VirtualControlTypes.Div"/> container.
    /// </summary>
    public static class DivTypes
    {
        /// <summary>A block that overlays its children in the assigned bounds.</summary>
        public const string Block = "Block";
        /// <summary>A vertical stack layout.</summary>
        public const string Column = "Column";
        /// <summary>A grid layout with row and column definitions.</summary>
        public const string Grid = "Grid";
        /// <summary>A horizontal stack layout.</summary>
        public const string Row = "Row";
        /// <summary>A single-child scrollable layout.</summary>
        public const string Scroll = "Scroll";
        /// <summary>A layout that positions children at explicit coordinates.</summary>
        public const string Absolute = "Absolute";
        /// <summary>A 2D camera over a single content element.</summary>
        public const string Viewport = "Viewport";
        /// <summary>A layout that wraps children onto multiple lines.</summary>
        public const string Wrap = "Wrap";
    }

    /// <summary>
    /// Behavior kinds for an <see cref="VirtualControlTypes.Input"/> element.
    /// </summary>
    public static class InputTypes
    {
        /// <summary>A standard button.</summary>
        public const string Button = "Button";
        /// <summary>A check box.</summary>
        public const string Checkbox = "Checkbox";
        /// <summary>A button styled to signal a destructive action.</summary>
        public const string Destructive = "Destructive";
        /// <summary>A button styled as a hyperlink.</summary>
        public const string Link = "Link";
        /// <summary>A password input that masks text.</summary>
        public const string Password = "Password";
        /// <summary>A button styled as the primary action.</summary>
        public const string Primary = "Primary";
        /// <summary>A radio button.</summary>
        public const string Radio = "Radio";
        /// <summary>A button styled as a secondary action.</summary>
        public const string Secondary = "Secondary";
        /// <summary>A button that submits a form.</summary>
        public const string Submit = "Submit";
        /// <summary>A single-line text input.</summary>
        public const string Text = "Text";
        /// <summary>A toggle button.</summary>
        public const string Toggle = "Toggle";
    }

    /// <summary>
    /// Presentation kinds for an <see cref="VirtualControlTypes.Items"/> container.
    /// </summary>
    public static class ItemsTypes
    {
        /// <summary>A grid of items.</summary>
        public const string Grid = "Grid";
        /// <summary>A list of items.</summary>
        public const string List = "List";
        /// <summary>A table of items.</summary>
        public const string Table = "Table";
        /// <summary>A hierarchical tree of items.</summary>
        public const string Tree = "Tree";
        /// <summary>A virtualized list that materializes only visible items.</summary>
        public const string Virtualized = "Virtualized";
    }

    /// <summary>
    /// Presentation kinds for a <see cref="VirtualControlTypes.Select"/> container.
    /// </summary>
    public static class SelectTypes
    {
        /// <summary>A single-selection dropdown.</summary>
        public const string Dropdown = "Dropdown";
        /// <summary>A multi-selection control.</summary>
        public const string Multi = "Multi";
    }

    /// <summary>
    /// Rendering kinds for an <see cref="VirtualControlTypes.Image"/> element.
    /// </summary>
    public static class ImageTypes
    {
        /// <summary>The default image rendering.</summary>
        public const string Default = "Default";
        /// <summary>An icon-style image.</summary>
        public const string Icon = "Icon";
    }

    /// <summary>
    /// Presentation kinds for an <see cref="VirtualControlTypes.Overlay"/> container.
    /// </summary>
    public static class OverlayTypes
    {
        /// <summary>A modal overlay that blocks interaction with content behind it.</summary>
        public const string Modal = "Modal";
        /// <summary>A popover overlay anchored to content.</summary>
        public const string Popover = "Popover";
        /// <summary>A tooltip overlay.</summary>
        public const string Tooltip = "Tooltip";
    }
}
