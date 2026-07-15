using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using Nuri.UI;
using Nuri.UI.Dsl;

namespace Nuri.WPF
{
    public static class NuriApplication
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<ApplicationRoot> Roots = new List<ApplicationRoot>();
        private static NuriApplicationOptions Options = new NuriApplicationOptions();
        private static bool _hotReloadAttached;
        private static bool _configurationLocked;

        public static void Run<TComponent>(string title, double width = 800, double height = 600)
            where TComponent : Component, new()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                RunFromNonStaThread(() => RunCore<TComponent>(title, width, height));
                return;
            }

            RunCore<TComponent>(title, width, height);
        }

        private static void RunCore<TComponent>(string title, double width, double height)
            where TComponent : Component, new()
        {
            EnsureHotReloadAttached();

            var application = Application.Current;
            if (application == null)
            {
                application = new Application
                {
                    ShutdownMode = ShutdownMode.OnMainWindowClose
                };
                application.Run(CreateWindow<TComponent>(title, width, height));
                return;
            }

            application.ShutdownMode = ShutdownMode.OnMainWindowClose;
            var window = Show<TComponent>(title, width, height);
            application.MainWindow ??= window;
        }

        private static void RunFromNonStaThread(Action run)
        {
            var existingApplication = Application.Current;
            if (existingApplication != null)
            {
                if (existingApplication.Dispatcher.CheckAccess())
                {
                    throw new InvalidOperationException(
                        "The existing WPF Application was created on a non-STA thread and cannot be repaired by NuriApplication.Run.");
                }

                existingApplication.Dispatcher.Invoke(run);
                return;
            }

            ExceptionDispatchInfo? failure = null;
            var applicationThread = new Thread(() =>
            {
                try
                {
                    run();
                }
                catch (Exception exception)
                {
                    failure = ExceptionDispatchInfo.Capture(exception);
                }
            })
            {
                IsBackground = false,
                Name = "Nuri WPF Application"
            };

            applicationThread.SetApartmentState(ApartmentState.STA);
            applicationThread.Start();
            applicationThread.Join();
            failure?.Throw();
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

        public static ApplicationRoot Attach(Window window, IElement rootElement)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            if (rootElement == null)
                throw new ArgumentNullException(nameof(rootElement));

            EnsureHotReloadAttached();

            var root = ApplicationRoot.Initialize(rootElement, window);
            Register(root);
            window.Closed += (_, __) =>
            {
                Unregister(root);
                root.Dispose();
            };

            return root;
        }

        public static void Configure()
        {
            EnsureHotReloadAttached();
        }

        public static void Configure(Action<NuriApplicationOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            lock (SyncRoot)
            {
                if (_configurationLocked)
                    throw new InvalidOperationException("NuriApplication must be configured before the first application root is created.");

                var configuredOptions = Options.Clone();
                configure(configuredOptions);
                Options = configuredOptions;
            }

            EnsureHotReloadAttached();
        }

        internal static DevToolsConfiguration LockDevToolsConfiguration()
        {
            lock (SyncRoot)
            {
                _configurationLocked = true;
                return new DevToolsConfiguration(Options.DevTools.Enabled, Options.DevTools.ToggleKey);
            }
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
    }
}
