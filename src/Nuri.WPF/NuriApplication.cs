using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Nuri.Runtime;
using Nuri.UI;
using Nuri.UI.Dsl;

namespace Nuri.WPF
{
    public static class NuriApplication
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<ApplicationRoot> Roots = new List<ApplicationRoot>();
        private static bool _hotReloadAttached;

        public static void Run<TComponent>(string title, double width = 800, double height = 600)
            where TComponent : Component, new()
        {
            EnsureHotReloadAttached();

            var application = Application.Current;
            if (application == null)
            {
                application = new Application();
                application.Run(CreateWindow<TComponent>(title, width, height));
                return;
            }

            var window = Show<TComponent>(title, width, height);
            application.MainWindow ??= window;
        }

        public static Window Show<TComponent>(string title, double width = 800, double height = 600)
            where TComponent : Component, new()
        {
            var window = new Window();
            Attach<TComponent>(window, title, width, height);
            window.Show();
            return window;
        }

        public static ApplicationRoot Attach<TComponent>(Window window, string title, double width = 800, double height = 600)
            where TComponent : Component, new()
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            return Attach(window, CreateRoot<TComponent>(title, width, height));
        }

        public static ApplicationRoot Attach(Window window, IElement rootElement, NuriServiceProvider? services = null)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            if (rootElement == null)
                throw new ArgumentNullException(nameof(rootElement));

            EnsureHotReloadAttached();

            var root = ApplicationRoot.Initialize(rootElement, window, services ?? CreateDefaultServices());
            Register(root);
            window.Closed += (_, __) =>
            {
                Unregister(root);
                root.Dispose();
            };

            return root;
        }

        public static ApplicationRoot Attach(Window window, IElement rootElement, Action<NuriServiceCollection> configureServices)
        {
            return Attach(window, rootElement, BuildServices(configureServices));
        }

        public static ApplicationRoot Attach(Window window, IElement rootElement, IServiceProvider serviceProvider)
        {
            return Attach(window, rootElement, BuildServices(serviceProvider, null));
        }

        public static ApplicationRoot Attach(Window window, IElement rootElement, IServiceProvider serviceProvider, Action<NuriServiceCollection> configureServices)
        {
            return Attach(window, rootElement, BuildServices(serviceProvider, configureServices));
        }

        public static ApplicationRoot Attach(ContentControl host, IElement rootElement, NuriServiceProvider? services = null)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            if (rootElement == null)
                throw new ArgumentNullException(nameof(rootElement));

            EnsureHotReloadAttached();

            var root = ApplicationRoot.Initialize(rootElement, host, services ?? CreateDefaultServices());
            Register(root);
            host.Unloaded += (_, __) =>
            {
                Unregister(root);
                root.Dispose();
            };

            return root;
        }

        public static ApplicationRoot Attach(ContentControl host, IElement rootElement, Action<NuriServiceCollection> configureServices)
        {
            return Attach(host, rootElement, BuildServices(configureServices));
        }

        public static ApplicationRoot Attach(ContentControl host, IElement rootElement, IServiceProvider serviceProvider)
        {
            return Attach(host, rootElement, BuildServices(serviceProvider, null));
        }

        public static ApplicationRoot Attach(ContentControl host, IElement rootElement, IServiceProvider serviceProvider, Action<NuriServiceCollection> configureServices)
        {
            return Attach(host, rootElement, BuildServices(serviceProvider, configureServices));
        }

        public static void Configure()
        {
            EnsureHotReloadAttached();
        }

        internal static void Register(ApplicationRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            EnsureHotReloadAttached();

            lock (SyncRoot)
            {
                if (!Roots.Contains(root))
                    Roots.Add(root);
            }
        }

        internal static void Unregister(ApplicationRoot root)
        {
            lock (SyncRoot)
            {
                Roots.Remove(root);
            }
        }

        private static void EnsureHotReloadAttached()
        {
            lock (SyncRoot)
            {
                if (_hotReloadAttached)
                    return;

                HotReloadService.UpdateApplicationEvent += OnHotReload;
                Component.AnyStateChanged += OnAnyComponentStateChanged;
                _hotReloadAttached = true;
            }
        }

        private static void OnHotReload(Type[]? _)
        {
            ApplicationRoot[] roots;
            lock (SyncRoot)
            {
                roots = Roots.ToArray();
            }

            foreach (var root in roots)
                root.DispatchRebuild();
        }

        private static void OnAnyComponentStateChanged(object? sender, Component component)
        {
            ApplicationRoot[] roots;
            lock (SyncRoot)
            {
                roots = Roots.ToArray();
            }

            foreach (var root in roots)
                root.ScheduleComponentRebuild(component);
        }

        private static Window CreateWindow<TComponent>(string title, double width, double height)
            where TComponent : Component, new()
        {
            var window = new Window();
            Attach<TComponent>(window, title, width, height);
            return window;
        }

        private static WindowView CreateRoot<TComponent>(string title, double width, double height)
            where TComponent : Component, new()
        {
            return new WindowView(new TComponent())
                .WithTitle(title)
                .WithSize(width, height);
        }

        private static NuriServiceProvider BuildServices(Action<NuriServiceCollection> configureServices)
        {
            if (configureServices == null)
                throw new ArgumentNullException(nameof(configureServices));

            var services = new NuriServiceCollection();
            services.AddNuriWpfServices();
            configureServices(services);
            return services.BuildServiceProvider();
        }

        private static NuriServiceProvider BuildServices(IServiceProvider serviceProvider, Action<NuriServiceCollection>? configureServices)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var services = new NuriServiceCollection();
            services.UseFallback(serviceProvider);
            services.AddNuriWpfServices();
            configureServices?.Invoke(services);
            return services.BuildServiceProvider();
        }

        public static NuriServiceProvider CreateDefaultServices()
        {
            var services = new NuriServiceCollection();
            services.AddNuriWpfServices();
            return services.BuildServiceProvider();
        }
    }
}
