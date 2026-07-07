using Nuri.TodoValidationSample.Components;
using Nuri.WPF;
using System.Windows;

namespace Nuri.TodoValidationSample;

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

        var window = NuriApplication.Show<TodoValidationComponent>("Nuri Todo Validation", width: 900, height: 640);
        application.MainWindow = window;
        application.Run();
    }
}
