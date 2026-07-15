using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Nuri.WPF.PreviewHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            var options = PreviewOptions.Parse(args);
            var application = new Application();
            application.Run(new PreviewWindow(options));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Nuri Preview Host", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

internal sealed class PreviewOptions
{
    private PreviewOptions(
        string projectPath,
        string? commandFilePath,
        string? statusFilePath,
        string? connectionFilePath,
        int captureFramesPerSecond,
        bool embedded,
        IntPtr parentHandle)
    {
        ProjectPath = projectPath;
        CommandFilePath = commandFilePath;
        StatusFilePath = statusFilePath;
        ConnectionFilePath = connectionFilePath;
        CaptureFramesPerSecond = captureFramesPerSecond;
        Embedded = embedded;
        ParentHandle = parentHandle;
    }

    public string ProjectPath { get; }

    public string? CommandFilePath { get; }

    public string? StatusFilePath { get; }

    public string? ConnectionFilePath { get; }

    public int CaptureFramesPerSecond { get; }

    public bool CaptureEnabled => !string.IsNullOrWhiteSpace(ConnectionFilePath);

    public bool Embedded { get; }
    public IntPtr ParentHandle { get; }
    public static PreviewOptions Parse(string[] args)
    {
        string? projectPath = null;
        string? commandFilePath = null;
        string? statusFilePath = null;
        string? connectionFilePath = null;
        var captureFramesPerSecond = 15;
        var embedded = false;
        IntPtr parentHandle = IntPtr.Zero;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--embedded", StringComparison.OrdinalIgnoreCase))
            {
                embedded = true;
                continue;
            }
            if (string.Equals (arg, "--parent-hwnd", StringComparison.OrdinalIgnoreCase))
            {
                parentHandle = new IntPtr (long.Parse (args[++i]));
                continue;
            }

            if (string.Equals(arg, "--project", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                projectPath = args[++i];
                continue;
            }

            if (string.Equals(arg, "--command-file", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                commandFilePath = args[++i];
                continue;
            }

            if (string.Equals(arg, "--status-file", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                statusFilePath = args[++i];
                continue;
            }

            if (string.Equals(arg, "--connection-file", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                connectionFilePath = args[++i];
                continue;
            }

            if (string.Equals(arg, "--capture-fps", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedFramesPerSecond))
                    captureFramesPerSecond = Math.Clamp(parsedFramesPerSecond, 1, 30);
                continue;
            }

            if (!arg.StartsWith("-", StringComparison.Ordinal) && projectPath == null)
                projectPath = arg;
        }

        projectPath ??= Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj").FirstOrDefault();
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new InvalidOperationException("Provide a project path with --project <path>.");

        var fullPath = Path.GetFullPath(projectPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Project file was not found.", fullPath);

        return new PreviewOptions(
            fullPath,
            commandFilePath,
            statusFilePath,
            connectionFilePath,
            captureFramesPerSecond,
            embedded,
            parentHandle);
    }
}
