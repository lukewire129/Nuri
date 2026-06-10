using System;
using DeltaUI.Core.UI.Events;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeltaUI.WPF
{
    public static partial class VisualExtention
    {
        public static T OnClick<T>(this T node, MouseButtonEventHandler handlerFactory) where T : IVisual
        {
            node.AddEvent ("MouseLeftButtonDown", handlerFactory);
            return node;
        }
        public static T OnClick<T>(this T node, RoutedEventHandler handlerFactory) where T : IInput
        {
            node.AddEvent ("Click", handlerFactory);
            return node;
        }

        public static T OnClick<T>(this T node, Action handler) where T : IInput
        {
            node.AddVirtualEvent("Click", new VirtualEvent(VirtualEventKind.Click, handler));
            return node;
        }

        public static T OnHover<T>(this T node, MouseEventHandler handlerFactory) where T : IVisual
        {
            node.AddEvent ("MouseEnter", handlerFactory);
            node.AddEvent ("MouseLeave", handlerFactory);

            return node;
        }

        public static IElement OnTextChanged<T>(this T node, Action<string> valueChangedHandler) where T : IInput
        {
            return node.AddVirtualEvent("TextChanged", new VirtualEvent(VirtualEventKind.TextChanged, valueChangedHandler));
        }

        public static IElement OnContentChanged<T>(this T node, Action<object> valueChangedHandler) where T : IContent
        {
            return node.AddVirtualEvent("ContentChanged", new VirtualEvent(VirtualEventKind.ContentChanged, valueChangedHandler));
        }

        public static IElement OnCheckChanged<T>(this T node, Action<bool> valueChangedHandler) where T : IInput
        {
            node.AddVirtualEvent("Checked", new VirtualEvent(VirtualEventKind.CheckChanged, valueChangedHandler));
            return node.AddVirtualEvent("Unchecked", new VirtualEvent(VirtualEventKind.CheckChanged, valueChangedHandler));
        }
    }
}
