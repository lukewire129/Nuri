using System;

namespace Nuri.UI.Values
{
    public readonly struct FontWeightValue : IEquatable<FontWeightValue>
    {
        public FontWeightValue(int openTypeWeight)
        {
            OpenTypeWeight = openTypeWeight;
        }

        public int OpenTypeWeight { get; }

        public static FontWeightValue Normal => new FontWeightValue(400);

        public static FontWeightValue Bold => new FontWeightValue(700);

        public bool Equals(FontWeightValue other)
        {
            return OpenTypeWeight == other.OpenTypeWeight;
        }

        public override bool Equals(object? obj)
        {
            return obj is FontWeightValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return OpenTypeWeight;
        }
    }
}
