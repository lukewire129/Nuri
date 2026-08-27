namespace Nuri.UI.Values
{
    /// <summary>
    /// The algorithm used when scaling images.
    /// </summary>
    public readonly struct ImageScalingModeValue
    {
        /// <summary>
        /// Creates an image scaling mode value.
        /// </summary>
        /// <param name="kind">The scaling mode kind.</param>
        public ImageScalingModeValue(ImageScalingModeKind kind)
        {
            Kind = kind;
        }

        /// <summary>Gets the scaling mode kind.</summary>
        public ImageScalingModeKind Kind { get; }

        /// <summary>Low-quality (fast) scaling.</summary>
        public static ImageScalingModeValue LowQuality => new ImageScalingModeValue(ImageScalingModeKind.LowQuality);

        /// <summary>High-quality scaling.</summary>
        public static ImageScalingModeValue HighQuality => new ImageScalingModeValue(ImageScalingModeKind.HighQuality);

        /// <summary>Fant (smooth) scaling.</summary>
        public static ImageScalingModeValue Fant => new ImageScalingModeValue(ImageScalingModeKind.Fant);

        /// <summary>Nearest-neighbor (pixelated) scaling.</summary>
        public static ImageScalingModeValue NearestNeighbor => new ImageScalingModeValue(ImageScalingModeKind.NearestNeighbor);
    }

    /// <summary>
    /// The available image scaling modes.
    /// </summary>
    public enum ImageScalingModeKind
    {
        /// <summary>Low quality.</summary>
        LowQuality,
        /// <summary>High quality.</summary>
        HighQuality,
        /// <summary>Fant.</summary>
        Fant,
        /// <summary>Nearest neighbor.</summary>
        NearestNeighbor
    }
}
