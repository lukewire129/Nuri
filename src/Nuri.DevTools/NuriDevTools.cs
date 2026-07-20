using System.Runtime.Versioning;
using Duxel.Core;
using Nuri.Duxel;
using Nuri.Runtime.Diagnostics;

namespace Nuri.DevTools;

[SupportedOSPlatform("windows")]
public static class NuriDevTools
{
    private static readonly object SyncRoot = new();
    private static bool _consoleCaptured;
    private static bool _windowRunning;

    public static void Enable()
    {
        NuriDiagnostics.Enable();
        CaptureConsole();
    }

    public static bool OpenInspector(
        Action<string?>? highlightRequested = null,
        Func<RuntimeSnapshot>? snapshotProvider = null,
        string title = "Nuri Runtime DevTools",
        int width = 1180,
        int height = 760)
    {
        Enable();

        lock (SyncRoot)
        {
            if (_windowRunning)
            {
                return false;
            }

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
                Console.Error.WriteLine($"Nuri DevTools failed: {exception}");
            }
            finally
            {
                highlightRequested?.Invoke(null);
                lock (SyncRoot)
                {
                    _windowRunning = false;
                }
            }
        })
        {
            IsBackground = true,
            Name = "Nuri DevTools"
        };
        thread.Start();
        return true;
    }

    [Obsolete("Use OpenInspector(...) so the DevTools window is distinct from application window creation.")]
    public static bool Show(
        Action<string?>? highlightRequested = null,
        Func<RuntimeSnapshot>? snapshotProvider = null,
        string title = "Nuri Runtime DevTools",
        int width = 1180,
        int height = 760)
    {
        return OpenInspector(highlightRequested, snapshotProvider, title, width, height);
    }

    public static void RunInspector(
        Action<string?>? highlightRequested = null,
        Func<RuntimeSnapshot>? snapshotProvider = null,
        string title = "Nuri Runtime DevTools",
        int width = 1180,
        int height = 760)
    {
        Enable();

        lock (SyncRoot)
        {
            if (_windowRunning)
            {
                throw new InvalidOperationException("The Nuri DevTools window is already running.");
            }

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
            {
                _windowRunning = false;
            }
        }
    }

    [Obsolete("Use RunInspector(...) so the DevTools window is distinct from application window creation.")]
    public static void Run(
        Action<string?>? highlightRequested = null,
        Func<RuntimeSnapshot>? snapshotProvider = null,
        string title = "Nuri Runtime DevTools",
        int width = 1180,
        int height = 760)
    {
        RunInspector(highlightRequested, snapshotProvider, title, width, height);
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

    private static void CaptureConsole()
    {
        lock (SyncRoot)
        {
            if (_consoleCaptured)
            {
                return;
            }

            Console.SetOut(new DiagnosticsConsoleWriter(Console.Out));
            _consoleCaptured = true;
        }
    }

}
