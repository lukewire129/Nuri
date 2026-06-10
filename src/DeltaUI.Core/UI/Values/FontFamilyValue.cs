using System;

namespace DeltaUI.Core.UI.Values
{
    public readonly struct FontFamilyValue : IEquatable<FontFamilyValue>
    {
        public FontFamilyValue(string source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public string Source { get; }

        public bool Equals(FontFamilyValue other)
        {
            return Source == other.Source;
        }

        public override bool Equals(object? obj)
        {
            return obj is FontFamilyValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode();
        }
    }
}
