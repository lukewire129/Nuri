using Nuri.AnimatedDashboardSample.Components;
using AvaloniaApplication = Nuri.Avalonia.NuriApplication;
using WpfApplication = Nuri.WPF.NuriApplication;

namespace Nuri.AnimatedDashboardSample;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--avalonia", StringComparer.Ordinal))
        {
            AvaloniaApplication.Run<AnimatedDashboardComponent>(
                args,
                "Nuri Animated Dashboard - Avalonia",
                width: 900,
                height: 640);
            return;
        }

        WpfApplication.Run<AnimatedDashboardComponent>(
            "Nuri Animated Dashboard - WPF",
            width: 900,
            height: 640);
    }
}
