using System;

namespace Nuri.UI.Values
{
    /// <summary>
    /// How text overflows its bounds when it does not fit.
    /// </summary>
    public readonly struct TextOverflowValue : IEquatable<TextOverflowValue>
    {
        /// <summary>
        /// Creates a text overflow value.
        /// </summary>
        /// <param name="kind">The overflow kind.</param>
        public TextOverflowValue(TextOverflowKind kind)
        {
            Kind = kind;
        }

        /// <summary>Gets the overflow kind.</summary>
        public TextOverflowKind Kind { get; }

        /// <summary>Clip overflowing text without indicator.</summary>
        public static TextOverflowValue Clip => new TextOverflowValue(TextOverflowKind.Clip);

        /// <summary>Replace overflowing text with an ellipsis.</summary>
        public static TextOverflowValue Ellipsis => new TextOverflowValue(TextOverflowKind.Ellipsis);

        /// <summary>Wrap text onto multiple lines.</summary>
        public static TextOverflowValue Wrap => new TextOverflowValue(TextOverflowKind.Wrap);

        /// <summary>Determines whether this value equals another.</summary>
        public bool Equals(TextOverflowValue other)
        {
            return Kind == other.Kind;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is TextOverflowValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)Kind;
        }
    }

    /// <summary>
    /// The available text overflow behaviors.
    /// </summary>
    public enum TextOverflowKind
    {
        /// <summary>Clip the overflow.</summary>
        Clip,
        /// <summary>Show an ellipsis for the overflow.</summary>
        Ellipsis,
        /// <summary>Wrap onto multiple lines.</summary>
        Wrap
    }
}
