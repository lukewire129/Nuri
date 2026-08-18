using Nuri.UI.Styles;
using Nuri.WPF;
using System.IO;
using System.Windows;

namespace Nuri.YamlStyleSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        StyleManager.Configure(new StyleConfiguration()
            .AddEmbeddedResource(typeof(Program).Assembly, "Nuri.YamlStyleSample.styles.embedded-default.yml")
            .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "default.yml"))
            .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "theme.yml"))
            .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "override.yml")));

        var application = new Application();
        application.MainWindow = NuriApplication.Show<YamlStyleComponent>("Nuri YAML Style", width: 560, height: 360);
        application.Run();
    }
}
