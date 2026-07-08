using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nuri.Constants;
using Nuri.UI.Events;

namespace Nuri.Avalonia
{
    internal static class AvaloniaEventMapper
    {
        public static bool TryAttach(Control control, string eventName, object? value, out string handlerKey, out Delegate handler)
        {
            handlerKey = GetHandlerKey(eventName, value);
            handler = null!;

            if (value is not VirtualEvent virtualEvent)
                return false;

            if (virtualEvent.Kind == VirtualEventKind.Click && control is Button button)
            {
                EventHandler<RoutedEventArgs> clickHandler = (_, __) => virtualEvent.Handler.DynamicInvoke();
                button.Click += clickHandler;
                handler = clickHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.Click && eventName == EventKeys.MouseLeftButtonDown)
            {
                EventHandler<PointerPressedEventArgs> pointerHandler = (_, __) => virtualEvent.Handler.DynamicInvoke();
                control.PointerPressed += pointerHandler;
                handler = pointerHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.TextChanged && control is TextBox textBox)
            {
                EventHandler<TextChangedEventArgs> textHandler = (sender, _) => virtualEvent.Handler.DynamicInvoke(((TextBox)sender!).Text ?? string.Empty);
                textBox.TextChanged += textHandler;
                handler = textHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.HoverChanged)
            {
                EventHandler<PointerEventArgs> hoverHandler = (_, __) => virtualEvent.Handler.DynamicInvoke(eventName == EventKeys.MouseEnter);

                if (eventName == EventKeys.MouseEnter)
                    control.PointerEntered += hoverHandler;
                else
                    control.PointerExited += hoverHandler;

                handler = hoverHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.MouseDown)
            {
                EventHandler<PointerPressedEventArgs> pointerHandler = (_, __) => virtualEvent.Handler.DynamicInvoke();
                control.PointerPressed += pointerHandler;
                handler = pointerHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.MouseUp)
            {
                EventHandler<PointerReleasedEventArgs> pointerHandler = (_, __) => virtualEvent.Handler.DynamicInvoke();
                control.PointerReleased += pointerHandler;
                handler = pointerHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.KeyDown)
            {
                EventHandler<KeyEventArgs> keyHandler = (_, args) => InvokeKeyHandler(virtualEvent, args);
                control.KeyDown += keyHandler;
                handler = keyHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.KeyUp)
            {
                EventHandler<KeyEventArgs> keyHandler = (_, args) => InvokeKeyHandler(virtualEvent, args);
                control.KeyUp += keyHandler;
                handler = keyHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.FocusChanged)
            {
                EventHandler<GotFocusEventArgs> gotFocusHandler = (_, __) => virtualEvent.Handler.DynamicInvoke(true);
                EventHandler<RoutedEventArgs> lostFocusHandler = (_, __) => virtualEvent.Handler.DynamicInvoke(false);

                if (eventName == EventKeys.GotFocus)
                {
                    control.GotFocus += gotFocusHandler;
                    handler = gotFocusHandler;
                }
                else
                {
                    control.LostFocus += lostFocusHandler;
                    handler = lostFocusHandler;
                }

                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.Loaded)
            {
                EventHandler<VisualTreeAttachmentEventArgs> loadedHandler = (_, __) => virtualEvent.Handler.DynamicInvoke();
                control.AttachedToVisualTree += loadedHandler;
                handler = loadedHandler;
                return true;
            }

            if (virtualEvent.Kind == VirtualEventKind.Unloaded)
            {
                EventHandler<VisualTreeAttachmentEventArgs> unloadedHandler = (_, __) => virtualEvent.Handler.DynamicInvoke();
                control.DetachedFromVisualTree += unloadedHandler;
                handler = unloadedHandler;
                return true;
            }

            return false;
        }

        public static void Detach(Control control, string eventName, Delegate handler)
        {
            if (control is Button button && handler is EventHandler<RoutedEventArgs> clickHandler)
            {
                button.Click -= clickHandler;
                return;
            }

            if (control is TextBox textBox && handler is EventHandler<TextChangedEventArgs> textHandler)
            {
                textBox.TextChanged -= textHandler;
                return;
            }

            if (handler is EventHandler<PointerEventArgs> hoverHandler)
            {
                if (eventName == EventKeys.MouseEnter)
                    control.PointerEntered -= hoverHandler;
                else if (eventName == EventKeys.MouseLeave)
                    control.PointerExited -= hoverHandler;

                return;
            }

            if (handler is EventHandler<PointerPressedEventArgs> pressedHandler)
            {
                control.PointerPressed -= pressedHandler;
                return;
            }

            if (handler is EventHandler<PointerReleasedEventArgs> releasedHandler)
            {
                control.PointerReleased -= releasedHandler;
                return;
            }

            if (handler is EventHandler<KeyEventArgs> keyHandler)
            {
                if (eventName == EventKeys.PreviewKeyUp || eventName == EventKeys.KeyUp)
                    control.KeyUp -= keyHandler;
                else
                    control.KeyDown -= keyHandler;

                return;
            }

            if (handler is EventHandler<GotFocusEventArgs> gotFocusHandler)
            {
                control.GotFocus -= gotFocusHandler;
                return;
            }

            if (handler is EventHandler<RoutedEventArgs> lostFocusHandler && eventName == EventKeys.LostFocus)
            {
                control.LostFocus -= lostFocusHandler;
                return;
            }

            if (handler is EventHandler<VisualTreeAttachmentEventArgs> attachmentHandler)
            {
                if (eventName == EventKeys.Loaded)
                    control.AttachedToVisualTree -= attachmentHandler;
                else if (eventName == EventKeys.Unloaded)
                    control.DetachedFromVisualTree -= attachmentHandler;
            }
        }

        public static string GetHandlerKey(string eventName, object? value)
        {
            return value is VirtualEvent virtualEvent
                ? $"{eventName}:{virtualEvent.Kind}:{virtualEvent.Handler.GetHashCode()}"
                : eventName;
        }

        private static void InvokeKeyHandler(VirtualEvent virtualEvent, KeyEventArgs args)
        {
            var key = ToKeyboardKey(args.Key);
            if (key == KeyboardKey.Unknown || args.Handled)
                return;

            virtualEvent.Handler.DynamicInvoke(key);
        }

        private static KeyboardKey ToKeyboardKey(Key key)
        {
            if (key == Key.Up)
                return KeyboardKey.Up;
            if (key == Key.Down)
                return KeyboardKey.Down;
            if (key == Key.Left)
                return KeyboardKey.Left;
            if (key == Key.Right)
                return KeyboardKey.Right;
            if (key == Key.Enter)
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
    }
}
