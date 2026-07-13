using Nuri.WPF;
using Nuri.WPFAnimatedDashboardSample.Components;

namespace Nuri.WPFAnimatedDashboardSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        NuriApplication.Run<AnimatedDashboardComponent>("Nuri WPF Animated Dashboard", width: 920, height: 700);
    }
}
