using Duxel.App;
using Duxel.Core;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;

namespace Nuri.Duxel;

internal sealed class NuriDuxelWindowHost : IDisposable
{
    private const float DuxelTitleBarHeight = 48f;

    private readonly FirstFrameWindowVisibilityGate _windowVisibilityGate;
    private readonly WindowsInputEventBridge _inputBridge;
    private readonly NuriDuxelScreen _screen;
    private readonly DuxelThemeController? _themeController;
    private readonly Action<Type[]?> _hotReloadHandler;
    private Action<UiTheme>? _requestTheme;
    private int _disposed;

    public NuriDuxelWindowHost(
        DuxelAppSession session,
        IElement rootElement,
        string title,
        int width,
        int height,
        bool vsync,
        UiTheme? theme,
        DuxelThemeController? themeController,
        bool useDuxelTitleBar,
        bool integrateSystemChrome,
        bool includeInDiagnostics,
        Action<NuriDuxelScreen>? screenCreated,
        Action<IntPtr>? windowCreated,
        Func<float>? contentScaleProvider,
        NuriDuxelPerformanceOptions? performance,
        DebugKey? debugKey,
        Action? debugShortcut,
        IntPtr ownerWindowHandle)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(rootElement);

        var inputEvents = new DuxelInputEventQueue();
        _themeController = themeController;
        _windowVisibilityGate = new FirstFrameWindowVisibilityGate();
        _inputBridge = new WindowsInputEventBridge(
            inputEvents,
            session.RequestFrame,
            contentScaleProvider,
            performance?.ResizeMessageReceived,
            debugKey,
            debugShortcut);

        UiVector2? GetContentAreaSize()
        {
            var size = _inputBridge.ClientAreaSize;
            return useDuxelTitleBar && size is { } clientSize
                ? new UiVector2(clientSize.X, MathF.Max(0f, clientSize.Y - DuxelTitleBarHeight))
                : size;
        }

        Action<UiTheme>? observeTheme = themeController is null
            ? null
            : themeController.ObserveTheme;
        _screen = new NuriDuxelScreen(
            rootElement,
            session.RequestFrame,
            title,
            inputEvents,
            GetContentAreaSize,
            observeTheme,
            includeInDiagnostics,
            contentScaleProvider,
            performance?.FrameCompleted,
            _windowVisibilityGate.Release);
        _hotReloadHandler = _ => _screen.RequestFullRebuild();

        try
        {
            screenCreated?.Invoke(_screen);
            var options = DuxelApp.Options(_screen, title, width, height, vsync);
            options = options with
            {
                Renderer = options.Renderer with
                {
                    Profile = DuxelPerformanceProfile.Render,
                    TextRendering = DuxelTextRenderingMode.DirectText
                }
            };
            if (theme is { } selectedTheme)
            {
                options = options with { Theme = selectedTheme };
            }

            if (performance is not null)
            {
                if (performance.DuxelLogEveryNFrames < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(performance),
                        "DuxelLogEveryNFrames must be zero or greater.");
                }

                var existingLog = options.Debug.Log;
                Action<string>? combinedLog = existingLog is null
                    ? performance.DuxelLog
                    : performance.DuxelLog is null
                        ? existingLog
                        : message =>
                        {
                            existingLog(message);
                            performance.DuxelLog(message);
                        };
                options = options with
                {
                    Debug = options.Debug with
                    {
                        Log = combinedLog,
                        LogStartupTimings = options.Debug.LogStartupTimings
                            || performance.LogDuxelStartupTimings,
                        LogEveryNFrames = performance.DuxelLogEveryNFrames
                    }
                };
            }

            var existingAnimationProvider = options.Frame.IsAnimationActiveProvider;
            var existingWindowCreated = options.Window.WindowCreated;
            Options = options with
            {
                Frame = options.Frame with
                {
                    IsAnimationActiveProvider = () =>
                        _screen.HasActiveAnimations
                        || _screen.HasActiveScrollMotion
                        || inputEvents.HasPending
                        || (existingAnimationProvider?.Invoke() ?? false)
                },
                Window = options.Window with
                {
                    UseDuxelTitleBar = useDuxelTitleBar,
                    DuxelTitleBarHeight = DuxelTitleBarHeight,
                    IntegrateSystemChrome = integrateSystemChrome,
                    OwnerWindowHandle = ownerWindowHandle,
                    WindowCreated = windowHandle =>
                    {
                        existingWindowCreated?.Invoke(windowHandle);
                        _inputBridge.Attach(windowHandle);
                        windowCreated?.Invoke(windowHandle);
                        _windowVisibilityGate.Attach(windowHandle);
                    }
                }
            };

            if (themeController is not null)
            {
                _requestTheme = _screen.RequestTheme;
                themeController.Attach(_requestTheme);
            }

            HotReloadService.UpdateApplicationEvent += _hotReloadHandler;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public DuxelAppOptions Options { get; private set; } = null!;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        HotReloadService.UpdateApplicationEvent -= _hotReloadHandler;
        if (_requestTheme is not null)
            _themeController!.Detach(_requestTheme);

        _screen.Dispose();
        _inputBridge.Dispose();
        _windowVisibilityGate.Dispose();
    }
}
