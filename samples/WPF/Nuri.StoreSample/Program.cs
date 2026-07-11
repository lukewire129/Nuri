using Nuri.StoreSample.Components;
using Nuri.WPF;

namespace Nuri.StoreSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        NuriApplication.Run<StoreSampleComponent>("Nuri Store Sample", width: 980, height: 640);
    }
}
