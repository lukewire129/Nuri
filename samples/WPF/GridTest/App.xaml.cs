using System.Windows;
using Nuri.WPF;
using GridTest.Components;

namespace GridTest
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            NuriApplication.Run<CounterComponent>("MVU Application", width: 400, height: 1000);
        }
    }
}
