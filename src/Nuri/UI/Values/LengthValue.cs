using System;

namespace Nuri.UI.Values
{
    /// <summary>
    /// Describes a length used for sizes, grid tracks, margins, and spacing. A length is either a fixed pixel value, a proportional star share, or auto (sized to content).
    /// </summary>
    public readonly struct LengthValue : IEquatable<LengthValue>
    {
        private LengthValue(double value, LengthUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        /// <summary>
        /// Gets the numeric value of the length (interpretation depends on <see cref="Unit"/>).
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Gets the unit of the length.
        /// </summary>
        public LengthUnit Unit { get; }

        /// <summary>
        /// Creates a fixed pixel length.
        /// </summary>
        /// <param name="value">The size in logical pixels.</param>
        /// <returns>A pixel length value.</returns>
        public static LengthValue Pixels(double value)
        {
            return new LengthValue(value, LengthUnit.Pixel);
        }

        /// <summary>
        /// Creates a proportional star length that shares remaining space by weight.
        /// </summary>
        /// <param name="value">The star weight (defaults to 1).</param>
        /// <returns>A star length value.</returns>
        public static LengthValue Star(double value = 1)
        {
            return new LengthValue(value, LengthUnit.Star);
        }

        /// <summary>
        /// Creates an auto length that sizes to the content.
        /// </summary>
        /// <returns>An auto length value.</returns>
        public static LengthValue Auto()
        {
            return new LengthValue(0, LengthUnit.Auto);
        }

        /// <summary>
        /// Implicitly converts a <see cref="double"/> into a fixed pixel length.
        /// </summary>
        /// <param name="value">The size in logical pixels.</param>
        /// <returns>A pixel length value.</returns>
        public static implicit operator LengthValue(double value)
        {
            return Pixels(value);
        }

        /// <summary>
        /// Determines whether this length equals another.
        /// </summary>
        /// <param name="other">The other length.</param>
        /// <returns><c>true</c> if value and unit match.</returns>
        public bool Equals(LengthValue other)
        {
            return Value.Equals(other.Value) && Unit == other.Unit;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is LengthValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Value.GetHashCode() * 397) ^ (int)Unit;
            }
        }
    }

    /// <summary>
    /// The unit of a <see cref="LengthValue"/>.
    /// </summary>
    public enum LengthUnit
    {
        /// <summary>A fixed size in logical pixels.</summary>
        Pixel,
        /// <summary>A proportional share of remaining space.</summary>
        Star,
        /// <summary>A size determined by content.</summary>
        Auto
    }
}
