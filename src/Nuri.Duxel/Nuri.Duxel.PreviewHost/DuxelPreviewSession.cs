using Duxel.Core;
using Nuri.PreviewHost;
using Nuri.UI.Dsl;

namespace Nuri.Duxel.PreviewHost;

internal sealed class DuxelPreviewSession : IDisposable
{
    private const int ReloadDebounceMilliseconds = 150;
    private static readonly string[] SharedAssemblyNames =
    {
        "Nuri",
        "Nuri.Duxel",
        "Nuri.Duxel.Windows",
        "Duxel.Core",
        "Duxel.App",
        "Duxel.Vulkan",
        "Duxel.Platform.Windows",
        "Duxel.Windows.App"
    };

    private readonly PreviewOptions _options;
    private readonly PreviewBuildService _buildService;
    private readonly NativePreviewWindow _nativeWindow = new();
    private readonly object _reloadGate = new();
    private readonly object _statusGate = new();
    private readonly FileSystemWatcher? _commandWatcher;
    private readonly System.Threading.Timer _reloadTimer;
    private readonly List<PreviewAssemblyLoadContext> _retiredLoadContexts = new();
    private PreviewAssemblyLoadContext? _loadContext;
    private PreviewCaptureServer? _captureServer;
    private NuriDuxelScreen? _screen;
    private string? _rootComponentFullName;
    private string? _lastCommandText;
    private bool _pendingFullReload;
    private bool _pendingPartialReload;
    private bool _reloadRunning;
    private bool _disposed;
    private string _latestStatus = "Starting Duxel preview...";
    private bool _isBuilding;
    private bool _hasError;
    private float _previewScale = 1f;
    private bool _fitToWindow;
    private const int PreviewWidth = 1000;
    private const int PreviewHeight = 700;
    private const float MinimumZoom = 0.25f;
    private const float MaximumZoom = 4f;
    private const float ZoomStep = 0.1f;

    public UiTheme Theme { get; private set; }

    public DuxelThemeController ThemeController { get; } = new();

    public float GetContentScale()
    {
        return Volatile.Read(ref _previewScale);
    }

    public DuxelPreviewSession(PreviewOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Theme = DuxelPreviewConfigurationDiscovery.DiscoverTheme(options.ProjectPath) ?? UiTheme.ImGuiDark;
        _buildService = new PreviewBuildService(options.ProjectPath);
        _reloadTimer = new System.Threading.Timer(_ => StartPendingReload(), null, Timeout.Infinite, Timeout.Infinite);
        _commandWatcher = CreateCommandWatcher(options.CommandFilePath);
        if (_commandWatcher is not null)
            _commandWatcher.EnableRaisingEvents = true;

        Component.AnyStateChanged += OnAnyComponentStateChanged;
    }

    public async Task<IElement> BuildInitialRootAsync()
    {
        ReportStatus("Rendering Duxel preview...", isBuilding: true);
        var result = await _buildService.BuildAsync(CancellationToken.None, preferRoslyn: true).ConfigureAwait(false);
        if (!result.Succeeded || result.AssemblyPath == null)
        {
            ReportStatus("Preview build failed." + Environment.NewLine + result.Log, hasError: true);
            throw new InvalidOperationException("Preview build failed." + Environment.NewLine + result.Log);
        }

        var renderSession = CreateRenderSession(result.AssemblyPath);
        _loadContext = renderSession.LoadContext;
        ReportStatus("Previewing " + renderSession.Root.FullName);
        return renderSession.RootElement;
    }

    public void AttachScreen(NuriDuxelScreen screen)
    {
        _screen = screen ?? throw new ArgumentNullException(nameof(screen));
    }

    public void AttachWindow(IntPtr windowHandle)
    {
        _nativeWindow.Attach(windowHandle, _options.Embedded, _options.ParentHandle);
        ApplyPreviewLayout();
        if (!_options.CaptureEnabled)
            return;

        _captureServer = new PreviewCaptureServer(
            _nativeWindow.CaptureJpeg,
            _nativeWindow.Focus,
            GetCaptureStatus,
            _options.ConnectionFilePath!,
            _options.CaptureFramesPerSecond);
        _captureServer.Start();
        _captureServer.RequestCapture();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Component.AnyStateChanged -= OnAnyComponentStateChanged;
        _captureServer?.Dispose();
        _commandWatcher?.Dispose();
        _reloadTimer.Dispose();
        _loadContext?.Unload();
        lock (_retiredLoadContexts)
        {
            foreach (var loadContext in _retiredLoadContexts)
                loadContext.Unload();
            _retiredLoadContexts.Clear();
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
        var commandText = ReadCommandText(args.FullPath);
        lock (_reloadGate)
        {
            if (string.Equals(commandText, _lastCommandText, StringComparison.Ordinal))
                return;

            _lastCommandText = commandText;
            var command = ParseCommand(commandText);
            if (string.Equals(command, "viewport-resize", StringComparison.OrdinalIgnoreCase))
            {
                ApplyPreviewLayout();
                return;
            }

            if (string.Equals(command, "zoom-in", StringComparison.OrdinalIgnoreCase))
            {
                SetZoom(GetContentScale() + ZoomStep);
                return;
            }

            if (string.Equals(command, "zoom-out", StringComparison.OrdinalIgnoreCase))
            {
                SetZoom(GetContentScale() - ZoomStep);
                return;
            }

            if (string.Equals(command, "zoom-reset", StringComparison.OrdinalIgnoreCase))
            {
                SetZoom(1f);
                return;
            }

            if (string.Equals(command, "zoom-fit", StringComparison.OrdinalIgnoreCase))
            {
                _fitToWindow = true;
                ApplyPreviewLayout();
                return;
            }

            if (string.Equals(command, "zoom-center", StringComparison.OrdinalIgnoreCase))
            {
                ApplyPreviewLayout();
                return;
            }

            if (string.Equals(command, "full", StringComparison.OrdinalIgnoreCase))
            {
                _pendingFullReload = true;
                _pendingPartialReload = false;
            }
            else
            {
                _pendingPartialReload = true;
            }

            _reloadTimer.Change(ReloadDebounceMilliseconds, Timeout.Infinite);
        }
    }

    private void StartPendingReload()
    {
        bool resetState;
        lock (_reloadGate)
        {
            if (_reloadRunning || (!_pendingFullReload && !_pendingPartialReload) || _disposed)
                return;

            _reloadRunning = true;
            resetState = _pendingFullReload;
            _pendingFullReload = false;
            _pendingPartialReload = false;
            if (resetState)
                _rootComponentFullName = null;
        }

        _ = ReloadAsync(resetState);
    }

    private async Task ReloadAsync(bool resetState)
    {
        try
        {
            ReportStatus(
                resetState ? "Rendering Duxel preview..." : "Applying Duxel preview changes...",
                isBuilding: true);
            var result = await _buildService.BuildAsync(CancellationToken.None, preferRoslyn: true).ConfigureAwait(false);
            if (!result.Succeeded || result.AssemblyPath == null)
            {
                ReportStatus("Preview build failed." + Environment.NewLine + result.Log, hasError: true);
                return;
            }

            var renderSession = CreateRenderSession(result.AssemblyPath);
            var screen = _screen ?? throw new InvalidOperationException("The Duxel preview screen is not ready.");
            screen.ReplaceRoot(renderSession.RootElement, resetState);
            RetirePreviousLoadContext(renderSession.LoadContext);
            ReportStatus("Previewing " + renderSession.Root.FullName);
            _captureServer?.RequestCapture();
        }
        catch (Exception ex)
        {
            ReportStatus(ex.ToString(), hasError: true);
        }
        finally
        {
            lock (_reloadGate)
            {
                _reloadRunning = false;
                if (_pendingFullReload || _pendingPartialReload)
                    _reloadTimer.Change(100, Timeout.Infinite);
            }
        }
    }

    private RenderSession CreateRenderSession(string assemblyPath)
    {
        RefreshConfiguredTheme();
        var shadowAssemblyPath = PreviewAssemblyLoadContext.ShadowCopy(assemblyPath);
        var loadContext = new PreviewAssemblyLoadContext(shadowAssemblyPath, SharedAssemblyNames);
        var assembly = loadContext.LoadFromAssemblyPath(shadowAssemblyPath);
        var root = ResolveRootComponent(assembly);
        var rootElement = CreateRootComponent(root.ComponentType);
        return new RenderSession(loadContext, root, rootElement);
    }

    private void RefreshConfiguredTheme()
    {
        Theme = DuxelPreviewConfigurationDiscovery.DiscoverTheme(_options.ProjectPath) ?? UiTheme.ImGuiDark;
        _screen?.RequestTheme(Theme);
    }

    private ComponentDescriptor ResolveRootComponent(System.Reflection.Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(_rootComponentFullName))
        {
            var type = assembly.GetType(_rootComponentFullName!, throwOnError: false);
            if (type != null && IsPreviewableDuxelComponent(type))
                return new ComponentDescriptor(type);
        }

        var components = GetLoadableTypes(assembly)
            .Where(IsPreviewableDuxelComponent)
            .Select(type => new ComponentDescriptor(type))
            .OrderBy(component => component.FullName, StringComparer.Ordinal)
            .ToArray();
        var root = RootComponentDiscovery.ResolveRoot(_options.ProjectPath, components);
        _rootComponentFullName = root.FullName;
        return root;
    }

    private Component CreateRootComponent(Type componentType)
    {
        var defaultConstructor = componentType.GetConstructor(Type.EmptyTypes);
        if (defaultConstructor is not null)
            return (Component)defaultConstructor.Invoke(null);

        var themeConstructor = componentType.GetConstructor(new[] { typeof(UiTheme) });
        if (themeConstructor is not null)
            return (Component)themeConstructor.Invoke(new object[] { Theme });

        var controllerConstructor = componentType.GetConstructor(new[] { typeof(DuxelThemeController) });
        if (controllerConstructor is not null)
            return (Component)controllerConstructor.Invoke(new object[] { ThemeController });

        throw new InvalidOperationException(
            $"Component '{componentType.FullName}' does not have a supported Duxel preview constructor.");
    }

    private static bool IsPreviewableDuxelComponent(Type type)
    {
        return typeof(Component).IsAssignableFrom(type)
            && type is { IsAbstract: false, IsGenericTypeDefinition: false }
            && (type.GetConstructor(Type.EmptyTypes) is not null
                || type.GetConstructor(new[] { typeof(UiTheme) }) is not null
                || type.GetConstructor(new[] { typeof(DuxelThemeController) }) is not null);
    }

    private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }

    private void RetirePreviousLoadContext(PreviewAssemblyLoadContext nextLoadContext)
    {
        var previous = _loadContext;
        _loadContext = nextLoadContext;
        if (previous == null)
            return;

        lock (_retiredLoadContexts)
            _retiredLoadContexts.Add(previous);

        _ = Task.Run(async () =>
        {
            await Task.Delay(1000).ConfigureAwait(false);
            lock (_retiredLoadContexts)
            {
                if (_retiredLoadContexts.Remove(previous))
                    previous.Unload();
            }
        });
    }

    private void SetZoom(float zoom)
    {
        _fitToWindow = false;
        Volatile.Write(ref _previewScale, Math.Clamp(zoom, MinimumZoom, MaximumZoom));
        ApplyPreviewLayout();
    }

    private void ApplyPreviewLayout()
    {
        var scale = _fitToWindow
            ? Math.Clamp(
                _nativeWindow.CalculateFitScale(PreviewWidth, PreviewHeight),
                0.05f,
                MaximumZoom)
            : GetContentScale();
        Volatile.Write(ref _previewScale, scale);
        _nativeWindow.ApplyPreviewScale(scale, PreviewWidth, PreviewHeight);
        _screen?.RequestFrame();
        _captureServer?.RequestCapture();
        ReportStatus(_fitToWindow
            ? $"Preview zoom: Fit ({scale:P0})"
            : $"Preview zoom: {scale:P0}");
    }

    private void OnAnyComponentStateChanged(object? sender, Component component)
    {
        _captureServer?.RequestCapture();
    }

    private PreviewCaptureStatus GetCaptureStatus()
    {
        lock (_statusGate)
            return new PreviewCaptureStatus(_latestStatus, _isBuilding, _hasError);
    }

    private void ReportStatus(string message, bool isBuilding = false, bool hasError = false)
    {
        lock (_statusGate)
        {
            _latestStatus = message;
            _isBuilding = isBuilding;
            _hasError = hasError;
        }

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

    private static string ReadCommandText(string path)
    {
        for (var i = 0; i < 5; i++)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path).Trim() : "partial";
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        return "partial";
    }

    private static string ParseCommand(string commandText)
    {
        var firstSpace = commandText.IndexOf(' ');
        return firstSpace < 0 ? commandText : commandText[..firstSpace];
    }

    private sealed record RenderSession(
        PreviewAssemblyLoadContext LoadContext,
        ComponentDescriptor Root,
        IElement RootElement);
}
