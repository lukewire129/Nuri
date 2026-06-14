using System.Windows;
using Nuri.WPF;
using Template.Project.Components;

namespace Template.Project
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
