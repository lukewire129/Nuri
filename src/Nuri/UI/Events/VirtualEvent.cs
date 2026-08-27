using System;

namespace Nuri.UI.Events
{
    /// <summary>
    /// The kind of a virtual event, used by renderers to materialize the appropriate native behavior.
    /// </summary>
    public enum VirtualEventKind
    {
        /// <summary>A click or left mouse button down.</summary>
        Click,
        /// <summary>The input text changed.</summary>
        TextChanged,
        /// <summary>The element content changed.</summary>
        ContentChanged,
        /// <summary>The checked state of a checkable input changed.</summary>
        CheckChanged,
        /// <summary>The pointer entered or left the element.</summary>
        HoverChanged,
        /// <summary>The pointer was pressed down.</summary>
        PointerDown,
        /// <summary>The pointer moved.</summary>
        PointerMove,
        /// <summary>The pointer was released.</summary>
        PointerUp,
        /// <summary>The pointer wheel rotated.</summary>
        PointerWheel,
        /// <summary>A key was pressed down.</summary>
        KeyDown,
        /// <summary>A key was released.</summary>
        KeyUp,
        /// <summary>The element gained or lost focus.</summary>
        FocusChanged,
        /// <summary>The element was loaded into the visual tree.</summary>
        Loaded,
        /// <summary>The element was removed from the visual tree.</summary>
        Unloaded
    }

    /// <summary>
    /// Recognized keyboard keys reported by key events.
    /// </summary>
    public enum KeyboardKey
    {
        /// <summary>An unrecognized key.</summary>
        Unknown,
        /// <summary>The up arrow key.</summary>
        Up,
        /// <summary>The down arrow key.</summary>
        Down,
        /// <summary>The left arrow key.</summary>
        Left,
        /// <summary>The right arrow key.</summary>
        Right,
        /// <summary>The Enter key.</summary>
        Enter,
        /// <summary>The Escape key.</summary>
        Escape,
        /// <summary>The Tab key.</summary>
        Tab,
        /// <summary>The Space key.</summary>
        Space,
        /// <summary>The Backspace key.</summary>
        Backspace,
        /// <summary>The Delete key.</summary>
        Delete
    }

    /// <summary>
    /// The direction an event travels through the element tree.
    /// </summary>
    public enum EventRouting
    {
        /// <summary>The event bubbles from the target up to ancestors (default).</summary>
        Bubble,
        /// <summary>The event tunnels from ancestors down to the target.</summary>
        Tunnel
    }

    /// <summary>
    /// A logical pointer button used when attaching pointer handlers.
    /// </summary>
    public enum PointerButton
    {
        /// <summary>The primary (typically left) button.</summary>
        Primary,
        /// <summary>The secondary (typically right) button.</summary>
        Secondary
    }

    /// <summary>
    /// The set of pointer buttons currently pressed, as flags.
    /// </summary>
    [Flags]
    public enum PointerButtons
    {
        /// <summary>No buttons pressed.</summary>
        None = 0,
        /// <summary>The primary button pressed.</summary>
        Primary = 1,
        /// <summary>The secondary button pressed.</summary>
        Secondary = 2,
        /// <summary>The middle button pressed.</summary>
        Middle = 4
    }

    /// <summary>
    /// Keyboard modifier keys held during an event, as flags.
    /// </summary>
    [Flags]
    public enum KeyModifiers
    {
        /// <summary>No modifiers held.</summary>
        None = 0,
        /// <summary>The Control modifier held.</summary>
        Control = 1,
        /// <summary>The Shift modifier held.</summary>
        Shift = 2,
        /// <summary>The Alt modifier held.</summary>
        Alt = 4,
        /// <summary>The Meta (Windows/Command) modifier held.</summary>
        Meta = 8
    }

    /// <summary>
    /// Describes a pointer event with coordinates, buttons, and modifiers. Coordinates <see cref="X"/>/<see cref="Y"/> are in the immediate parent layout space; <see cref="LocalX"/>/<see cref="LocalY"/> are relative to the event source.
    /// </summary>
    public class PointerEvent
    {
        /// <summary>
        /// Creates a pointer event with coordinates and a primary-button flag.
        /// </summary>
        /// <param name="x">The X coordinate in the parent layout space.</param>
        /// <param name="y">The Y coordinate in the parent layout space.</param>
        /// <param name="isPrimaryButtonPressed">Whether the primary button is pressed.</param>
        public PointerEvent(double x, double y, bool isPrimaryButtonPressed = false)
            : this(
                x,
                y,
                isPrimaryButtonPressed ? PointerButtons.Primary : PointerButtons.None)
        {
        }

        /// <summary>
        /// Creates a pointer event with full detail.
        /// </summary>
        /// <param name="x">The X coordinate in the parent layout space.</param>
        /// <param name="y">The Y coordinate in the parent layout space.</param>
        /// <param name="buttons">The pressed buttons.</param>
        /// <param name="modifiers">The held key modifiers.</param>
        /// <param name="changedButton">The button that changed, if any.</param>
        /// <param name="localX">The X coordinate relative to the event source (defaults to <paramref name="x"/>).</param>
        /// <param name="localY">The Y coordinate relative to the event source (defaults to <paramref name="y"/>).</param>
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

        /// <summary>Gets the X coordinate in the immediate parent layout space.</summary>
        public double X { get; }

        /// <summary>Gets the Y coordinate in the immediate parent layout space.</summary>
        public double Y { get; }

        /// <summary>Gets the X coordinate relative to the event source.</summary>
        public double LocalX { get; }

        /// <summary>Gets the Y coordinate relative to the event source.</summary>
        public double LocalY { get; }

        /// <summary>Gets the set of pressed buttons.</summary>
        public PointerButtons Buttons { get; }

        /// <summary>Gets the held key modifiers.</summary>
        public KeyModifiers Modifiers { get; }

        /// <summary>Gets the button that changed, if the event is button-specific.</summary>
        public PointerButton? ChangedButton { get; }

        /// <summary>Gets a value indicating whether the primary button is pressed.</summary>
        public bool IsPrimaryButtonPressed => (Buttons & PointerButtons.Primary) != 0;

        /// <summary>Gets a value indicating whether the secondary button is pressed.</summary>
        public bool IsSecondaryButtonPressed => (Buttons & PointerButtons.Secondary) != 0;

        /// <summary>Gets a value indicating whether the middle button is pressed.</summary>
        public bool IsMiddleButtonPressed => (Buttons & PointerButtons.Middle) != 0;

        /// <summary>
        /// Gets or sets a value indicating whether the event has been handled, stopping further routing.
        /// </summary>
        public bool Handled { get; set; }
    }

    /// <summary>
    /// A <see cref="PointerEvent"/> that also carries wheel deltas.
    /// </summary>
    public sealed class PointerWheelEvent : PointerEvent
    {
        /// <summary>
        /// Creates a pointer wheel event.
        /// </summary>
        /// <param name="x">The X coordinate in the parent layout space.</param>
        /// <param name="y">The Y coordinate in the parent layout space.</param>
        /// <param name="deltaX">The horizontal wheel delta.</param>
        /// <param name="deltaY">The vertical wheel delta.</param>
        /// <param name="buttons">The pressed buttons.</param>
        /// <param name="modifiers">The held key modifiers.</param>
        /// <param name="localX">The X coordinate relative to the event source.</param>
        /// <param name="localY">The Y coordinate relative to the event source.</param>
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

        /// <summary>Gets the horizontal wheel delta.</summary>
        public double DeltaX { get; }

        /// <summary>Gets the vertical wheel delta.</summary>
        public double DeltaY { get; }
    }

    /// <summary>
    /// A neutral description of an event attached to an element, materialized by each renderer.
    /// </summary>
    public sealed class VirtualEvent : IEquatable<VirtualEvent>
    {
        /// <summary>
        /// Creates a virtual event.
        /// </summary>
        /// <param name="kind">The event kind.</param>
        /// <param name="handler">The handler delegate (must not be null).</param>
        /// <param name="capturePointer">Whether to capture the pointer when the event fires.</param>
        /// <param name="routing">The event routing mode.</param>
        /// <param name="button">The logical button the handler targets.</param>
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

        /// <summary>Gets the event kind.</summary>
        public VirtualEventKind Kind { get; }

        /// <summary>Gets the handler delegate.</summary>
        public Delegate Handler { get; }

        /// <summary>Gets a value indicating whether the pointer is captured when the event fires.</summary>
        public bool CapturePointer { get; }

        /// <summary>Gets the event routing mode.</summary>
        public EventRouting Routing { get; }

        /// <summary>Gets the logical button the handler targets.</summary>
        public PointerButton Button { get; }

        /// <summary>Determines whether this virtual event equals another.</summary>
        public bool Equals(VirtualEvent? other)
        {
            return other != null
                && Kind == other.Kind
                && CapturePointer == other.CapturePointer
                && Routing == other.Routing
                && Button == other.Button
                && Equals(Handler, other.Handler);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is VirtualEvent other && Equals(other);
        }

        /// <inheritdoc />
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
