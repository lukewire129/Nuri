using Nuri.VirtualExplorerTreeSample.Components;
using Nuri.WPF;
using System.Windows;

namespace Nuri.VirtualExplorerTreeSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/themes/Fluent.Light.xaml")
        });
        var window = NuriApplication.Show<VirtualExplorerTreeComponent>(
            "Nuri Virtual Explorer Tree",
            width: 1120,
            height: 720);
        app.MainWindow = window;
        app.Run();
    }
}
