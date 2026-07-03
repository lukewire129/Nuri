using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

            if (virtualEvent.Kind == VirtualEventKind.TextChanged && control is TextBox textBox)
            {
                EventHandler<TextChangedEventArgs> textHandler = (sender, _) => virtualEvent.Handler.DynamicInvoke(((TextBox)sender!).Text ?? string.Empty);
                textBox.TextChanged += textHandler;
                handler = textHandler;
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
                textBox.TextChanged -= textHandler;
        }

        public static string GetHandlerKey(string eventName, object? value)
        {
            return value is VirtualEvent virtualEvent
                ? $"{eventName}:{virtualEvent.Kind}:{virtualEvent.Handler.GetHashCode()}"
                : eventName;
        }
    }
}
