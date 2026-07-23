using System.Runtime.Versioning;
using Duxel.App;
using Duxel.Core;
using Duxel.Windows.App;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;

namespace Nuri.Duxel;

[SupportedOSPlatform("windows")]
public static class NuriApplication
{
    public static NuriApplicationBuilder<TComponent> Create<TComponent>(
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true,
        UiTheme? theme = null,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false,
        NuriDuxelPerformanceOptions? performance = null)
        where TComponent : Component, new()
    {
        return new NuriApplicationBuilder<TComponent>(
            title,
            width,
            height,
            vsync,
            theme,
            useDuxelTitleBar,
            integrateSystemChrome,
            performance);
    }

    public static void Run<TComponent>(
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true,
        UiTheme? theme = null,
        DuxelThemeController? themeController = null,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false,
        bool includeInDiagnostics = true,
        Action<NuriDuxelScreen>? screenCreated = null,
        Action<IntPtr>? windowCreated = null,
        Func<float>? contentScaleProvider = null,
        NuriDuxelPerformanceOptions? performance = null)
        where TComponent : Component, new()
    {
        Run(
            new TComponent(),
            title,
            width,
            height,
            vsync,
            theme,
            themeController,
            useDuxelTitleBar,
            integrateSystemChrome,
            includeInDiagnostics,
            screenCreated,
            windowCreated,
            contentScaleProvider,
            performance);
    }

    public static void Run(
        UiTheme theme,
        Func<UiTheme, IElement> rootFactory,
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false,
        NuriDuxelPerformanceOptions? performance = null)
    {
        ArgumentNullException.ThrowIfNull(rootFactory);

        Run(
            rootFactory(theme),
            title,
            width,
            height,
            vsync,
            theme,
            themeController: null,
            useDuxelTitleBar,
            integrateSystemChrome,
            performance: performance);
    }

    public static void Run(
        Func<DuxelThemeController, IElement> rootFactory,
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true,
        UiTheme? theme = null,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false,
        NuriDuxelPerformanceOptions? performance = null)
    {
        ArgumentNullException.ThrowIfNull(rootFactory);

        var themeController = new DuxelThemeController();
        Run(
            rootFactory(themeController),
            title,
            width,
            height,
            vsync,
            theme,
            themeController,
            useDuxelTitleBar,
            integrateSystemChrome,
            performance: performance);
    }

    public static void Run(
        IElement rootElement,
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true,
        UiTheme? theme = null,
        DuxelThemeController? themeController = null,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false,
        bool includeInDiagnostics = true,
        Action<NuriDuxelScreen>? screenCreated = null,
        Action<IntPtr>? windowCreated = null,
        Func<float>? contentScaleProvider = null,
        NuriDuxelPerformanceOptions? performance = null)
    {
        RunCore(
            rootElement,
            title,
            width,
            height,
            vsync,
            theme,
            themeController,
            useDuxelTitleBar,
            integrateSystemChrome,
            includeInDiagnostics,
            screenCreated,
            windowCreated,
            contentScaleProvider,
            performance,
            debugKey: null,
            debugShortcut: null);
    }

    public static DuxelModelessWindow ShowModeless(
        Func<Action, IElement> rootFactory,
        string title = "Nuri Duxel",
        int width = 800,
        int height = 600,
        bool vsync = true,
        UiTheme? theme = null,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false,
        bool includeInDiagnostics = true,
        IntPtr ownerWindowHandle = default,
        Action? closed = null)
    {
        ArgumentNullException.ThrowIfNull(rootFactory);

        NuriDuxelWindowHost? host = null;
        try
        {
            return DuxelWindowsApp.ShowModeless(
                session =>
                {
                    var rootElement = rootFactory(session.Exit);
                    host = new NuriDuxelWindowHost(
                        session,
                        rootElement,
                        title,
                        width,
                        height,
                        vsync,
                        theme,
                        themeController: null,
                        useDuxelTitleBar,
                        integrateSystemChrome,
                        includeInDiagnostics,
                        screenCreated: null,
                        windowCreated: null,
                        contentScaleProvider: null,
                        performance: null,
                        debugKey: null,
                        debugShortcut: null,
                        ownerWindowHandle);
                    return host.Options;
                },
                () =>
                {
                    try
                    {
                        host?.Dispose();
                    }
                    finally
                    {
                        closed?.Invoke();
                    }
                });
        }
        catch
        {
            host?.Dispose();
            throw;
        }
    }

    internal static void RunCore(
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
        Action? debugShortcut)
    {
        ArgumentNullException.ThrowIfNull(rootElement);

        var session = new DuxelAppSession();
        using var host = new NuriDuxelWindowHost(
            session,
            rootElement,
            title,
            width,
            height,
            vsync,
            theme,
            themeController,
            useDuxelTitleBar,
            integrateSystemChrome,
            includeInDiagnostics,
            screenCreated,
            windowCreated,
            contentScaleProvider,
            performance,
            debugKey,
            debugShortcut,
            ownerWindowHandle: default);
        DuxelWindowsApp.Run(host.Options, session);
    }
}
