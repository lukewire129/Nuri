using Nuri.MasterDetailSample.Components;
using Nuri.WPF;
using System.Windows;

namespace Nuri.MasterDetailSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/themes/Fluent.Light.xaml") });
        var window = NuriApplication.Show<MasterDetailComponent>("Nuri Master Detail", width: 960, height: 680);
        app.MainWindow = window;
        app.Run();
    }
}
