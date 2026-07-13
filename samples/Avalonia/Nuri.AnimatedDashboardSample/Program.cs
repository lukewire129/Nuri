using Nuri.AnimatedDashboardSample.Components;
using Nuri.Avalonia;

namespace Nuri.AnimatedDashboardSample;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        NuriApplication.Run<AnimatedDashboardComponent>(args, "Nuri Animated Dashboard", width: 900, height: 640);
        return;
    }
}
