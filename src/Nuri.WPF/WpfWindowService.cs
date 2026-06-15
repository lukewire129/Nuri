using System;
using System.Threading.Tasks;
using System.Windows;
using Nuri.Hosting;
using Nuri.Runtime;
using Nuri.UI.Dsl;

namespace Nuri.WPF
{
    public sealed class WpfWindowService : IWindowService
    {
        private readonly NuriServiceProvider _services;

        public WpfWindowService(NuriServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public INuriWindowHandle Show<TComponent>(NuriWindowOptions? options = null)
            where TComponent : Component, new()
        {
            return Show(new TComponent(), options);
        }

        public INuriWindowHandle Show(IElement rootElement, NuriWindowOptions? options = null)
        {
            var window = CreateWindow(options);
            NuriApplication.Attach(window, rootElement, _services);
            window.Show();
            return new WpfWindowHandle(window);
        }

        public bool? ShowDialog<TComponent>(NuriWindowOptions? options = null)
            where TComponent : Component, new()
        {
            return ShowDialog(new TComponent(), options);
        }

        public bool? ShowDialog(IElement rootElement, NuriWindowOptions? options = null)
        {
            var window = CreateWindow(options);
            NuriApplication.Attach(window, rootElement, CreateDialogServices(window));
            return window.ShowDialog();
        }

        public Task<bool?> ShowDialogAsync<TComponent>(NuriWindowOptions? options = null)
            where TComponent : Component, new()
        {
            return ShowDialogAsync(new TComponent(), options);
        }

        public Task<bool?> ShowDialogAsync(IElement rootElement, NuriWindowOptions? options = null)
        {
            return Task.FromResult(ShowDialog(rootElement, options));
        }

        private static Window CreateWindow(NuriWindowOptions? options)
        {
            var window = new Window();
            ApplyOptions(window, options);
            return window;
        }

        private static void ApplyOptions(Window window, NuriWindowOptions? options)
        {
            if (options == null)
                return;

            if (!string.IsNullOrWhiteSpace(options.Title))
                window.Title = options.Title!;

            if (options.Width.HasValue)
                window.Width = options.Width.Value;

            if (options.Height.HasValue)
                window.Height = options.Height.Value;
        }

        private NuriServiceProvider CreateDialogServices(Window window)
        {
            var services = new NuriServiceCollection();
            services.UseFallback(_services);
            services.AddNuriWpfServices();
            services.AddSingleton<IDialogContext>(new WpfDialogContext(window));
            return services.BuildServiceProvider();
        }
    }
}
