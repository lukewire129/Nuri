using System.Runtime.Versioning;
using Duxel.App;
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
        bool vsync = true)
        where TComponent : Component, new()
    {
        Run(new TComponent(), title, width, height, vsync);
    }

    public static void Run(
        IElement rootElement,
        string title = "Nuri Duxel",
        int width = 1280,
        int height = 720,
        bool vsync = true)
    {
        ArgumentNullException.ThrowIfNull(rootElement);

        var session = new DuxelAppSession();
        using var screen = new NuriDuxelScreen(rootElement, session.RequestFrame, title);
        DuxelWindowsApp.Run(screen, title, width, height, vsync);
    }
}
