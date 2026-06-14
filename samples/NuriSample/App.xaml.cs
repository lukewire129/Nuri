using System.Windows;
using NuriSample.Components;
using Nuri.WPF;

namespace NuriSample
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            NuriApplication.Run<CounterComponent>("MVU Application", width: 400, height: 300);
        }
    }
}
