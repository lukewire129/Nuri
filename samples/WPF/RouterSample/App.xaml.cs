using System.Windows;
using RouterSample.Components;
using Nuri.WPF;

namespace RouterSample
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            NuriApplication.Run<ShowcaseApp>("Nuri WPF Samples", width: 1040, height: 720);
        }
    }
}
