using System;

namespace Nuri.UI.Values
{
    /// <summary>
    /// The weight (thickness) of text, expressed as an OpenType weight value.
    /// </summary>
    public readonly struct FontWeightValue : IEquatable<FontWeightValue>
    {
        /// <summary>
        /// Creates a font weight from an OpenType weight (for example 400 = normal, 700 = bold).
        /// </summary>
        /// <param name="openTypeWeight">The OpenType weight value.</param>
        public FontWeightValue(int openTypeWeight)
        {
            OpenTypeWeight = openTypeWeight;
        }

        /// <summary>Gets the OpenType weight value.</summary>
        public int OpenTypeWeight { get; }

        /// <summary>Normal weight (400).</summary>
        public static FontWeightValue Normal => new FontWeightValue(400);

        /// <summary>Bold weight (700).</summary>
        public static FontWeightValue Bold => new FontWeightValue(700);

        /// <summary>Determines whether this weight equals another.</summary>
        public bool Equals(FontWeightValue other)
        {
            return OpenTypeWeight == other.OpenTypeWeight;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is FontWeightValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return OpenTypeWeight;
        }
    }
}
