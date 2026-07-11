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
                case VirtualEventKind.MouseDown:
                    wpfEventName = EventKeys.MouseLeftButtonDown;
                    handler = new MouseButtonEventHandler((s, e) => Invoke(virtualEvent.Handler));
                    return true;
                case VirtualEventKind.MouseUp:
                    wpfEventName = EventKeys.MouseLeftButtonUp;
                    handler = new MouseButtonEventHandler((s, e) => Invoke(virtualEvent.Handler));
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

        private static void Invoke(Delegate handler, params object[] values)
        {
            handler.DynamicInvoke(values);
        }
    }
}
