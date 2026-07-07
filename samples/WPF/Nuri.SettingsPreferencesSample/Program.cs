using Nuri.SettingsPreferencesSample.Components;
using Nuri.WPF;
using System.Windows;

namespace Nuri.SettingsPreferencesSample;

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

        var window = NuriApplication.Show<SettingsPreferencesComponent>("Nuri Settings Preferences", width: 920, height: 680);
        application.MainWindow = window;
        application.Run();
    }
}
