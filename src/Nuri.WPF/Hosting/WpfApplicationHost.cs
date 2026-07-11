using System;
using System.Windows;
using Nuri.Constants;
using Nuri.Platform.Abstractions;
using Nuri.UI.Dsl;

namespace Nuri.WPF
{
    internal sealed class WpfApplicationHost : IHostAdapter<FrameworkElement>
    {
        private readonly Window _window;

        public WpfApplicationHost(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public void SetContent(FrameworkElement root)
        {
            _window.Content = root;
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
