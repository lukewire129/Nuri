using System.Windows;
using Nuri.DevToolsSample.Components;
using Nuri.WPF;

namespace Nuri.DevToolsSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var application = new Application();

        var appWindow = NuriApplication.Show<DevToolsSampleComponent>("Nuri DevTools Sample", width: 940, height: 620);

        application.MainWindow = appWindow;
        application.Run();
    }
}
