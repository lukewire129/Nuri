using Nuri.AnimatedDashboardSample.Components;
using AvaloniaApplication = Nuri.Avalonia.NuriApplication;
using WpfApplication = Nuri.WPF.NuriApplication;

namespace Nuri.AnimatedDashboardSample;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--avalonia", StringComparer.OrdinalIgnoreCase))
        {
            var avaloniaArgs = args
                .Where(argument => !string.Equals(argument, "--avalonia", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            AvaloniaApplication.Run<AnimatedDashboardComponent>(avaloniaArgs, "Nuri Animated Dashboard", width: 900, height: 640);
            return;
        }

        WpfApplication.Run<AnimatedDashboardComponent>("Nuri Animated Dashboard", width: 900, height: 640);
    }
}
