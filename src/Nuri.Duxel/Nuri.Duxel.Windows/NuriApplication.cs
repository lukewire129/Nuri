using System.Runtime.Versioning;
using Duxel.App;
using Duxel.Core;
using Duxel.Windows.App;
using Nuri.UI.Dsl;

namespace Nuri.Duxel;

[SupportedOSPlatform("windows")]
public static class NuriApplication
{
    public static void Run<TComponent>(
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true,
        UiTheme? theme = null,
        DuxelThemeController? themeController = null,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false)
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
            integrateSystemChrome);
    }

    public static void Run(
        UiTheme theme,
        Func<UiTheme, IElement> rootFactory,
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false)
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
            integrateSystemChrome);
    }

    public static void Run(
        Func<DuxelThemeController, IElement> rootFactory,
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true,
        UiTheme? theme = null,
        bool useDuxelTitleBar = false,
        bool integrateSystemChrome = false)
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
            integrateSystemChrome);
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
        bool integrateSystemChrome = false)
    {
        ArgumentNullException.ThrowIfNull(rootElement);

        const float duxelTitleBarHeight = 48f;
        var session = new DuxelAppSession();
        var inputEvents = new DuxelInputEventQueue();
        using var inputBridge = new WindowsInputEventBridge(inputEvents, session.RequestFrame);
        Action<UiTheme>? observeTheme = themeController is null
            ? null
            : themeController.ObserveTheme;
        UiVector2? GetContentAreaSize()
        {
            var size = inputBridge.ClientAreaSize;
            return useDuxelTitleBar && size is { } clientSize
                ? new UiVector2(clientSize.X, MathF.Max(0f, clientSize.Y - duxelTitleBarHeight))
                : size;
        }

        using var screen = new NuriDuxelScreen(
            rootElement,
            session.RequestFrame,
            title,
            inputEvents,
            GetContentAreaSize,
            observeTheme);
        var options = DuxelApp.Options(screen, title, width, height, vsync);
        if (theme is { } selectedTheme)
        {
            options = options with { Theme = selectedTheme };
        }

        var existingAnimationProvider = options.Frame.IsAnimationActiveProvider;
        var existingWindowCreated = options.Window.WindowCreated;
        options = options with
        {
            Frame = options.Frame with
            {
                IsAnimationActiveProvider = () =>
                    screen.HasActiveAnimations
                    || screen.HasActiveScrollMotion
                    || inputEvents.HasPending
                    || (existingAnimationProvider?.Invoke() ?? false)
            },
            Window = options.Window with
            {
                UseDuxelTitleBar = useDuxelTitleBar,
                DuxelTitleBarHeight = duxelTitleBarHeight,
                IntegrateSystemChrome = integrateSystemChrome,
                WindowCreated = windowHandle =>
                {
                    existingWindowCreated?.Invoke(windowHandle);
                    inputBridge.Attach(windowHandle);
                }
            }
        };
        void OnHotReload(Type[]? _) => screen.RequestFullRebuild();
        Action<UiTheme>? requestTheme = null;

        if (themeController is not null)
        {
            requestTheme = screen.RequestTheme;
            themeController.Attach(requestTheme);
        }

        HotReloadService.UpdateApplicationEvent += OnHotReload;
        try
        {
            DuxelWindowsApp.Run(options, session);
        }
        finally
        {
            HotReloadService.UpdateApplicationEvent -= OnHotReload;
            if (requestTheme is not null)
            {
                themeController!.Detach(requestTheme);
            }
        }
    }
}
