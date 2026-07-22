using System.Windows;
using Nuri.WPF;
using Nuri.WPF.Diagnostics;
using Nuri.WPFDiagnosticsSample.Components;

namespace Nuri.WPFDiagnosticsSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var application = new Application();

        var app = NuriApplication.Create<DiagnosticsSampleComponent>(
            "Nuri WPF Diagnostics Sample",
            width: 940,
            height: 620);

#if DEBUG
        app.UseAttachDevTools();
#endif

        var appWindow = app.Show();
        application.MainWindow = appWindow;
        application.Run();
    }
}
