using System;

namespace Nuri.UI.Values
{
    /// <summary>
    /// A four-sided thickness used for margins, padding, and border thickness (left, top, right, bottom).
    /// </summary>
    public readonly struct ThicknessValue : IEquatable<ThicknessValue>
    {
        /// <summary>
        /// Creates a thickness with explicit side values.
        /// </summary>
        /// <param name="left">Left value.</param>
        /// <param name="top">Top value.</param>
        /// <param name="right">Right value.</param>
        /// <param name="bottom">Bottom value.</param>
        public ThicknessValue(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>Gets the left value.</summary>
        public double Left { get; }

        /// <summary>Gets the top value.</summary>
        public double Top { get; }

        /// <summary>Gets the right value.</summary>
        public double Right { get; }

        /// <summary>Gets the bottom value.</summary>
        public double Bottom { get; }

        /// <summary>
        /// Creates a uniform thickness applied to all four sides.
        /// </summary>
        /// <param name="value">The uniform value.</param>
        /// <returns>A uniform thickness.</returns>
        public static ThicknessValue Uniform(double value)
        {
            return new ThicknessValue(value, value, value, value);
        }

        /// <summary>Determines whether this thickness equals another.</summary>
        public bool Equals(ThicknessValue other)
        {
            return Left.Equals(other.Left) && Top.Equals(other.Top) && Right.Equals(other.Right) && Bottom.Equals(other.Bottom);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ThicknessValue other && Equals(other);
        }

        /// <inheritdoc />
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
