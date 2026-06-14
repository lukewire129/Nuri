using System;

namespace Nuri.UI.Values
{
    public readonly struct ThicknessValue : IEquatable<ThicknessValue>
    {
        public ThicknessValue(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public double Left { get; }

        public double Top { get; }

        public double Right { get; }

        public double Bottom { get; }

        public static ThicknessValue Uniform(double value)
        {
            return new ThicknessValue(value, value, value, value);
        }

        public bool Equals(ThicknessValue other)
        {
            return Left.Equals(other.Left) && Top.Equals(other.Top) && Right.Equals(other.Right) && Bottom.Equals(other.Bottom);
        }

        public override bool Equals(object? obj)
        {
            return obj is ThicknessValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Left.GetHashCode();
                hashCode = (hashCode * 397) ^ Top.GetHashCode();
                hashCode = (hashCode * 397) ^ Right.GetHashCode();
                hashCode = (hashCode * 397) ^ Bottom.GetHashCode();
                return hashCode;
            }
        }
    }
}
