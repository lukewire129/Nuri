using System;
using Avalonia.Controls;
using Nuri.Constants;
using Nuri.Platform.Abstractions;
using Nuri.UI.Dsl;

namespace Nuri.Avalonia
{
    internal sealed class AvaloniaApplicationHost : IHostAdapter<Control>
    {
        private readonly Window _window;

        public AvaloniaApplicationHost(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            ConfigureContentHost(_window, null);
        }

        internal static void ConfigureContentHost(ContentControl host, Control? root)
        {
            host.HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
            host.VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
            if (root == null)
                return;

            root.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
            root.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
            host.Content = root;
        }

        public void SetContent(Control root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            ConfigureContentHost(_window, root);
        }

        public void ApplyWindowProperties(IElement rootElement)
        {
            if (rootElement.Properties.TryGetValue(PropertyKeys.Title, out var title) && title is string titleText)
                _window.Title = titleText;

            if (rootElement.Properties.TryGetValue(PropertyKeys.Width, out var width) && width is not null)
                _window.Width = Convert.ToDouble(width);

            if (rootElement.Properties.TryGetValue(PropertyKeys.Height, out var height) && height is not null)
                _window.Height = Convert.ToDouble(height);
        }
    }
}
