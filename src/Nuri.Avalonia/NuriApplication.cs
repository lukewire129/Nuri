using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Nuri.UI;
using Nuri.UI.Dsl;

namespace Nuri.Avalonia
{
    public static class NuriApplication
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<AvaloniaApplicationRoot> Roots = new List<AvaloniaApplicationRoot>();
        private static bool _hotReloadAttached;

        public static NuriApplicationBuilder<TComponent> Create<TComponent>(
            string[] args,
            string title,
            double width = 800,
            double height = 600)
            where TComponent : Component, new()
        {
            return new NuriApplicationBuilder<TComponent>(args, title, width, height);
        }

        public static void Run<TComponent>(string[] args, string title, double width = 800, double height = 600)
            where TComponent : Component, new()
        {
            Create<TComponent>(args, title, width, height).Run();
        }

        public static AvaloniaApplicationRoot Attach(Window window, IElement rootElement)
        {
            return Attach(window, rootElement, includeInDiagnostics: true);
        }

        public static AvaloniaApplicationRoot Attach(
            Window window,
            IElement rootElement,
            bool includeInDiagnostics)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            if (rootElement == null)
                throw new ArgumentNullException(nameof(rootElement));

            EnsureHotReloadAttached();

            var root = AvaloniaApplicationRoot.Initialize(rootElement, window, includeInDiagnostics);
            Register(root);
            window.Closed += (_, __) =>
            {
                Unregister(root);
                root.Dispose();
            };

            return root;
        }

        internal static void Run(
            string[] args,
            IElement rootElement,
            Action<Window>? windowCreated)
        {
            EnsureHotReloadAttached();

            AppBuilder
                .Configure(() => new NuriAvaloniaApp(rootElement, windowCreated))
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }

        internal static void Register(AvaloniaApplicationRoot root)
        {
            lock (SyncRoot)
            {
                if (!Roots.Contains(root))
                    Roots.Add(root);
            }
        }

        internal static void Unregister(AvaloniaApplicationRoot root)
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
            AvaloniaApplicationRoot[] roots;
            lock (SyncRoot)
            {
                roots = Roots.ToArray();
            }

            foreach (var root in roots)
                root.DispatchRebuild();
        }

        private static void OnAnyComponentStateChanged(object? sender, Component component)
        {
            AvaloniaApplicationRoot[] roots;
            lock (SyncRoot)
            {
                roots = Roots.ToArray();
            }

            foreach (var root in roots)
                root.ScheduleComponentRebuild(component);
        }

        internal static WindowView CreateRoot<TComponent>(string title, double width, double height)
            where TComponent : Component, new()
        {
            return new WindowView(new TComponent())
                .WithTitle(title)
                .WithSize(width, height);
        }
    }

    internal sealed class NuriAvaloniaApp : Application
    {
        private readonly IElement _rootElement;
        private readonly Action<Window>? _windowCreated;

        public NuriAvaloniaApp(IElement rootElement, Action<Window>? windowCreated = null)
        {
            _rootElement = rootElement;
            _windowCreated = windowCreated;
        }

        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new Window();
                NuriApplication.Attach(window, _rootElement);
                _windowCreated?.Invoke(window);
                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
