using Nuri.WPF;
using Nuri.WorkflowSample.Components;
using System.Windows;

namespace Nuri.WorkflowSample;

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

        var window = NuriApplication.Show<WorkflowComponent>(
            "Nuri Workflow",
            width: 1200,
            height: 680);
        app.MainWindow = window;
        app.Run();
    }
}
