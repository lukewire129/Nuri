using System;
using Nuri.Constants;
using Nuri.UI.Events;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace Nuri.WPF
{
    internal static class WpfEventMapper
    {
        public static bool TryCreate(string eventName, object? eventValue, out string wpfEventName, out Delegate handler)
        {
            switch (eventValue)
            {
                case Delegate nativeHandler:
                    wpfEventName = eventName;
                    handler = nativeHandler;
                    return true;
                case VirtualEvent virtualEvent:
                    return TryCreateVirtual(eventName, virtualEvent, out wpfEventName, out handler);
                default:
                    wpfEventName = string.Empty;
                    handler = null!;
                    return false;
            }
        }

        public static string GetHandlerKey(string eventName, object? eventValue)
        {
            return eventValue is VirtualEvent virtualEvent
                ? $"{eventName}:{virtualEvent.Kind}:{virtualEvent.Handler.GetHashCode()}"
                : $"{eventName}:native:{eventValue?.GetHashCode() ?? 0}";
        }

        private static bool TryCreateVirtual(string eventName, VirtualEvent virtualEvent, out string wpfEventName, out Delegate handler)
        {
            switch (virtualEvent.Kind)
            {
                case VirtualEventKind.Click:
                    if (eventName == EventKeys.MouseLeftButtonDown)
                    {
                        wpfEventName = EventKeys.MouseLeftButtonDown;
                        handler = new MouseButtonEventHandler((s, e) => Invoke(virtualEvent.Handler));
                    }
                    else
                    {
                        wpfEventName = EventKeys.Click;
                        handler = new RoutedEventHandler((s, e) => Invoke(virtualEvent.Handler));
                    }
                    return true;
                case VirtualEventKind.TextChanged:
                    wpfEventName = EventKeys.TextChanged;
                    handler = new TextChangedEventHandler((s, e) =>
                    {
                        if (s is FrameworkElement element && element.AreChangeEventsSuppressed())
                            return;

                        if (s is TextBox textBox)
                            Invoke(virtualEvent.Handler, textBox.Text);
                    });
                    return true;
                case VirtualEventKind.ContentChanged:
                    wpfEventName = EventKeys.ContentChanged;
                    handler = new RoutedEventHandler((s, e) =>
                    {
                        if (s is System.Windows.Controls.ContentControl contentControl)
                            Invoke(virtualEvent.Handler, contentControl.Content ?? string.Empty);
                    });
                    return true;
                case VirtualEventKind.CheckChanged:
                    wpfEventName = eventName;
                    handler = new RoutedEventHandler((s, e) =>
                    {
                        if (s is FrameworkElement element && element.AreChangeEventsSuppressed())
                            return;

                        if (s is CheckBox checkBox)
                            Invoke(virtualEvent.Handler, checkBox.IsChecked ?? false);
                        else if (s is RadioButton radioButton)
                            Invoke(virtualEvent.Handler, radioButton.IsChecked ?? false);
                        else if (s is ToggleButton toggleButton)
                            Invoke(virtualEvent.Handler, toggleButton.IsChecked ?? false);
                    });
                    return true;
                case VirtualEventKind.HoverChanged:
                    wpfEventName = eventName;
                    handler = new MouseEventHandler((s, e) => Invoke(virtualEvent.Handler, eventName == EventKeys.MouseEnter));
                    return true;
                case VirtualEventKind.PointerDown:
                    wpfEventName = GetMouseButtonEventName(virtualEvent, isDown: true);
                    handler = new MouseButtonEventHandler((s, e) =>
                    {
                        var element = s as FrameworkElement;
                        if (virtualEvent.CapturePointer)
                            element?.CaptureMouse();

                        try
                        {
                            var pointerEvent = CreatePointerEvent(s, e);
                            InvokePointer(virtualEvent.Handler, pointerEvent);
                            e.Handled |= pointerEvent.Handled;
                        }
                        catch
                        {
                            if (virtualEvent.CapturePointer)
                                element?.ReleaseMouseCapture();
                            throw;
                        }
                    });
                    return true;
                case VirtualEventKind.PointerMove:
                    wpfEventName = virtualEvent.Routing == EventRouting.Tunnel
                        ? EventKeys.PreviewMouseMove
                        : EventKeys.MouseMove;
                    handler = new MouseEventHandler((s, e) =>
                    {
                        var pointerEvent = CreatePointerEvent(s, e);
                        InvokePointer(virtualEvent.Handler, pointerEvent);
                        e.Handled |= pointerEvent.Handled;
                    });
                    return true;
                case VirtualEventKind.PointerUp:
                    wpfEventName = GetMouseButtonEventName(virtualEvent, isDown: false);
                    handler = new MouseButtonEventHandler((s, e) =>
                    {
                        try
                        {
                            var pointerEvent = CreatePointerEvent(s, e);
                            InvokePointer(virtualEvent.Handler, pointerEvent);
                            e.Handled |= pointerEvent.Handled;
                        }
                        finally
                        {
                            if (virtualEvent.CapturePointer && s is FrameworkElement element)
                                element.ReleaseMouseCapture();
                        }
                    });
                    return true;
                case VirtualEventKind.PointerWheel:
                    wpfEventName = virtualEvent.Routing == EventRouting.Tunnel
                        ? EventKeys.PreviewMouseWheel
                        : EventKeys.MouseWheel;
                    handler = new MouseWheelEventHandler((s, e) =>
                    {
                        var pointerEvent = CreatePointerWheelEvent(s, e);
                        InvokePointer(virtualEvent.Handler, pointerEvent);
                        e.Handled |= pointerEvent.Handled;
                    });
                    return true;
                case VirtualEventKind.KeyDown:
                    wpfEventName = eventName == EventKeys.PreviewKeyDown ? EventKeys.PreviewKeyDown : EventKeys.KeyDown;
                    handler = new KeyEventHandler((s, e) =>
                    {
                        var key = ToKeyboardKey(e);
                        if (key == KeyboardKey.Unknown || e.Handled)
                            return;

                        Invoke(virtualEvent.Handler, key);

                        if (key == KeyboardKey.Up || key == KeyboardKey.Down || key == KeyboardKey.Enter || key == KeyboardKey.Escape)
                            e.Handled = true;
                    });
                    return true;
                case VirtualEventKind.KeyUp:
                    wpfEventName = eventName == EventKeys.PreviewKeyUp ? EventKeys.PreviewKeyUp : EventKeys.KeyUp;
                    handler = new KeyEventHandler((s, e) =>
                    {
                        var key = ToKeyboardKey(e);
                        if (key == KeyboardKey.Unknown || e.Handled)
                            return;

                        Invoke(virtualEvent.Handler, key);
                    });
                    return true;
                case VirtualEventKind.FocusChanged:
                    wpfEventName = eventName == EventKeys.GotFocus ? EventKeys.GotFocus : EventKeys.LostFocus;
                    handler = new RoutedEventHandler((s, e) => Invoke(virtualEvent.Handler, eventName == EventKeys.GotFocus));
                    return true;
                case VirtualEventKind.Loaded:
                    wpfEventName = EventKeys.Loaded;
                    handler = new RoutedEventHandler((s, e) => Invoke(virtualEvent.Handler));
                    return true;
                case VirtualEventKind.Unloaded:
                    wpfEventName = EventKeys.Unloaded;
                    handler = new RoutedEventHandler((s, e) => Invoke(virtualEvent.Handler));
                    return true;
                default:
                    wpfEventName = string.Empty;
                    handler = null!;
                    return false;
            }
        }

        private static KeyboardKey ToKeyboardKey(KeyEventArgs args)
        {
            var key = args.Key == Key.System
                ? args.SystemKey
                : args.Key == Key.ImeProcessed
                    ? args.ImeProcessedKey
                    : args.Key;

            if (key == Key.Up)
                return KeyboardKey.Up;
            if (key == Key.Down)
                return KeyboardKey.Down;
            if (key == Key.Left)
                return KeyboardKey.Left;
            if (key == Key.Right)
                return KeyboardKey.Right;
            if (key == Key.Return)
                return KeyboardKey.Enter;
            if (key == Key.Escape)
                return KeyboardKey.Escape;
            if (key == Key.Tab)
                return KeyboardKey.Tab;
            if (key == Key.Space)
                return KeyboardKey.Space;
            if (key == Key.Back)
                return KeyboardKey.Backspace;
            if (key == Key.Delete)
                return KeyboardKey.Delete;

            return KeyboardKey.Unknown;
        }

        private static string GetMouseButtonEventName(VirtualEvent virtualEvent, bool isDown)
        {
            if (virtualEvent.Button == PointerButton.Secondary)
            {
                if (virtualEvent.Routing == EventRouting.Tunnel)
                    return isDown ? EventKeys.PreviewMouseRightButtonDown : EventKeys.PreviewMouseRightButtonUp;

                return isDown ? EventKeys.MouseRightButtonDown : EventKeys.MouseRightButtonUp;
            }

            if (virtualEvent.Routing == EventRouting.Tunnel)
                return isDown ? EventKeys.PreviewMouseLeftButtonDown : EventKeys.PreviewMouseLeftButtonUp;

            return isDown ? EventKeys.MouseLeftButtonDown : EventKeys.MouseLeftButtonUp;
        }

        private static PointerEvent CreatePointerEvent(object? source, MouseEventArgs args)
        {
            var element = source as FrameworkElement;
            var relativeTarget = element?.Parent as IInputElement ?? element;
            var point = relativeTarget == null ? new System.Windows.Point() : args.GetPosition(relativeTarget);
            var localPoint = element == null ? point : args.GetPosition(element);
            return new PointerEvent(
                point.X,
                point.Y,
                GetPointerButtons(args),
                GetModifiers(),
                args is MouseButtonEventArgs buttonArgs ? GetPointerButton(buttonArgs.ChangedButton) : null,
                localPoint.X,
                localPoint.Y);
        }

        private static PointerWheelEvent CreatePointerWheelEvent(object? source, MouseWheelEventArgs args)
        {
            var pointerEvent = CreatePointerEvent(source, args);
            return new PointerWheelEvent(
                pointerEvent.X,
                pointerEvent.Y,
                0,
                args.Delta,
                pointerEvent.Buttons,
                pointerEvent.Modifiers,
                pointerEvent.LocalX,
                pointerEvent.LocalY);
        }

        private static PointerButtons GetPointerButtons(MouseEventArgs args)
        {
            var buttons = PointerButtons.None;
            if (args.LeftButton == MouseButtonState.Pressed)
                buttons |= PointerButtons.Primary;
            if (args.RightButton == MouseButtonState.Pressed)
                buttons |= PointerButtons.Secondary;
            if (args.MiddleButton == MouseButtonState.Pressed)
                buttons |= PointerButtons.Middle;
            return buttons;
        }

        private static PointerButton? GetPointerButton(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => PointerButton.Primary,
                MouseButton.Right => PointerButton.Secondary,
                _ => null
            };
        }

        private static KeyModifiers GetModifiers()
        {
            var modifiers = KeyModifiers.None;
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                modifiers |= KeyModifiers.Control;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                modifiers |= KeyModifiers.Shift;
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                modifiers |= KeyModifiers.Alt;
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0)
                modifiers |= KeyModifiers.Meta;
            return modifiers;
        }

        private static void InvokePointer(Delegate handler, PointerEvent pointerEvent)
        {
            if (handler is Action action)
                action();
            else
                Invoke(handler, pointerEvent);
        }

        private static void Invoke(Delegate handler, params object[] values)
        {
            handler.DynamicInvoke(values);
        }
    }
}
