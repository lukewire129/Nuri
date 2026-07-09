using System.Windows;
using Nuri.DevToolsSample.Components;
using Nuri.WPF;
using Nuri.WPF.DevTools;

namespace Nuri.DevToolsSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var application = new Application();
        NuriDevTools.Enable();

        var appWindow = NuriApplication.Show<DevToolsSampleComponent>("Nuri DevTools Sample", width: 940, height: 620);
        NuriDevTools.AttachHotKey(appWindow);

        application.MainWindow = appWindow;
        application.Run();
    }
}
