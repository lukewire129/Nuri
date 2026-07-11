using Nuri.LargeListSample.Components;
using Nuri.WPF;
using System.Windows;

namespace Nuri.LargeListSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/themes/Fluent.Light.xaml") });
        var window = NuriApplication.Show<LargeListComponent>("Nuri Large List", width: 980, height: 720);
        app.MainWindow = window;
        app.Run();
    }
}
