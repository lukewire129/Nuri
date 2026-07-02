using System;

namespace Nuri.UI.Events
{
    public enum VirtualEventKind
    {
        Click,
        TextChanged,
        ContentChanged,
        CheckChanged,
        HoverChanged,
        KeyDown
    }

    public enum KeyboardKey
    {
        Unknown,
        Up,
        Down,
        Enter,
        Escape
    }

    public sealed class VirtualEvent : IEquatable<VirtualEvent>
    {
        public VirtualEvent(VirtualEventKind kind, Delegate handler)
        {
            Kind = kind;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public VirtualEventKind Kind { get; }

        public Delegate Handler { get; }

        public bool Equals(VirtualEvent? other)
        {
            return other != null
                && Kind == other.Kind
                && Equals(Handler, other.Handler);
        }

        public override bool Equals(object? obj)
        {
            return obj is VirtualEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ Handler.GetHashCode();
            }
        }
    }
}
