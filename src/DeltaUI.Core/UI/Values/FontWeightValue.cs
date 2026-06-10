using System;

namespace DeltaUI.Core.UI.Values
{
    public readonly struct FontWeightValue : IEquatable<FontWeightValue>
    {
        public FontWeightValue(int openTypeWeight)
        {
            OpenTypeWeight = openTypeWeight;
        }

        public int OpenTypeWeight { get; }

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
