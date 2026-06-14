namespace Nuri.UI.Values
{
    public readonly struct HorizontalAlignmentValue
    {
        public HorizontalAlignmentValue(LayoutAlignmentKind kind)
        {
            Kind = kind;
        }

        public LayoutAlignmentKind Kind { get; }

        public static HorizontalAlignmentValue Start => new HorizontalAlignmentValue(LayoutAlignmentKind.Start);

        public static HorizontalAlignmentValue Center => new HorizontalAlignmentValue(LayoutAlignmentKind.Center);

        public static HorizontalAlignmentValue End => new HorizontalAlignmentValue(LayoutAlignmentKind.End);

        public static HorizontalAlignmentValue Stretch => new HorizontalAlignmentValue(LayoutAlignmentKind.Stretch);
    }

    public readonly struct VerticalAlignmentValue
    {
        public VerticalAlignmentValue(LayoutAlignmentKind kind)
        {
            Kind = kind;
        }

        public LayoutAlignmentKind Kind { get; }

        public static VerticalAlignmentValue Start => new VerticalAlignmentValue(LayoutAlignmentKind.Start);

        public static VerticalAlignmentValue Center => new VerticalAlignmentValue(LayoutAlignmentKind.Center);

        public static VerticalAlignmentValue End => new VerticalAlignmentValue(LayoutAlignmentKind.End);

        public static VerticalAlignmentValue Stretch => new VerticalAlignmentValue(LayoutAlignmentKind.Stretch);
    }

    public enum LayoutAlignmentKind
    {
        Start,
        Center,
        End,
        Stretch
    }
}
