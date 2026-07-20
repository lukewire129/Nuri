using System;
using System.Windows;
using System.Windows.Input;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;
using Nuri.WPF.Diagnostics;

namespace Nuri.WPF
{
    public sealed class NuriApplicationBuilder<TComponent> : INuriDebugHost
        where TComponent : Component, new()
    {
        private readonly object _syncRoot = new object();
        private readonly string _title;
        private readonly double _width;
        private readonly double _height;
        private Window? _window;
        private DebugKey _debugKey = DebugKey.F12;
        private Action? _openInspector;
        private bool _started;
        private bool _closed;

        internal NuriApplicationBuilder(string title, double width, double height)
        {
            _title = title;
            _width = width;
            _height = height;
        }

        public bool HasStarted
        {
            get
            {
                lock (_syncRoot)
                    return _started;
            }
        }

        public bool IsClosed
        {
            get
            {
                lock (_syncRoot)
                    return _closed;
            }
        }

        public Window Show()
        {
            var window = CreateWindow();
            window.Show();
            return window;
        }

        public void Run()
        {
            NuriApplication.RunOnApplicationThread(RunCore);
        }

        public void SetDebugShortcut(DebugKey key, Action openInspector)
        {
            if (key < DebugKey.F1 || key > DebugKey.F12)
                throw new ArgumentOutOfRangeException(nameof(key), key, "DebugKey must be between F1 and F12.");

            if (openInspector == null)
                throw new ArgumentNullException(nameof(openInspector));

            lock (_syncRoot)
            {
                if (_closed)
                    throw new ObjectDisposedException(GetType().FullName);

                _debugKey = key;
                _openInspector = openInspector;
            }
        }

        public RuntimeSnapshot CaptureSnapshot()
        {
            Window? window;
            bool closed;
            lock (_syncRoot)
            {
                window = _window;
                closed = _closed;
            }

            if (window == null || closed || window.Dispatcher.HasShutdownStarted)
                return NuriDiagnostics.GetSnapshot();

            return window.Dispatcher.CheckAccess()
                ? NuriDiagnostics.GetSnapshot()
                : window.Dispatcher.Invoke(NuriDiagnostics.GetSnapshot);
        }

        public void HighlightComponent(string? componentId)
        {
            Window? window;
            bool closed;
            lock (_syncRoot)
            {
                window = _window;
                closed = _closed;
            }

            if (window == null || closed || window.Dispatcher.HasShutdownStarted)
                return;

            void ApplyHighlight()
            {
                if (componentId == null)
                    WpfElementHighlighter.Clear();
                else
                    WpfElementHighlighter.Highlight(componentId);
            }

            if (window.Dispatcher.CheckAccess())
                ApplyHighlight();
            else
                window.Dispatcher.BeginInvoke(new Action(ApplyHighlight));
        }

        private void RunCore()
        {
            var application = Application.Current;
            if (application == null)
            {
                application = new Application
                {
                    ShutdownMode = ShutdownMode.OnMainWindowClose
                };
                application.Run(CreateWindow());
                return;
            }

            application.ShutdownMode = ShutdownMode.OnMainWindowClose;
            var window = CreateWindow();
            window.Show();
            application.MainWindow ??= window;
        }

        private Window CreateWindow()
        {
            lock (_syncRoot)
            {
                if (_started)
                    throw new InvalidOperationException("This Nuri application builder has already started.");

                if (_closed)
                    throw new ObjectDisposedException(GetType().FullName);

                _started = true;
            }

            var window = new Window();
            lock (_syncRoot)
                _window = window;

            window.PreviewKeyDown += OnPreviewKeyDown;
            try
            {
                NuriApplication.Attach<TComponent>(window, _title, _width, _height);
            }
            catch
            {
                window.PreviewKeyDown -= OnPreviewKeyDown;
                lock (_syncRoot)
                    _closed = true;
                throw;
            }

            window.Closed += OnWindowClosed;
            return window;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            if (args.IsRepeat)
                return;

            DebugKey debugKey;
            Action? openInspector;
            lock (_syncRoot)
            {
                if (_closed)
                    return;

                debugKey = _debugKey;
                openInspector = _openInspector;
            }

            if (openInspector == null)
                return;

            var pressedKey = args.Key == Key.System ? args.SystemKey : args.Key;
            if (pressedKey != ToWpfKey(debugKey))
                return;

            args.Handled = true;
            openInspector();
        }

        private void OnWindowClosed(object? sender, EventArgs args)
        {
            if (sender is Window window)
                window.PreviewKeyDown -= OnPreviewKeyDown;

            lock (_syncRoot)
            {
                _closed = true;
                _openInspector = null;
            }

            WpfElementHighlighter.Clear();
        }

        private static Key ToWpfKey(DebugKey key)
        {
            return key switch
            {
                DebugKey.F1 => Key.F1,
                DebugKey.F2 => Key.F2,
                DebugKey.F3 => Key.F3,
                DebugKey.F4 => Key.F4,
                DebugKey.F5 => Key.F5,
                DebugKey.F6 => Key.F6,
                DebugKey.F7 => Key.F7,
                DebugKey.F8 => Key.F8,
                DebugKey.F9 => Key.F9,
                DebugKey.F10 => Key.F10,
                DebugKey.F11 => Key.F11,
                DebugKey.F12 => Key.F12,
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, "DebugKey must be between F1 and F12.")
            };
        }
    }
}
