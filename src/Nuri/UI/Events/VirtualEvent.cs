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
        PointerDown,
        PointerMove,
        PointerUp,
        KeyDown,
        KeyUp,
        FocusChanged,
        Loaded,
        Unloaded
    }

    public enum KeyboardKey
    {
        Unknown,
        Up,
        Down,
        Left,
        Right,
        Enter,
        Escape,
        Tab,
        Space,
        Backspace,
        Delete
    }

    public enum EventRouting
    {
        Bubble,
        Tunnel
    }

    public sealed class PointerEvent
    {
        public PointerEvent(double x, double y, bool isPrimaryButtonPressed = false)
        {
            X = x;
            Y = y;
            IsPrimaryButtonPressed = isPrimaryButtonPressed;
        }

        public double X { get; }

        public double Y { get; }

        public bool IsPrimaryButtonPressed { get; }
    }

    public sealed class VirtualEvent : IEquatable<VirtualEvent>
    {
        public VirtualEvent(
            VirtualEventKind kind,
            Delegate handler,
            bool capturePointer = false,
            EventRouting routing = EventRouting.Bubble)
        {
            Kind = kind;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            CapturePointer = capturePointer;
            Routing = routing;
        }

        public VirtualEventKind Kind { get; }

        public Delegate Handler { get; }

        public bool CapturePointer { get; }

        public EventRouting Routing { get; }

        public bool Equals(VirtualEvent? other)
        {
            return other != null
                && Kind == other.Kind
                && CapturePointer == other.CapturePointer
                && Routing == other.Routing
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
                var hashCode = ((int)Kind * 397) ^ Handler.GetHashCode();
                hashCode = (hashCode * 397) ^ CapturePointer.GetHashCode();
                return (hashCode * 397) ^ (int)Routing;
            }
        }
    }
}
