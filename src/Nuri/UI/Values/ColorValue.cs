using System;
using System.Globalization;

namespace Nuri.UI.Values
{
    /// <summary>
    /// An ARGB color value used for backgrounds, foregrounds, and borders.
    /// </summary>
    public readonly struct ColorValue : IEquatable<ColorValue>
    {
        /// <summary>
        /// Creates a color from alpha, red, green, and blue components (0-255).
        /// </summary>
        /// <param name="a">Alpha (opacity) component.</param>
        /// <param name="r">Red component.</param>
        /// <param name="g">Green component.</param>
        /// <param name="b">Blue component.</param>
        public ColorValue(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>Gets the alpha (opacity) component.</summary>
        public byte A { get; }

        /// <summary>Gets the red component.</summary>
        public byte R { get; }

        /// <summary>Gets the green component.</summary>
        public byte G { get; }

        /// <summary>Gets the blue component.</summary>
        public byte B { get; }

        /// <summary>
        /// Creates a color with explicit alpha, red, green, and blue components.
        /// </summary>
        /// <returns>A new color value.</returns>
        public static ColorValue FromArgb(byte a, byte r, byte g, byte b)
        {
            return new ColorValue(a, r, g, b);
        }

        /// <summary>
        /// Creates an opaque color (alpha 255) from red, green, and blue components.
        /// </summary>
        /// <returns>A new color value.</returns>
        public static ColorValue FromRgb(byte r, byte g, byte b)
        {
            return new ColorValue(255, r, g, b);
        }

        /// <summary>
        /// Parses a color from a hex string in <c>#RRGGBB</c> or <c>#AARRGGBB</c> format.
        /// </summary>
        /// <param name="value">The hex color string.</param>
        /// <returns>A new color value.</returns>
        public static ColorValue FromHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value[0] != '#')
                throw new ArgumentException("Color value must be in #RRGGBB or #AARRGGBB format.", nameof(value));

            var hex = value.Substring(1);
            if (hex.Length == 6)
                return FromRgb(ParseByte(hex, 0), ParseByte(hex, 2), ParseByte(hex, 4));

            if (hex.Length == 8)
                return FromArgb(ParseByte(hex, 0), ParseByte(hex, 2), ParseByte(hex, 4), ParseByte(hex, 6));

            throw new ArgumentException("Color value must be in #RRGGBB or #AARRGGBB format.", nameof(value));
        }

        public bool Equals(ColorValue other)
        {
            return A == other.A && R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object? obj)
        {
            return obj is ColorValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = A.GetHashCode();
                hashCode = (hashCode * 397) ^ R.GetHashCode();
                hashCode = (hashCode * 397) ^ G.GetHashCode();
                hashCode = (hashCode * 397) ^ B.GetHashCode();
                return hashCode;
            }
        }

        private static byte ParseByte(string hex, int startIndex)
        {
            return byte.Parse(hex.Substring(startIndex, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
    }
}
