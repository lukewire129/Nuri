namespace Nuri.UI.Values
{
    public readonly struct ImageScalingModeValue
    {
        public ImageScalingModeValue(ImageScalingModeKind kind)
        {
            Kind = kind;
        }

        public ImageScalingModeKind Kind { get; }

        public static ImageScalingModeValue LowQuality => new ImageScalingModeValue(ImageScalingModeKind.LowQuality);

        public static ImageScalingModeValue HighQuality => new ImageScalingModeValue(ImageScalingModeKind.HighQuality);

        public static ImageScalingModeValue Fant => new ImageScalingModeValue(ImageScalingModeKind.Fant);

        public static ImageScalingModeValue NearestNeighbor => new ImageScalingModeValue(ImageScalingModeKind.NearestNeighbor);
    }

    public enum ImageScalingModeKind
    {
        LowQuality,
        HighQuality,
        Fant,
        NearestNeighbor
    }
}
