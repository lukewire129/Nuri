using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Threading;
using Nuri.Diagnostics.Internal;
using Nuri.Runtime.Diagnostics;

namespace Nuri.WPF.Diagnostics;

[SupportedOSPlatform("windows")]
public static class WpfDevTools
{
    private static readonly object SyncRoot = new();
    private static Window? _inspectorWindow;

    public static bool OpenInspector(
        Action<string?>? highlightRequested = null,
        Func<RuntimeSnapshot>? snapshotProvider = null,
        string title = "Nuri Runtime DevTools",
        double width = 1180,
        double height = 760)
    {
        DevToolsRuntime.Enable();
        var application = Application.Current
            ?? throw new InvalidOperationException("A WPF Application must be running before opening Nuri DevTools.");

        return application.Dispatcher.CheckAccess()
            ? OpenOnDispatcher(application, highlightRequested, snapshotProvider, title, width, height)
            : application.Dispatcher.Invoke(() =>
                OpenOnDispatcher(application, highlightRequested, snapshotProvider, title, width, height));
    }

    private static bool OpenOnDispatcher(
        Application application,
        Action<string?>? highlightRequested,
        Func<RuntimeSnapshot>? snapshotProvider,
        string title,
        double width,
        double height)
    {
        lock (SyncRoot)
        {
            if (_inspectorWindow is { IsLoaded: true })
            {
                _inspectorWindow.Activate();
                return false;
            }
        }

        var window = new Window
        {
            Title = title,
            Width = width,
            Height = height,
            MinWidth = 720,
            MinHeight = 480
        };
        if (application.MainWindow is { IsLoaded: true } owner && !ReferenceEquals(owner, window))
            window.Owner = owner;

        var component = new RuntimeInspectorComponent(highlightRequested, snapshotProvider);
        var root = NuriApplication.Attach(window, component, includeInDiagnostics: false);
        void OnDiagnosticsChanged(object? _, EventArgs __) => root.DispatchRebuild();
        NuriDiagnostics.Changed += OnDiagnosticsChanged;

        window.Closed += (_, __) =>
        {
            NuriDiagnostics.Changed -= OnDiagnosticsChanged;
            highlightRequested?.Invoke(null);
            lock (SyncRoot)
            {
                if (ReferenceEquals(_inspectorWindow, window))
                    _inspectorWindow = null;
            }
        };

        lock (SyncRoot)
            _inspectorWindow = window;
        window.Show();
        return true;
    }
}
