using System;

namespace Nuri.UI.Values
{
    public readonly struct TextOverflowValue : IEquatable<TextOverflowValue>
    {
        public TextOverflowValue(TextOverflowKind kind)
        {
            Kind = kind;
        }

        public TextOverflowKind Kind { get; }

        public static TextOverflowValue Clip => new TextOverflowValue(TextOverflowKind.Clip);

        public static TextOverflowValue Ellipsis => new TextOverflowValue(TextOverflowKind.Ellipsis);

        public static TextOverflowValue Wrap => new TextOverflowValue(TextOverflowKind.Wrap);

        public bool Equals(TextOverflowValue other)
        {
            return Kind == other.Kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is TextOverflowValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Kind;
        }
    }

    public enum TextOverflowKind
    {
        Clip,
        Ellipsis,
        Wrap
    }
}
