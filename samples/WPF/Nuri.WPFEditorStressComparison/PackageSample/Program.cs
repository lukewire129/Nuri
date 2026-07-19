using Nuri.WPF;
using Nuri.WPFEditorStressComparison;
using System.Windows;

namespace Nuri.WPFEditorStress.PackageSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application();
        var window = NuriApplication.Show<EditorStressComponent>(
            "Nuri WPF Editor Stress - NuGet 0.2.0",
            width: 1120,
            height: 760);
        app.MainWindow = window;
        app.Run();
    }
}
