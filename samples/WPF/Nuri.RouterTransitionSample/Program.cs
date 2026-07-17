using Nuri.RouterTransitionSample.Components;
using Nuri.WPF;

namespace Nuri.RouterTransitionSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        NuriApplication.Run<RouterTransitionComponent>(
            "Nuri WPF Router Transition",
            width: 900,
            height: 640);
    }
}
