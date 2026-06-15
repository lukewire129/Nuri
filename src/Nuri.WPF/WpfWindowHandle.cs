using System.Windows;
using Nuri.Hosting;

namespace Nuri.WPF
{
    internal sealed class WpfWindowHandle : INuriWindowHandle
    {
        private readonly Window _window;

        public WpfWindowHandle(Window window)
        {
            _window = window;
        }

        public void Close()
        {
            _window.Close();
        }
    }
}
