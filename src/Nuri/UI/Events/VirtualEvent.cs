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
        PointerWheel,
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

    public enum PointerButton
    {
        Primary,
        Secondary
    }

    [Flags]
    public enum PointerButtons
    {
        None = 0,
        Primary = 1,
        Secondary = 2,
        Middle = 4
    }

    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Control = 1,
        Shift = 2,
        Alt = 4,
        Meta = 8
    }

    public class PointerEvent
    {
        public PointerEvent(double x, double y, bool isPrimaryButtonPressed = false)
            : this(
                x,
                y,
                isPrimaryButtonPressed ? PointerButtons.Primary : PointerButtons.None)
        {
        }

        public PointerEvent(
            double x,
            double y,
            PointerButtons buttons,
            KeyModifiers modifiers = KeyModifiers.None,
            PointerButton? changedButton = null,
            double? localX = null,
            double? localY = null)
        {
            X = x;
            Y = y;
            LocalX = localX ?? x;
            LocalY = localY ?? y;
            Buttons = buttons;
            Modifiers = modifiers;
            ChangedButton = changedButton;
        }

        public double X { get; }

        public double Y { get; }

        public double LocalX { get; }

        public double LocalY { get; }

        public PointerButtons Buttons { get; }

        public KeyModifiers Modifiers { get; }

        public PointerButton? ChangedButton { get; }

        public bool IsPrimaryButtonPressed => (Buttons & PointerButtons.Primary) != 0;

        public bool IsSecondaryButtonPressed => (Buttons & PointerButtons.Secondary) != 0;

        public bool IsMiddleButtonPressed => (Buttons & PointerButtons.Middle) != 0;

        public bool Handled { get; set; }
    }

    public sealed class PointerWheelEvent : PointerEvent
    {
        public PointerWheelEvent(
            double x,
            double y,
            double deltaX,
            double deltaY,
            PointerButtons buttons = PointerButtons.None,
            KeyModifiers modifiers = KeyModifiers.None,
            double? localX = null,
            double? localY = null)
            : base(x, y, buttons, modifiers, localX: localX, localY: localY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public double DeltaX { get; }

        public double DeltaY { get; }
    }

    public sealed class VirtualEvent : IEquatable<VirtualEvent>
    {
        public VirtualEvent(
            VirtualEventKind kind,
            Delegate handler,
            bool capturePointer = false,
            EventRouting routing = EventRouting.Bubble,
            PointerButton button = PointerButton.Primary)
        {
            Kind = kind;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            CapturePointer = capturePointer;
            Routing = routing;
            Button = button;
        }

        public VirtualEventKind Kind { get; }

        public Delegate Handler { get; }

        public bool CapturePointer { get; }

        public EventRouting Routing { get; }

        public PointerButton Button { get; }

        public bool Equals(VirtualEvent? other)
        {
            return other != null
                && Kind == other.Kind
                && CapturePointer == other.CapturePointer
                && Routing == other.Routing
                && Button == other.Button
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
                hashCode = (hashCode * 397) ^ (int)Routing;
                return (hashCode * 397) ^ (int)Button;
            }
        }
    }
}
