using System;
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
                    if (eventName == "MouseLeftButtonDown")
                    {
                        wpfEventName = "MouseLeftButtonDown";
                        handler = new MouseButtonEventHandler((s, e) => Invoke(virtualEvent.Handler));
                    }
                    else
                    {
                        wpfEventName = "Click";
                        handler = new RoutedEventHandler((s, e) => Invoke(virtualEvent.Handler));
                    }
                    return true;
                case VirtualEventKind.TextChanged:
                    wpfEventName = "TextChanged";
                    handler = new TextChangedEventHandler((s, e) =>
                    {
                        if (s is TextBox textBox)
                            Invoke(virtualEvent.Handler, textBox.Text);
                    });
                    return true;
                case VirtualEventKind.ContentChanged:
                    wpfEventName = "ContentChanged";
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
                    handler = new MouseEventHandler((s, e) => Invoke(virtualEvent.Handler, eventName == "MouseEnter"));
                    return true;
                default:
                    wpfEventName = string.Empty;
                    handler = null!;
                    return false;
            }
        }

        private static void Invoke(Delegate handler, params object[] values)
        {
            handler.DynamicInvoke(values);
        }
    }
}
