using Nuri.CommandPaletteSample.Components;
using Nuri.WPF;

namespace Nuri.CommandPaletteSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        NuriApplication.Run<CommandPaletteComponent>("Nuri Command Palette", width: 900, height: 680);
    }
}
