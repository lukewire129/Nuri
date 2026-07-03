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
    public static class NuriAvaloniaApplication
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<AvaloniaApplicationRoot> Roots = new List<AvaloniaApplicationRoot>();
        private static bool _hotReloadAttached;

        public static void Run<TComponent>(string[] args, string title, double width = 800, double height = 600)
            where TComponent : Component, new()
        {
            EnsureHotReloadAttached();

            AppBuilder
                .Configure(() => new NuriAvaloniaApp(CreateRoot<TComponent>(title, width, height)))
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AvaloniaApplicationRoot Attach(Window window, IElement rootElement)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            if (rootElement == null)
                throw new ArgumentNullException(nameof(rootElement));

            EnsureHotReloadAttached();

            var root = AvaloniaApplicationRoot.Initialize(rootElement, window);
            Register(root);
            window.Closed += (_, __) =>
            {
                Unregister(root);
                root.Dispose();
            };

            return root;
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

        private static WindowView CreateRoot<TComponent>(string title, double width, double height)
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

        public NuriAvaloniaApp(IElement rootElement)
        {
            _rootElement = rootElement;
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
                NuriAvaloniaApplication.Attach(window, _rootElement);
                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
