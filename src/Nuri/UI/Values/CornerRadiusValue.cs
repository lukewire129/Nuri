using System;

namespace Nuri.UI.Values
{
    public readonly struct CornerRadiusValue : IEquatable<CornerRadiusValue>
    {
        public CornerRadiusValue(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        public double TopLeft { get; }

        public double TopRight { get; }

        public double BottomRight { get; }

        public double BottomLeft { get; }

        
        public static CornerRadiusValue Uniform(double value)
        {
            return new CornerRadiusValue(value, value, value, value);
        }

        public bool Equals(CornerRadiusValue other)
        {
            return TopLeft.Equals(other.TopLeft) && TopRight.Equals(other.TopRight) && BottomRight.Equals(other.BottomRight) && BottomLeft.Equals(other.BottomLeft);
        }

        public override bool Equals(object? obj)
        {
            return obj is CornerRadiusValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TopLeft.GetHashCode();
                hashCode = (hashCode * 397) ^ TopRight.GetHashCode();
                hashCode = (hashCode * 397) ^ BottomRight.GetHashCode();
                hashCode = (hashCode * 397) ^ BottomLeft.GetHashCode();
                return hashCode;
            }
        }
    }
}
