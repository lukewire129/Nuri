using Nuri.Avalonia;
using Nuri.AvaloniaHotReloadSample.Components;

namespace Nuri.AvaloniaHotReloadSample;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        NuriApplication.Run<HotReloadProbeComponent>(args, "Nuri Avalonia Hot Reload", width: 760, height: 520);
    }
}
