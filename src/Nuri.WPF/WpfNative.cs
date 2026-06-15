using System;
using System.Windows;
using Nuri.UI.Dsl;

namespace Nuri.WPF
{
    public static class WpfNative
    {
        public static NativeView Control(Func<FrameworkElement> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return new NativeView(factory);
        }

        public static NativeView Control<TControl>()
            where TControl : FrameworkElement, new()
        {
            return Control(() => new TControl());
        }
    }
}
