using Nuri.ModalDialogSample.Components;
using Nuri.WPF;
using System.Windows;

namespace Nuri.ModalDialogSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var application = new Application();
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/themes/Fluent.Light.xaml")
        });

        var window = NuriApplication.Show<ModalDialogComponent>("Nuri Modal Dialog", width: 920, height: 680);
        application.MainWindow = window;
        application.Run();
    }
}
