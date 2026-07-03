using System.Windows;
using ContentControlChildrenTest.Components;
using Nuri.WPF;

namespace ContentControlChildrenTest
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            NuriApplication.Run<CounterComponent>("MVU Application", width: 800, height: 600);
        }
    }
}
