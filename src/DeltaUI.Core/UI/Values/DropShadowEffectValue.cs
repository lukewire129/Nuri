namespace DeltaUI.Core.UI.Values
{
    public enum DropShadowRenderingBiasValue
    {
        Performance,
        Quality
    }

    public readonly struct DropShadowEffectValue : System.IEquatable<DropShadowEffectValue>
    {
        public DropShadowEffectValue(ColorValue color, double blurRadius, double shadowDepth, double opacity, double direction, DropShadowRenderingBiasValue renderingBias)
        {
            Color = color;
            BlurRadius = blurRadius;
            ShadowDepth = shadowDepth;
            Opacity = opacity;
            Direction = direction;
            RenderingBias = renderingBias;
        }

        public ColorValue Color { get; }

        public double BlurRadius { get; }

        public double ShadowDepth { get; }

        public double Depth => ShadowDepth;

        public double Opacity { get; }

        public double Direction { get; }

        public DropShadowRenderingBiasValue RenderingBias { get; }

        public bool Equals(DropShadowEffectValue other)
        {
            return Color.Equals(other.Color)
                && BlurRadius.Equals(other.BlurRadius)
                && ShadowDepth.Equals(other.ShadowDepth)
                && Opacity.Equals(other.Opacity)
                && Direction.Equals(other.Direction)
                && RenderingBias == other.RenderingBias;
        }

        public override bool Equals(object? obj)
        {
            return obj is DropShadowEffectValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Color.GetHashCode();
                hashCode = (hashCode * 397) ^ BlurRadius.GetHashCode();
                hashCode = (hashCode * 397) ^ ShadowDepth.GetHashCode();
                hashCode = (hashCode * 397) ^ Opacity.GetHashCode();
                hashCode = (hashCode * 397) ^ Direction.GetHashCode();
                hashCode = (hashCode * 397) ^ RenderingBias.GetHashCode();
                return hashCode;
            }
        }
    }
}
