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
    private PreviewOptions(string projectPath, string? componentName)
    {
        ProjectPath = projectPath;
        ComponentName = componentName;
    }

    public string ProjectPath { get; }

    public string? ComponentName { get; }

    public static PreviewOptions Parse(string[] args)
    {
        string? projectPath = null;
        string? componentName = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--project", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                projectPath = args[++i];
                continue;
            }

            if (string.Equals(arg, "--component", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                componentName = args[++i];
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

        return new PreviewOptions(fullPath, componentName);
    }
}
