using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Nuri.Navigation;
using Nuri.WPF;
using NuriFlowSample.Components;
using NuriFlowSample.Services;

namespace NuriFlowSample
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var appServices = new SampleServiceProvider ()
                .Add<IAppInfoService> (new AppInfoService ("Existing MVVM service"));

            var host = new ContentControl();
            var window = new Window
            {
                Title = "Nuri Flow Sample",
                Width = 900,
                Height = 620,
                Content = host
            };

            NuriApplication.Attach(host, new AppShell(), appServices, services =>
            {
                services.AddSingleton<IRouter>(NuriRouter.Create<HomePage>());
            });

            MainWindow = window;
            window.Show();
        }
    }

    internal sealed class SampleServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public SampleServiceProvider Add<TService>(TService service) where TService : class
        {
            _services[typeof(TService)] = service;
            return this;
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }
}
