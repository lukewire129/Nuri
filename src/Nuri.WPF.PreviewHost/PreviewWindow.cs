using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Nuri.UI.Dsl;
using Nuri.WPF;

namespace Nuri.WPF.PreviewHost;

internal sealed class PreviewWindow : Window
{
    private readonly PreviewOptions _options;
    private readonly PreviewBuildService _buildService;
    private readonly ListBox _componentList = new();
    private readonly ContentControl _previewSurface = new();
    private readonly TextBox _statusText = new();
    private readonly DispatcherTimer _reloadTimer;
    private readonly FileSystemWatcher _watcher;
    private IReadOnlyList<ComponentDescriptor> _components = Array.Empty<ComponentDescriptor>();
    private ApplicationRoot? _currentRoot;
    private PreviewAssemblyLoadContext? _loadContext;
    private string? _selectedComponentName;
    private bool _isReloading;
    private bool _reloadAgain;
    private bool _suppressSelectionChanged;

    public PreviewWindow(PreviewOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _buildService = new PreviewBuildService(options.ProjectPath);
        _selectedComponentName = options.ComponentName;

        Title = "Nuri Preview Host";
        Width = 1200;
        Height = 800;
        Content = CreateLayout();

        _reloadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _reloadTimer.Tick += async (_, _) =>
        {
            _reloadTimer.Stop();
            await ReloadAsync();
        };

        _watcher = CreateWatcher(options.ProjectPath);
        Loaded += async (_, _) =>
        {
            Component.AnyStateChanged += OnAnyComponentStateChanged;
            _watcher.EnableRaisingEvents = true;
            await ReloadAsync();
        };
        Closed += (_, _) => Cleanup();
    }

    private UIElement CreateLayout()
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(140) });

        _componentList.Margin = new Thickness(8);
        _componentList.SelectionChanged += (_, _) =>
        {
            if (_suppressSelectionChanged)
                return;

            if (_componentList.SelectedItem is not ComponentDescriptor selected)
                return;

            _selectedComponentName = selected.FullName;
            RenderSelectedComponent();
        };
        Grid.SetRowSpan(_componentList, 2);
        root.Children.Add(_componentList);

        var previewBorder = new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = System.Windows.Media.Brushes.LightGray,
            Margin = new Thickness(0, 8, 8, 4),
            Child = _previewSurface
        };
        Grid.SetColumn(previewBorder, 1);
        root.Children.Add(previewBorder);

        _statusText.IsReadOnly = true;
        _statusText.TextWrapping = TextWrapping.Wrap;
        _statusText.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _statusText.Margin = new Thickness(0, 4, 8, 8);
        Grid.SetColumn(_statusText, 1);
        Grid.SetRow(_statusText, 1);
        root.Children.Add(_statusText);

        return root;
    }

    private FileSystemWatcher CreateWatcher(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        var watcher = new FileSystemWatcher(projectDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileChanged;
        return watcher;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        if (!ShouldReloadFor(args.FullPath))
            return;

        Dispatcher.Invoke(() =>
        {
            _reloadTimer.Stop();
            _reloadTimer.Start();
        });
    }

    private void OnAnyComponentStateChanged(object? sender, Component component)
    {
        _currentRoot?.ScheduleComponentRebuild(component);
    }

    private static bool ShouldReloadFor(string path)
    {
        if (path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return false;

        var extension = Path.GetExtension(path);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".xaml", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ReloadAsync()
    {
        if (_isReloading)
        {
            _reloadAgain = true;
            return;
        }

        _isReloading = true;
        try
        {
            SetStatus("Building " + _options.ProjectPath);
            var result = await _buildService.BuildAsync(CancellationToken.None);
            if (!result.Succeeded || result.AssemblyPath == null)
            {
                SetStatus("Build failed." + Environment.NewLine + result.Log);
                return;
            }

            LoadComponents(result.AssemblyPath);
            SetStatus("Build succeeded." + Environment.NewLine + result.Log);
        }
        catch (Exception ex)
        {
            SetStatus(ex.ToString());
        }
        finally
        {
            _isReloading = false;
            if (_reloadAgain)
            {
                _reloadAgain = false;
                _reloadTimer.Stop();
                _reloadTimer.Start();
            }
        }
    }

    private void LoadComponents(string assemblyPath)
    {
        DisposeCurrentRoot();
        ClearComponentList();
        UnloadCurrentAssembly();

        var shadowAssemblyPath = PreviewAssemblyLoadContext.ShadowCopy(assemblyPath);
        _loadContext = new PreviewAssemblyLoadContext(shadowAssemblyPath);
        var assembly = _loadContext.LoadFromAssemblyPath(shadowAssemblyPath);
        _components = ComponentDiscovery.Discover(assembly);

        _suppressSelectionChanged = true;
        _componentList.ItemsSource = _components;

        if (_components.Count == 0)
        {
            _suppressSelectionChanged = false;
            _previewSurface.Content = new TextBlock
            {
                Text = "No previewable Nuri components were found.",
                Margin = new Thickness(20)
            };
            return;
        }

        var selected = SelectComponent(_components);
        _componentList.SelectedItem = selected;
        _suppressSelectionChanged = false;
        RenderSelectedComponent();
    }

    private ComponentDescriptor SelectComponent(IReadOnlyList<ComponentDescriptor> components)
    {
        var selected = components.FirstOrDefault(component =>
            string.Equals(component.FullName, _selectedComponentName, StringComparison.Ordinal)
            || string.Equals(component.ComponentType.Name, _selectedComponentName, StringComparison.Ordinal));

        selected ??= components[0];
        _selectedComponentName = selected.FullName;
        return selected;
    }

    private void RenderSelectedComponent()
    {
        if (_componentList.SelectedItem is not ComponentDescriptor selected)
            return;

        try
        {
            DisposeCurrentRoot();
            var component = (Component)Activator.CreateInstance(selected.ComponentType)!;
            component.SetProperty(Nuri.Constants.PropertyKeys.Title, selected.DisplayName);
            _currentRoot = ApplicationRoot.Initialize(
                component,
                new ContentControlHost(_previewSurface),
                () => _previewSurface.Dispatcher,
                _ => { });
            SetStatus("Previewing " + selected.FullName);
        }
        catch (Exception ex)
        {
            SetStatus(ex.ToString());
        }
    }

    private void SetStatus(string message)
    {
        _statusText.Text = message;
    }

    private void DisposeCurrentRoot()
    {
        _currentRoot?.Dispose();
        _currentRoot = null;
        _previewSurface.Content = null;
    }

    private void ClearComponentList()
    {
        _suppressSelectionChanged = true;
        _componentList.ItemsSource = null;
        _components = Array.Empty<ComponentDescriptor>();
        _suppressSelectionChanged = false;
    }

    private void UnloadCurrentAssembly()
    {
        if (_loadContext == null)
            return;

        var weakReference = new WeakReference(_loadContext, trackResurrection: false);
        _loadContext.Unload();
        _loadContext = null;

        for (var i = 0; weakReference.IsAlive && i < 2; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private void Cleanup()
    {
        Component.AnyStateChanged -= OnAnyComponentStateChanged;
        _watcher.Dispose();
        DisposeCurrentRoot();
        UnloadCurrentAssembly();
    }
}
