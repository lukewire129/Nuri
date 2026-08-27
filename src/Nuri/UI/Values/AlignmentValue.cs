namespace Nuri.UI.Values
{
    /// <summary>
    /// Horizontal alignment of an element within its layout slot.
    /// </summary>
    public readonly struct HorizontalAlignmentValue
    {
        /// <summary>
        /// Creates a horizontal alignment value.
        /// </summary>
        /// <param name="kind">The alignment kind.</param>
        public HorizontalAlignmentValue(LayoutAlignmentKind kind)
        {
            Kind = kind;
        }

        /// <summary>Gets the alignment kind.</summary>
        public LayoutAlignmentKind Kind { get; }

        /// <summary>Aligns to the start (left).</summary>
        public static HorizontalAlignmentValue Start => new HorizontalAlignmentValue(LayoutAlignmentKind.Start);

        /// <summary>Centers horizontally.</summary>
        public static HorizontalAlignmentValue Center => new HorizontalAlignmentValue(LayoutAlignmentKind.Center);

        /// <summary>Aligns to the end (right).</summary>
        public static HorizontalAlignmentValue End => new HorizontalAlignmentValue(LayoutAlignmentKind.End);

        /// <summary>Stretches to fill the available width.</summary>
        public static HorizontalAlignmentValue Stretch => new HorizontalAlignmentValue(LayoutAlignmentKind.Stretch);
    }

    /// <summary>
    /// Vertical alignment of an element within its layout slot.
    /// </summary>
    public readonly struct VerticalAlignmentValue
    {
        /// <summary>
        /// Creates a vertical alignment value.
        /// </summary>
        /// <param name="kind">The alignment kind.</param>
        public VerticalAlignmentValue(LayoutAlignmentKind kind)
        {
            Kind = kind;
        }

        /// <summary>Gets the alignment kind.</summary>
        public LayoutAlignmentKind Kind { get; }

        /// <summary>Aligns to the top.</summary>
        public static VerticalAlignmentValue Start => new VerticalAlignmentValue(LayoutAlignmentKind.Start);

        /// <summary>Centers vertically.</summary>
        public static VerticalAlignmentValue Center => new VerticalAlignmentValue(LayoutAlignmentKind.Center);

        /// <summary>Aligns to the bottom.</summary>
        public static VerticalAlignmentValue End => new VerticalAlignmentValue(LayoutAlignmentKind.End);

        /// <summary>Stretches to fill the available height.</summary>
        public static VerticalAlignmentValue Stretch => new VerticalAlignmentValue(LayoutAlignmentKind.Stretch);
    }

    /// <summary>
    /// The alignment behavior within a layout slot.
    /// </summary>
    public enum LayoutAlignmentKind
    {
        /// <summary>Align to the start edge.</summary>
        Start,
        /// <summary>Align to the center.</summary>
        Center,
        /// <summary>Align to the end edge.</summary>
        End,
        /// <summary>Stretch to fill the available space.</summary>
        Stretch
    }
}
