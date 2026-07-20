using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using DiagnosticsProcess = System.Diagnostics.Process;
using WinForms = System.Windows.Forms;

namespace Nuri.VisualStudioPreview;

public sealed class NuriPreviewControl : UserControl, IDisposable
{
    private readonly TextBlock _statusText = new TextBlock();
    private readonly TextBlock _zoomText = new TextBlock { Text = "100%" };
    private readonly RichTextBox _logText = new RichTextBox();
    private readonly WinForms.Panel _hostPanel = new WinForms.Panel
    {
        Dock = WinForms.DockStyle.Fill,
        BackColor = System.Drawing.Color.FromArgb(44, 44, 47)
    };
    private DiagnosticsProcess? _previewProcess;
    private FileSystemWatcher? _statusWatcher;
    private DocumentEvents? _documentEvents;
    private IntPtr _previewWindowHandle;
    private string? _commandFilePath;
    private string? _statusFilePath;
    private string? _startupProjectDirectory;
    private string? _lastHostStatusMessage;
    private bool _previewOwnsEmbeddedLayout;
    private int _zoomPercent = 100;
    private const int MinimumZoomPercent = 25;
    private const int MaximumZoomPercent = 400;
    private const int ZoomStepPercent = 10;

    public NuriPreviewControl()
    {
        var workspaceBrush = new SolidColorBrush(Color.FromRgb(44, 44, 47));
        var root = new DockPanel { Background = workspaceBrush };

        var toolbar = new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0),
            Height = 34
        };
        toolbar.SetResourceReference(Panel.BackgroundProperty, EnvironmentColors.ToolWindowBackgroundBrushKey);
        DockPanel.SetDock(toolbar, Dock.Top);

        var refreshButton = CreateToolbarButton("↻", "Refresh preview", 30);
        refreshButton.Click += (_, _) =>
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (IsPreviewHostRunning())
                {
                    SendFullRefreshCommand();
                    SetStatus("Refreshing preview...");
                }
                else if (PackageProvider.Package != null)
                {
                    await StartPreviewAsync(PackageProvider.Package);
                }
            }).FileAndForget("NuriPreview/Refresh");
        };
        DockPanel.SetDock(refreshButton, Dock.Left);
        toolbar.Children.Add(refreshButton);

        var zoomControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        };
        DockPanel.SetDock(zoomControls, Dock.Right);

        var zoomOutButton = CreateToolbarButton("−", "Zoom out", 28);
        zoomOutButton.Click += (_, _) => ChangeZoom(-ZoomStepPercent);
        zoomControls.Children.Add(zoomOutButton);

        var zoomResetButton = CreateToolbarButton(_zoomText, "Reset zoom", 52);
        zoomResetButton.Click += (_, _) => ResetZoom();
        _zoomText.HorizontalAlignment = HorizontalAlignment.Center;
        _zoomText.VerticalAlignment = VerticalAlignment.Center;
        _zoomText.Foreground = Brushes.Black;
        zoomControls.Children.Add(zoomResetButton);

        var zoomInButton = CreateToolbarButton("+", "Zoom in", 28);
        zoomInButton.Click += (_, _) => ChangeZoom(ZoomStepPercent);
        zoomControls.Children.Add(zoomInButton);

        var centerButton = CreateToolbarButton("Center", "Center the preview at the current zoom", 52);
        centerButton.Click += (_, _) => CenterPreview();
        zoomControls.Children.Add(centerButton);

        var fitButton = CreateToolbarButton("Fit", "Fit preview to window", 38);
        fitButton.Click += (_, _) => FitZoom();
        zoomControls.Children.Add(fitButton);
        toolbar.Children.Add(zoomControls);

        _statusText.VerticalAlignment = VerticalAlignment.Center;
        _statusText.TextTrimming = TextTrimming.CharacterEllipsis;
        _statusText.Text = "Open a C# file and run Nuri Preview.";
        _statusText.Margin = new Thickness(6, 0, 8, 0);
        _statusText.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.ToolWindowTextBrushKey);
        toolbar.Children.Add(_statusText);

        _logText.IsReadOnly = true;
        _logText.IsReadOnlyCaretVisible = true;
        _logText.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _logText.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _logText.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        _logText.Foreground = Brushes.White;
        _logText.BorderThickness = new Thickness(0);
        _logText.Padding = new Thickness(6, 4, 6, 4);
        _logText.FontFamily = new FontFamily("Consolas");
        _logText.FontSize = 12;
        _logText.Document = new FlowDocument
        {
            PagePadding = new Thickness(0)
        };

        var logBorder = new Border
        {
            Height = 140,
            Background = _logText.Background,
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 74)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = _logText
        };
        DockPanel.SetDock(logBorder, Dock.Bottom);

        var formsHost = new WindowsFormsHost
        {
            Child = _hostPanel,
            Background = workspaceBrush
        };
        _hostPanel.Resize += (_, _) => UpdateEmbeddedWindowLayout();

        root.Children.Add(toolbar);
        root.Children.Add(logBorder);
        root.Children.Add(formsHost);
        Content = root;

        AppendLog(_statusText.Text, isError: false);

        Unloaded += (_, _) =>
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                StopPreview();
            }).FileAndForget("NuriPreview/Unload");
        };
    }

    private static Button CreateToolbarButton(object content, string toolTip, double width)
    {
        var button = new Button
        {
            Content = content,
            ToolTip = toolTip,
            Width = width,
            Height = 26,
            Margin = new Thickness(2, 4, 0, 4),
            Padding = new Thickness(4, 0, 4, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        button.Foreground = Brushes.Black;
        return button;
    }

    private void ChangeZoom(int delta)
    {
        _zoomPercent = Math.Max(MinimumZoomPercent, Math.Min(MaximumZoomPercent, _zoomPercent + delta));
        _zoomText.Text = _zoomPercent + "%";
        SendPreviewCommand(delta > 0 ? "zoom-in" : "zoom-out");
    }

    private void ResetZoom()
    {
        _zoomPercent = 100;
        _zoomText.Text = "100%";
        SendPreviewCommand("zoom-reset");
    }

    private void FitZoom()
    {
        _zoomText.Text = "Fit";
        SendPreviewCommand("zoom-fit");
    }

    private void CenterPreview()
    {
        SendPreviewCommand("zoom-center");
    }

    public async Task StartPreviewAsync(AsyncPackage package)
    {
        PackageProvider.Package = package;
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        StopPreview();

        var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
        if (dte == null)
        {
            SetStatus("Visual Studio automation service is unavailable.", isError: true);
            return;
        }

        var projectPath = ResolvePreviewProjectPath(dte);
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            SetStatus("Open a C# file in a Nuri project or select a Startup Project.");
            return;
        }

        var hostSelection = PreviewHostCatalog.Select(projectPath!);
        if (hostSelection.Host == null)
        {
            SetStatus(
                hostSelection.ErrorMessage ?? "No supported Nuri preview renderer was detected.",
                isError: true);
            return;
        }

        var previewHost = hostSelection.Host;
        var previewHostPath = ResolvePreviewHostPath(dte, previewHost, projectPath!);
        if (string.IsNullOrWhiteSpace(previewHostPath))
        {
            SetStatus(
                "Build " + previewHost.ExecutableName + " first, then open Nuri Preview again.",
                isError: true);
            return;
        }

        SubscribeDocumentEvents(dte, projectPath!);
        StartPreviewHost(previewHost, previewHostPath!, projectPath!);
    }

    public void Dispose()
    {
        ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            StopPreview();
        });
    }

    private void StartPreviewHost(
        PreviewHostDefinition previewHost,
        string previewHostPath,
        string projectPath)
    {
        _previewOwnsEmbeddedLayout = string.Equals(
            previewHost.Id,
            "duxel",
            StringComparison.OrdinalIgnoreCase);
        //var channelDirectory = Path.Combine(Path.GetTempPath(), "NuriVisualStudioPreview", Guid.NewGuid().ToString("N"));
        //Directory.CreateDirectory(channelDirectory);
        //_commandFilePath = Path.Combine(channelDirectory, "preview.cmd");
        //_statusFilePath = Path.Combine(channelDirectory, "preview.status");
        //StartStatusWatcher(_statusFilePath);

        //var startInfo = new ProcessStartInfo
        //{
        //    FileName = previewHostPath,
        //    Arguments = "--embedded"
        //        + " --parent-hwnd " + _hostPanel.Handle
        //        + "--project " + Quote(projectPath)
        //        + " --command-file " + Quote(_commandFilePath)
        //        + " --status-file " + Quote(_statusFilePath),
        //    WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory,
        //    UseShellExecute = false
        //};

        _hostPanel.CreateControl ();

        var parentHandle = _hostPanel.Handle;

        var channelDirectory = Path.Combine (
            Path.GetTempPath (),
            "NuriVisualStudioPreview",
            Guid.NewGuid ().ToString ("N"));

        Directory.CreateDirectory (channelDirectory);

        _commandFilePath = Path.Combine (channelDirectory, "preview.cmd");
        _statusFilePath = Path.Combine (channelDirectory, "preview.status");

        StartStatusWatcher (_statusFilePath);

        var startInfo = new ProcessStartInfo
        {
            FileName = previewHostPath,

            Arguments =
                "--embedded"
                + " --parent-hwnd " + parentHandle.ToInt64 ()
                + " --project " + Quote (projectPath)
                + " --command-file " + Quote (_commandFilePath)
                + " --status-file " + Quote (_statusFilePath),

            WorkingDirectory =
                Path.GetDirectoryName (projectPath)
                ?? Environment.CurrentDirectory,

            UseShellExecute = false
        };

        try
        {
            _previewProcess = DiagnosticsProcess.Start(startInfo);
        }
        catch (Exception ex)
        {
            SetStatus("Failed to start PreviewHost: " + ex.Message, isError: true);
            return;
        }

        if (_previewProcess == null)
        {
            SetStatus("Failed to start PreviewHost.", isError: true);
            return;
        }

        SetStatus("Starting preview: " + Path.GetFileName(projectPath));
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            for (var i = 0; i < 80; i++)
            {
                if (_previewProcess == null || _previewProcess.HasExited)
                    break;

                _previewProcess.Refresh();
                if (_previewProcess.MainWindowHandle != IntPtr.Zero)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    AttachPreviewWindow(_previewProcess.MainWindowHandle);
                    SetStatus("Previewing " + projectPath);
                    return;
                }

                await Task.Delay(100);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            SetStatus("PreviewHost started, but no window handle was found.", isError: true);
        }).FileAndForget("NuriPreview/AttachPreviewHost");
    }

    private void AttachPreviewWindow(IntPtr windowHandle)
    {
        _previewWindowHandle = windowHandle;

        UpdateEmbeddedWindowLayout();

        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await Task.Yield();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            UpdateEmbeddedWindowLayout();
        }).FileAndForget("NuriPreview/ResizeEmbeddedWindow");
    }

    private void UpdateEmbeddedWindowLayout()
    {
        if (_previewOwnsEmbeddedLayout)
        {
            SendPreviewCommand("viewport-resize");
            return;
        }

        ResizeEmbeddedWindow();
    }

    private void ResizeEmbeddedWindow()
    {
        if (_previewWindowHandle == IntPtr.Zero)
            return;

        var width = _hostPanel.ClientSize.Width;
        var height = _hostPanel.ClientSize.Height;

        if (width <= 0 || height <= 0)
            return;

        MoveWindow (
            _previewWindowHandle,
            0,
            0,
            width,
            height,
            true);
    }

    private void StopPreview()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _previewWindowHandle = IntPtr.Zero;
        _previewOwnsEmbeddedLayout = false;
        _lastHostStatusMessage = null;
        _statusWatcher?.Dispose();
        _statusWatcher = null;
        UnsubscribeDocumentEvents();

        if (_previewProcess == null)
            return;

        try
        {
            if (!_previewProcess.HasExited)
            {
                _previewProcess.CloseMainWindow();
                if (!_previewProcess.WaitForExit(1000))
                    _previewProcess.Kill();
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            _previewProcess.Dispose();
            _previewProcess = null;
        }
    }

    private bool IsPreviewHostRunning()
    {
        try
        {
            return _previewProcess != null && !_previewProcess.HasExited;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void SendFullRefreshCommand()
    {
        SendPreviewCommand("full");
    }

    private void SendPartialRefreshCommand()
    {
        SendPreviewCommand("partial");
    }

    private void SendPreviewCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(_commandFilePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(_commandFilePath)!);
        File.WriteAllText(_commandFilePath, command + " " + DateTime.UtcNow.Ticks.ToString());
    }

    private void SubscribeDocumentEvents(DTE2 dte, string projectPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        UnsubscribeDocumentEvents();

        _startupProjectDirectory = Path.GetDirectoryName(projectPath);
        _documentEvents = dte.Events?.DocumentEvents;
        if (_documentEvents != null)
            _documentEvents.DocumentSaved += OnDocumentSaved;
    }

    private void UnsubscribeDocumentEvents()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_documentEvents != null)
            _documentEvents.DocumentSaved -= OnDocumentSaved;

        _documentEvents = null;
        _startupProjectDirectory = null;
    }

    private void OnDocumentSaved(Document document)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (!IsPreviewSourceDocument(document))
            return;

        if (!IsPreviewHostRunning())
            return;

        SendPartialRefreshCommand();
        SetStatus("저장 감지: preview 갱신 요청됨");
    }

    private bool IsPreviewSourceDocument(Document document)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var path = document.FullName;
        if (string.IsNullOrWhiteSpace(path)
            || !Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            return false;

        var startupProjectDirectory = _startupProjectDirectory;
        if (startupProjectDirectory == null || startupProjectDirectory.Length == 0)
            return false;

        var projectDirectory = startupProjectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase)
            && fullPath.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) < 0
            && fullPath.IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private void StartStatusWatcher(string statusFilePath)
    {
        var directory = Path.GetDirectoryName(statusFilePath);
        var fileName = Path.GetFileName(statusFilePath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            return;

        _statusWatcher?.Dispose();
        _statusWatcher = new FileSystemWatcher(directory, fileName)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size
        };

        _statusWatcher.Changed += (_, _) => ReadStatusFileAsync(statusFilePath).FileAndForget("NuriPreview/ReadStatus");
        _statusWatcher.Created += (_, _) => ReadStatusFileAsync(statusFilePath).FileAndForget("NuriPreview/ReadStatus");
        _statusWatcher.EnableRaisingEvents = true;
    }

    private async Task ReadStatusFileAsync(string statusFilePath)
    {
        string? message = null;
        for (var i = 0; i < 5; i++)
        {
            try
            {
                if (File.Exists(statusFilePath))
                {
                    message = File.ReadAllText(statusFilePath);
                    break;
                }
            }
            catch (IOException)
            {
            }

            await Task.Delay(50);
        }

        if (string.IsNullOrWhiteSpace(message))
            return;

        var statusMessage = (message ?? string.Empty).TrimEnd();
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (string.Equals(statusMessage, _lastHostStatusMessage, StringComparison.Ordinal))
            return;

        _lastHostStatusMessage = statusMessage;
        var firstLine = statusMessage.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0];
        SetStatus(firstLine, IsErrorStatus(firstLine), statusMessage);
    }

    private void SetStatus(string message, bool isError = false, string? logMessage = null)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _statusText.Text = message;
        UpdateZoomTextFromStatus(message);
        var output = logMessage ?? message;
        AppendLog(output, isError);
        (PackageProvider.Package as NuriPreviewPackage)?.WriteLine(output);
    }

    private void AppendLog(string message, bool isError)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (string.IsNullOrWhiteSpace(message))
            return;

        var paragraph = new Paragraph(new Run(message))
        {
            Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(255, 96, 96))
                : Brushes.White,
            Margin = new Thickness(0, 0, 0, 3)
        };
        _logText.Document.Blocks.Add(paragraph);
        while (_logText.Document.Blocks.Count > 500)
        {
            var firstBlock = _logText.Document.Blocks.FirstBlock;
            if (firstBlock == null)
                break;

            _logText.Document.Blocks.Remove(firstBlock);
        }

        _logText.ScrollToEnd();
    }

    private static bool IsErrorStatus(string message)
    {
        return message.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void UpdateZoomTextFromStatus(string message)
    {
        const string prefix = "Preview zoom: ";
        if (!message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var value = message.Substring(prefix.Length).Trim();
        if (value.StartsWith("Fit", StringComparison.OrdinalIgnoreCase))
        {
            _zoomText.Text = "Fit";
            return;
        }

        var percentIndex = value.IndexOf('%');
        if (percentIndex <= 0)
            return;

        var numberText = value.Substring(0, percentIndex);
        if (!int.TryParse(numberText, out var percent))
            return;

        _zoomPercent = Math.Max(MinimumZoomPercent, Math.Min(MaximumZoomPercent, percent));
        _zoomText.Text = _zoomPercent + "%";
    }

    private static string? ResolvePreviewProjectPath(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var activeProjectPath = ResolveActiveDocumentProjectPath(dte);
        return !string.IsNullOrWhiteSpace(activeProjectPath)
            ? activeProjectPath
            : ResolveStartupProjectPath(dte);
    }

    private static string? ResolveActiveDocumentProjectPath(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var document = dte.ActiveDocument;
        if (document == null
            || !Path.GetExtension(document.FullName).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var projectPath = document.ProjectItem?.ContainingProject?.FullName;
            return !string.IsNullOrWhiteSpace(projectPath) && File.Exists(projectPath)
                ? projectPath
                : null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static string? ResolveStartupProjectPath(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var startupProjects = dte.Solution?.SolutionBuild?.StartupProjects as Array;
        if (startupProjects == null || startupProjects.Length == 0)
            return null;

        foreach (var startupProject in startupProjects)
        {
            var uniqueName = startupProject as string;
            if (string.IsNullOrWhiteSpace(uniqueName))
                continue;

            var projectPath = FindProjectPath(dte.Solution?.Projects, uniqueName!);
            if (!string.IsNullOrWhiteSpace(projectPath) && File.Exists(projectPath))
                return projectPath;
        }

        return null;
    }

    private static string? FindProjectPath(Projects? projects, string uniqueName)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (projects == null)
            return null;

        foreach (Project project in projects)
        {
            var projectPath = FindProjectPath(project, uniqueName);
            if (!string.IsNullOrWhiteSpace(projectPath))
                return projectPath;
        }

        return null;
    }

    private static string? FindProjectPath(Project? project, string uniqueName)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (project == null)
            return null;

        if (string.Equals(project.UniqueName, uniqueName, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(project.FullName)
            && File.Exists(project.FullName))
            return project.FullName;

        var projectItems = project.ProjectItems;
        if (projectItems == null)
            return null;

        foreach (ProjectItem item in projectItems)
        {
            var nestedProject = item.SubProject;
            var nestedProjectPath = FindProjectPath(nestedProject, uniqueName);
            if (!string.IsNullOrWhiteSpace(nestedProjectPath))
                return nestedProjectPath;
        }

        return null;
    }

    private static string? ResolvePreviewHostPath(
        DTE2 dte,
        PreviewHostDefinition host,
        string projectPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var targetFrameworkDirectories = PreviewTargetFrameworkDiscovery.OrderDirectories(
            projectPath,
            host.TargetFrameworkDirectories);

        var extensionDirectory = Path.GetDirectoryName(typeof(NuriPreviewControl).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(extensionDirectory))
        {
            var installedHost = targetFrameworkDirectories
                .Select(targetFramework => Path.Combine(
                    extensionDirectory,
                    host.InstalledDirectoryName,
                    targetFramework,
                    host.ExecutableName))
                .Concat(new[]
                {
                    Path.Combine(
                        extensionDirectory,
                        host.InstalledDirectoryName,
                        host.ExecutableName)
                })
                .FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(installedHost))
                return installedHost;
        }

        var solutionPath = dte.Solution?.FullName;
        if (string.IsNullOrWhiteSpace(solutionPath))
            return null;

        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrWhiteSpace(solutionDirectory))
            return null;

        var configurations = new[] { "Debug", "Release" };
        var candidates = configurations
            .SelectMany(configuration => targetFrameworkDirectories.Select(targetFramework =>
                Path.Combine(
                    solutionDirectory,
                    "src",
                    host.SourceProjectDirectoryName,
                    "bin",
                    configuration,
                    targetFramework,
                    host.ExecutableName)));

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    private enum ShowWindowCommand
    {
        Show = 5
    }

    private static class PackageProvider
    {
        public static AsyncPackage? Package { get; set; }
    }
}
