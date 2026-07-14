using Nuri.MultiWindowSample.Components;
using Nuri.WPF;

namespace Nuri.MultiWindowSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        NuriApplication.Run<MultiWindowLauncherComponent>(
            "Nuri Multi-Window",
            width: 620,
            height: 500);
    }
}
