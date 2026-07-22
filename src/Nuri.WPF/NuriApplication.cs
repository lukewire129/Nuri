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
        private static bool _hotReloadAttached;

        public static NuriApplicationBuilder<TComponent> Create<TComponent>(
            string title,
            double width = 800,
            double height = 600)
            where TComponent : Component, new()
        {
            return new NuriApplicationBuilder<TComponent>(title, width, height);
        }

        public static void Run<TComponent>(string title, double width = 800, double height = 600)
            where TComponent : Component, new()
        {
            Create<TComponent>(title, width, height).Run();
        }

        internal static void RunOnApplicationThread(Action run)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                run();
                return;
            }

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
            return Create<TComponent>(title, width, height).Show();
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
            return Attach(window, rootElement, includeInDiagnostics: true);
        }

        public static ApplicationRoot Attach(
            Window window,
            IElement rootElement,
            bool includeInDiagnostics)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            if (rootElement == null)
                throw new ArgumentNullException(nameof(rootElement));

            EnsureHotReloadAttached();

            var root = ApplicationRoot.Initialize(rootElement, window, includeInDiagnostics);
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

        private static WindowView CreateRoot<TComponent>(string title, double width, double height)
            where TComponent : Component, new()
        {
            return new WindowView(new TComponent())
                .WithTitle(title)
                .WithSize(width, height);
        }
    }
}
