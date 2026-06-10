using DeltaUI.Core.VirtualDom;
using DeltaUI.Core.UI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeltaUI.WPF
{
    public static class VirtualEntryAdapter
    {
        public static VirtualEntry ToVirtualEntry(this IElement element)
        {
            var properties = element.Properties.Select(property => new KeyValuePair<string, object?>(property.Key, property.Value));
            var animations = element.Animations.Select(animation => new KeyValuePair<string, object?>(animation.Key, animation.Value));
            var children = element.Children.Select(ToVirtualEntry);
            var events = element.Events.Concat(element.VirtualEvents.Select(ToWpfEvent));

            var entry = new VirtualEntry(
                element.Type,
                kind: element.Kind,
                key: GetKey(element),
                properties: properties,
                events: events,
                animations: animations,
                children: children);

            return entry.WithIdentity(string.IsNullOrWhiteSpace(element.Id) ? "0" : element.Id, element.ParentId, rewriteChildren: false);
        }

        private static string? GetKey(IElement element)
        {
            if (!string.IsNullOrWhiteSpace(element.Key))
                return element.Key;

            return string.IsNullOrWhiteSpace(element.Name) ? null : element.Name;
        }

        private static KeyValuePair<string, Delegate> ToWpfEvent(KeyValuePair<string, VirtualEvent> value)
        {
            return value.Value.Kind switch
            {
                VirtualEventKind.Click => new KeyValuePair<string, Delegate>("Click", new RoutedEventHandler((s, e) => Invoke(value.Value.Handler))),
                VirtualEventKind.TextChanged => new KeyValuePair<string, Delegate>("TextChanged", new TextChangedEventHandler((s, e) =>
                {
                    if (s is TextBox textBox)
                        Invoke(value.Value.Handler, textBox.Text);
                })),
                VirtualEventKind.ContentChanged => new KeyValuePair<string, Delegate>("ContentChanged", new RoutedEventHandler((s, e) =>
                {
                    if (s is System.Windows.Controls.ContentControl contentControl)
                        Invoke(value.Value.Handler, contentControl.Content ?? string.Empty);
                })),
                VirtualEventKind.CheckChanged => new KeyValuePair<string, Delegate>(value.Key, new RoutedEventHandler((s, e) =>
                {
                    if (s is CheckBox checkBox)
                        Invoke(value.Value.Handler, checkBox.IsChecked ?? false);
                    else if (s is RadioButton radioButton)
                        Invoke(value.Value.Handler, radioButton.IsChecked ?? false);
                })),
                _ => throw new NotSupportedException($"Unsupported virtual event kind: {value.Value.Kind}")
            };
        }

        private static void Invoke(Delegate handler, params object[] values)
        {
            handler.DynamicInvoke(values);
        }
    }
}
