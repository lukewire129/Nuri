using Nuri.UI.Dsl;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Nuri.WPF.PreviewHost;

internal sealed class PreviewWindow : Window
{
    private readonly PreviewOptions _options;
    private readonly PreviewBuildService _buildService;
    private readonly ContentControl _previewSurface = new();
    private readonly DispatcherTimer _reloadTimer;
    private readonly FileSystemWatcher? _commandWatcher;
    private readonly List<PreviewAssemblyLoadContext> _retiredLoadContexts = new();
    private ApplicationRoot? _currentRoot;
    private PreviewAssemblyLoadContext? _loadContext;
    private string? _rootComponentFullName;
    private bool _isReloading;
    private bool _reloadAgain;
    private bool _pendingFullReload;
    private bool _pendingPartialReload;
    [StructLayout (LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport ("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport ("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    [DllImport ("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport ("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(
        IntPtr hWnd,
        int nIndex,
        IntPtr dwNewLong);

    [DllImport ("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport ("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr32(
        IntPtr hWnd,
        int nIndex,
        IntPtr dwNewLong);
    private const int GWL_STYLE = -16;
    private const long WS_CHILD = 0x40000000L;
    private const long WS_POPUP = 0x80000000L;
    private const long WS_CAPTION = 0x00C00000L;
    private const long WS_THICKFRAME = 0x00040000L;
    private const long WS_SYSMENU = 0x00080000L;
    private const long WS_MINIMIZEBOX = 0x00020000L;
    private const long WS_MAXIMIZEBOX = 0x00010000L;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport ("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
    public PreviewWindow(PreviewOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _buildService = new PreviewBuildService(options.ProjectPath);

        Title = "Nuri Preview Renderer";
        Background = System.Windows.Media.Brushes.White;
        Content = _previewSurface;

        if (options.Embedded)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
        }

        _reloadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _reloadTimer.Tick += (_, _) =>
        {
            _reloadTimer.Stop();
            OnReloadTimerTick();
        };

        _commandWatcher = CreateCommandWatcher(options.CommandFilePath);

        Loaded += async (_, _) =>
        {
            Component.AnyStateChanged += OnAnyComponentStateChanged;
            if (_commandWatcher != null)
                _commandWatcher.EnableRaisingEvents = true;

            await ReloadAsync(resetState: true);
        };
        Closed += (_, _) => Cleanup();
    }
    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64 (hWnd, nIndex)
            : GetWindowLongPtr32 (hWnd, nIndex);
    }
    private static IntPtr SetWindowLongPtr(
        IntPtr hWnd,
        int nIndex,
        IntPtr value)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64 (hWnd, nIndex, value)
            : SetWindowLongPtr32 (hWnd, nIndex, value);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized (e);

        if (!_options.Embedded || _options.ParentHandle == IntPtr.Zero)
            return;

        var childHandle = new WindowInteropHelper (this).Handle;

        // 1. 먼저 자식 창 스타일로 변경
        var style = GetWindowLongPtr (childHandle, GWL_STYLE).ToInt64 ();

        style &= ~WS_POPUP;
        style |= WS_CHILD;

        SetWindowLongPtr (
        childHandle,
        GWL_STYLE,
        new IntPtr (style));

        SetParent (childHandle, _options.ParentHandle);

        if (GetClientRect (_options.ParentHandle, out var rect))
        {
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            SetWindowPos (
                childHandle,
                IntPtr.Zero,
                0,
                0,
                width,
                height,
                SWP_NOZORDER |
                SWP_NOACTIVATE |
                SWP_FRAMECHANGED |
                SWP_SHOWWINDOW);
        }
    }

    private FileSystemWatcher? CreateCommandWatcher(string? commandFilePath)
    {
        if (string.IsNullOrWhiteSpace(commandFilePath))
            return null;

        var directory = Path.GetDirectoryName(commandFilePath);
        var fileName = Path.GetFileName(commandFilePath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            return null;

        Directory.CreateDirectory(directory);
        var watcher = new FileSystemWatcher(directory, fileName)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size
        };

        watcher.Changed += OnCommandFileChanged;
        watcher.Created += OnCommandFileChanged;
        return watcher;
    }

    private void OnCommandFileChanged(object sender, FileSystemEventArgs args)
    {
        Dispatcher.Invoke(() =>
        {
            var command = ReadCommand(args.FullPath);
            if (string.Equals(command, "full", StringComparison.OrdinalIgnoreCase))
            {
                _pendingFullReload = true;
                _pendingPartialReload = false;
            }
            else
            {
                _pendingPartialReload = true;
            }

            _reloadTimer.Stop();
            _reloadTimer.Start();
        });
    }

    private async void OnReloadTimerTick()
    {
        if (_pendingFullReload)
        {
            _pendingFullReload = false;
            _pendingPartialReload = false;
            _rootComponentFullName = null;
            await ReloadAsync(resetState: true);
            return;
        }

        if (_pendingPartialReload)
        {
            _pendingPartialReload = false;
            await ReloadAsync(resetState: false);
        }
    }

    private void OnAnyComponentStateChanged(object? sender, Component component)
    {
        _currentRoot?.ScheduleComponentRebuild(component);
    }

    private async Task ReloadAsync(bool resetState)
    {
        if (_isReloading)
        {
            _reloadAgain = true;
            _pendingFullReload |= resetState;
            return;
        }

        _isReloading = true;
        try
        {
            ReportStatus(resetState ? "Rendering preview..." : "Applying preview changes...");
            ShowMessage(resetState ? "Rendering preview..." : "Applying preview changes...");

            var result = await _buildService.BuildAsync(CancellationToken.None, preferRoslyn: true);
            if (!result.Succeeded || result.AssemblyPath == null)
            {
                ReportStatus("Preview build failed." + Environment.NewLine + result.Log);
                ShowMessage("Preview build failed." + Environment.NewLine + result.Log);
                return;
            }

            var renderSession = CreateRenderSession(result.AssemblyPath);
            ApplyRenderSession(renderSession, resetState || _currentRoot == null);
            RetirePreviousLoadContext(renderSession.LoadContext);
        }
        catch (Exception ex)
        {
            ReportStatus(ex.ToString());
            ShowMessage(ex.ToString());
        }
        finally
        {
            _isReloading = false;
            if (_reloadAgain)
            {
                var resetAgain = _pendingFullReload;
                _reloadAgain = false;
                _pendingFullReload = false;
                _pendingPartialReload = true;
                _reloadTimer.Stop();
                _reloadTimer.Start();
                if (resetAgain)
                {
                    _pendingPartialReload = false;
                    _pendingFullReload = true;
                }
            }
        }
    }

    private RenderSession CreateRenderSession(string assemblyPath)
    {
        var shadowAssemblyPath = PreviewAssemblyLoadContext.ShadowCopy(assemblyPath);
        var loadContext = new PreviewAssemblyLoadContext(shadowAssemblyPath);
        var assembly = loadContext.LoadFromAssemblyPath(shadowAssemblyPath);
        var root = ResolveRootComponent(assembly);
        var rootElement = CreateRootElement(root);
        return new RenderSession(loadContext, root, rootElement);
    }

    private ComponentDescriptor ResolveRootComponent(System.Reflection.Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(_rootComponentFullName))
        {
            var type = assembly.GetType(_rootComponentFullName!, throwOnError: false);
            if (type != null && ComponentDiscovery.IsPreviewableComponent(type))
                return new ComponentDescriptor(type);
        }

        var components = ComponentDiscovery.Discover(assembly);
        var root = RootComponentDiscovery.ResolveRoot(_options.ProjectPath, components);
        _rootComponentFullName = root.FullName;
        return root;
    }

    private static IElement CreateRootElement(ComponentDescriptor root)
    {
        var component = (Component)Activator.CreateInstance(root.ComponentType)!;
        return new WindowView(component)
            .WithTitle(root.DisplayName)
            .WithSize(1000, 700);
    }

    private void ApplyRenderSession(RenderSession session, bool resetState)
    {
        if (resetState || _currentRoot == null)
        {
            DisposeCurrentRoot();
            _currentRoot = ApplicationRoot.Initialize(
                session.RootElement,
                new ContentControlHost(_previewSurface),
                () => _previewSurface.Dispatcher,
                _ => { });
        }
        else
        {
            _currentRoot.ReplaceRoot(session.RootElement, resetState: false);
        }

        ReportStatus("Previewing " + session.Root.FullName);
        ShowMessageIfSurfaceIsEmpty("Previewing " + session.Root.FullName);
    }

    private void RetirePreviousLoadContext(PreviewAssemblyLoadContext nextLoadContext)
    {
        if (_loadContext != null)
            _retiredLoadContexts.Add(_loadContext);

        _loadContext = nextLoadContext;
        CollectRetiredLoadContexts();
    }

    private void CollectRetiredLoadContexts()
    {
        for (var i = _retiredLoadContexts.Count - 1; i >= 0; i--)
        {
            _retiredLoadContexts[i].Unload();
            _retiredLoadContexts.RemoveAt(i);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void ShowMessageIfSurfaceIsEmpty(string message)
    {
        if (_previewSurface.Content == null)
            ShowMessage(message);
    }

    private void ShowMessage(string message)
    {
        if (_currentRoot != null && _previewSurface.Content != null)
            return;

        _previewSurface.Content = new TextBlock
        {
            Text = message,
            Margin = new Thickness(16),
            TextWrapping = TextWrapping.Wrap
        };
    }

    private void ReportStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(_options.StatusFilePath))
            return;

        try
        {
            var directory = Path.GetDirectoryName(_options.StatusFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_options.StatusFilePath, message);
        }
        catch
        {
        }
    }

    private static string ReadCommand(string path)
    {
        for (var i = 0; i < 5; i++)
        {
            try
            {
                if (!File.Exists(path))
                    return "partial";

                var text = File.ReadAllText(path).Trim();
                var firstSpace = text.IndexOf(' ');
                return firstSpace < 0 ? text : text.Substring(0, firstSpace);
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        return "partial";
    }

    private void DisposeCurrentRoot()
    {
        _currentRoot?.Dispose();
        _currentRoot = null;
        _previewSurface.Content = null;
    }

    private void UnloadCurrentAssembly()
    {
        if (_loadContext != null)
        {
            _loadContext.Unload();
            _loadContext = null;
        }

        CollectRetiredLoadContexts();
    }

    private void Cleanup()
    {
        Component.AnyStateChanged -= OnAnyComponentStateChanged;
        _commandWatcher?.Dispose();
        DisposeCurrentRoot();
        UnloadCurrentAssembly();
    }

    private sealed class RenderSession
    {
        public RenderSession(PreviewAssemblyLoadContext loadContext, ComponentDescriptor root, IElement rootElement)
        {
            LoadContext = loadContext ?? throw new ArgumentNullException(nameof(loadContext));
            Root = root ?? throw new ArgumentNullException(nameof(root));
            RootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
        }

        public PreviewAssemblyLoadContext LoadContext { get; }

        public ComponentDescriptor Root { get; }

        public IElement RootElement { get; }
    }
}
