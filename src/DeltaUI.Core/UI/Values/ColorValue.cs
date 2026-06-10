using System;
using System.Globalization;

namespace DeltaUI.Core.UI.Values
{
    public readonly struct ColorValue : IEquatable<ColorValue>
    {
        public ColorValue(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public byte A { get; }

        public byte R { get; }

        public byte G { get; }

        public byte B { get; }

        public static ColorValue FromArgb(byte a, byte r, byte g, byte b)
        {
            return new ColorValue(a, r, g, b);
        }

        public static ColorValue FromRgb(byte r, byte g, byte b)
        {
            return new ColorValue(255, r, g, b);
        }

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
