using System.Windows;
using Nuri.Hosting;

namespace Nuri.WPF
{
    internal sealed class WpfDialogContext : IDialogContext
    {
        private readonly Window _window;

        public WpfDialogContext(Window window)
        {
            _window = window;
        }

        public void Close(bool? result = null)
        {
            _window.DialogResult = result;
            _window.Close();
        }
    }
}
