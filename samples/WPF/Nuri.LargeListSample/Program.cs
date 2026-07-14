using Nuri.LargeListSample.Components;
using Nuri.Runtime.Diagnostics;
using Nuri.WPF;
using System.Windows;

namespace Nuri.LargeListSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        NuriDiagnostics.Enable();
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/themes/Fluent.Light.xaml") });
        var window = NuriApplication.Show<LargeListComponent>("Nuri Large List", width: 980, height: 720);
        app.MainWindow = window;
        app.Run();
    }
}
