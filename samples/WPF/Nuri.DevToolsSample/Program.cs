using System.Windows;
using Nuri.DevTools;
using Nuri.DevToolsSample.Components;
using Nuri.WPF;

namespace Nuri.DevToolsSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var application = new Application();

        var app = NuriApplication.Create<DevToolsSampleComponent>(
            "Nuri WPF DevTools Sample",
            width: 940,
            height: 620);

#if DEBUG
        app.UseDebug();
#endif

        var appWindow = app.Show();
        application.MainWindow = appWindow;
        application.Run();
    }
}
