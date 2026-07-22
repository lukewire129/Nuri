using System.Runtime.Versioning;
using Duxel.Core;
using Nuri.Diagnostics.Internal;
using Nuri.Runtime.Diagnostics;

namespace Nuri.Duxel.Diagnostics;

[SupportedOSPlatform("windows")]
public static class DuxelDevTools
{
    private static readonly object SyncRoot = new();
    private static bool _windowRunning;

    public static bool OpenInspector(
        Action<string?>? highlightRequested = null,
        Func<RuntimeSnapshot>? snapshotProvider = null,
        string title = "Nuri Runtime DevTools",
        int width = 1180,
        int height = 760)
    {
        DevToolsRuntime.Enable();
        lock (SyncRoot)
        {
            if (_windowRunning)
                return false;
            _windowRunning = true;
        }

        var thread = new Thread(() =>
        {
            try
            {
                RunCore(highlightRequested, snapshotProvider, title, width, height);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Nuri Duxel DevTools failed: {exception}");
            }
            finally
            {
                highlightRequested?.Invoke(null);
                lock (SyncRoot)
                    _windowRunning = false;
            }
        })
        {
            IsBackground = true,
            Name = "Nuri Duxel DevTools"
        };
        thread.Start();
        return true;
    }

    public static void RunInspector(
        Action<string?>? highlightRequested = null,
        Func<RuntimeSnapshot>? snapshotProvider = null,
        string title = "Nuri Runtime DevTools",
        int width = 1180,
        int height = 760)
    {
        DevToolsRuntime.Enable();
        lock (SyncRoot)
        {
            if (_windowRunning)
                throw new InvalidOperationException("The Nuri Duxel DevTools window is already running.");
            _windowRunning = true;
        }

        try
        {
            RunCore(highlightRequested, snapshotProvider, title, width, height);
        }
        finally
        {
            highlightRequested?.Invoke(null);
            lock (SyncRoot)
                _windowRunning = false;
        }
    }

    private static void RunCore(
        Action<string?>? highlightRequested,
        Func<RuntimeSnapshot>? snapshotProvider,
        string title,
        int width,
        int height)
    {
        NuriDuxelScreen? inspectorScreen = null;
        void OnDiagnosticsChanged(object? _, EventArgs __) => inspectorScreen?.RequestFullRebuild();

        NuriDiagnostics.Changed += OnDiagnosticsChanged;
        try
        {
            NuriApplication.Run(
                new RuntimeInspectorComponent(highlightRequested, snapshotProvider),
                title,
                width,
                height,
                theme: UiTheme.ImGuiLight,
                includeInDiagnostics: false,
                screenCreated: screen => inspectorScreen = screen);
        }
        finally
        {
            NuriDiagnostics.Changed -= OnDiagnosticsChanged;
        }
    }
}
