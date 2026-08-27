using System;

namespace Nuri.UI.Values
{
    /// <summary>
    /// A four-corner radius used to round element corners (top-left, top-right, bottom-right, bottom-left).
    /// </summary>
    public readonly struct CornerRadiusValue : IEquatable<CornerRadiusValue>
    {
        /// <summary>
        /// Creates a corner radius with explicit per-corner values.
        /// </summary>
        /// <param name="topLeft">Top-left radius.</param>
        /// <param name="topRight">Top-right radius.</param>
        /// <param name="bottomRight">Bottom-right radius.</param>
        /// <param name="bottomLeft">Bottom-left radius.</param>
        public CornerRadiusValue(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        /// <summary>Gets the top-left radius.</summary>
        public double TopLeft { get; }

        /// <summary>Gets the top-right radius.</summary>
        public double TopRight { get; }

        /// <summary>Gets the bottom-right radius.</summary>
        public double BottomRight { get; }

        /// <summary>Gets the bottom-left radius.</summary>
        public double BottomLeft { get; }

        /// <summary>
        /// Creates a uniform corner radius applied to all four corners.
        /// </summary>
        /// <param name="value">The uniform radius.</param>
        /// <returns>A uniform corner radius.</returns>
        public static CornerRadiusValue Uniform(double value)
        {
            return new CornerRadiusValue(value, value, value, value);
        }

        /// <summary>Determines whether this corner radius equals another.</summary>
        public bool Equals(CornerRadiusValue other)
        {
            return TopLeft.Equals(other.TopLeft) && TopRight.Equals(other.TopRight) && BottomRight.Equals(other.BottomRight) && BottomLeft.Equals(other.BottomLeft);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is CornerRadiusValue other && Equals(other);
        }

        /// <inheritdoc />
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
