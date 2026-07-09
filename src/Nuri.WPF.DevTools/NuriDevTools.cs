using System.Windows;
using System.Windows.Input;
using System;
using Nuri.Runtime.Diagnostics;

namespace Nuri.WPF.DevTools
{
    public static class NuriDevTools
    {
        private static DevToolsWindow? _window;
        private static bool _consoleCaptured;

        public static void Enable()
        {
            NuriDiagnostics.Enable();
            CaptureConsole();
        }

        public static void AttachHotKey(Window window, Key key = Key.F12)
        {
            if (window == null)
                return;

            NuriDiagnostics.Enable();
            CaptureConsole();
            window.PreviewKeyDown += (_, args) =>
            {
                if (args.Key != key)
                    return;

                ShowWindow();
                args.Handled = true;
            };
        }

        public static Window ShowWindow()
        {
            NuriDiagnostics.Enable();
            CaptureConsole();

            if (_window == null || !_window.IsLoaded)
            {
                _window = new DevToolsWindow();
                _window.Closed += (_, __) => _window = null;
                _window.Show();
            }
            else
            {
                _window.Activate();
            }

            return _window;
        }

        private static void CaptureConsole()
        {
            if (_consoleCaptured)
                return;

            Console.SetOut(new DiagnosticsConsoleWriter(Console.Out));
            _consoleCaptured = true;
        }
    }
}
