using Nuri.AsyncLoadingSample.Components;
using Nuri.WPF;
using System.Windows;

namespace Nuri.AsyncLoadingSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/themes/Fluent.Light.xaml") });
        var window = NuriApplication.Show<AsyncLoadingComponent>("Nuri Async Loading", width: 900, height: 640);
        app.MainWindow = window;
        app.Run();
    }
}
