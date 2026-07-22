using System.Runtime.Versioning;
using Duxel.Core;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;

namespace Nuri.Duxel;

[SupportedOSPlatform("windows")]
public sealed class NuriApplicationBuilder<TComponent> : INuriDebugHost
    where TComponent : Component, new()
{
    private readonly object _syncRoot = new();
    private readonly string _title;
    private readonly int _width;
    private readonly int _height;
    private readonly bool _vsync;
    private readonly UiTheme? _theme;
    private readonly bool _useDuxelTitleBar;
    private readonly bool _integrateSystemChrome;
    private readonly NuriDuxelPerformanceOptions? _performance;
    private DebugKey _debugKey = DebugKey.F12;
    private Action? _openInspector;
    private NuriDuxelScreen? _screen;
    private bool _started;
    private bool _closed;

    internal NuriApplicationBuilder(
        string title,
        int width,
        int height,
        bool vsync,
        UiTheme? theme,
        bool useDuxelTitleBar,
        bool integrateSystemChrome,
        NuriDuxelPerformanceOptions? performance)
    {
        _title = title;
        _width = width;
        _height = height;
        _vsync = vsync;
        _theme = theme;
        _useDuxelTitleBar = useDuxelTitleBar;
        _integrateSystemChrome = integrateSystemChrome;
        _performance = performance;
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

    public void Run()
    {
        DebugKey? debugKey;
        Action? openInspector;
        lock (_syncRoot)
        {
            if (_started)
                throw new InvalidOperationException("This Nuri application builder has already started.");
            if (_closed)
                throw new ObjectDisposedException(GetType().FullName);

            _started = true;
            debugKey = _openInspector is null ? null : _debugKey;
            openInspector = _openInspector;
        }

        try
        {
            NuriApplication.RunCore(
                new TComponent(),
                _title,
                _width,
                _height,
                _vsync,
                _theme,
                themeController: null,
                _useDuxelTitleBar,
                _integrateSystemChrome,
                includeInDiagnostics: true,
                screenCreated: screen =>
                {
                    lock (_syncRoot)
                        _screen = screen;
                },
                windowCreated: null,
                contentScaleProvider: null,
                _performance,
                debugKey,
                openInspector);
        }
        finally
        {
            lock (_syncRoot)
            {
                _screen = null;
                _openInspector = null;
                _closed = true;
            }
        }
    }

    public void SetDebugShortcut(DebugKey key, Action openInspector)
    {
        ArgumentNullException.ThrowIfNull(openInspector);
        if (key < DebugKey.F1 || key > DebugKey.F12)
            throw new ArgumentOutOfRangeException(nameof(key), key, "DebugKey must be between F1 and F12.");

        lock (_syncRoot)
        {
            if (_closed)
                throw new ObjectDisposedException(GetType().FullName);
            if (_started)
                throw new InvalidOperationException("The Duxel debug shortcut must be configured before Run().");

            _debugKey = key;
            _openInspector = openInspector;
        }
    }

    public RuntimeSnapshot CaptureSnapshot()
    {
        NuriDuxelScreen? screen;
        lock (_syncRoot)
            screen = _screen;

        return screen is null
            ? NuriDiagnostics.GetSnapshot()
            : screen.InvokeOnFrame(NuriDiagnostics.GetSnapshot);
    }

    public void HighlightComponent(string? componentId)
    {
        _ = componentId;
    }
}
