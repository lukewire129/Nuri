using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Nuri.Diagnostics.Internal;
using Nuri.Runtime.Diagnostics;

namespace Nuri.Avalonia.Diagnostics;

public static class AvaloniaDevTools
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
            ?? throw new InvalidOperationException("An Avalonia Application must be running before opening Nuri DevTools.");

        return Dispatcher.UIThread.CheckAccess()
            ? OpenOnDispatcher(application, highlightRequested, snapshotProvider, title, width, height)
            : Dispatcher.UIThread.InvokeAsync(() =>
                OpenOnDispatcher(application, highlightRequested, snapshotProvider, title, width, height))
                .GetAwaiter()
                .GetResult();
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
            if (_inspectorWindow != null)
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

        var component = new RuntimeInspectorComponent(
            highlightRequested,
            snapshotProvider,
            useVirtualizedLists: false);
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

        var owner = (application.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner != null && !ReferenceEquals(owner, window))
            window.Show(owner);
        else
            window.Show();

        return true;
    }
}
