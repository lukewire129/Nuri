using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nuri.WPF.PreviewHost;

internal sealed class PreviewBuildService
{
    private readonly string _projectPath;
    private readonly string _projectDirectory;
    private readonly string _configuration;

    public PreviewBuildService(string projectPath, string configuration = "Debug")
    {
        _projectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
        _projectDirectory = Path.GetDirectoryName(_projectPath) ?? Directory.GetCurrentDirectory();
        _configuration = configuration;
    }

    public async Task<PreviewBuildResult> BuildAsync(CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _projectDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(_projectPath);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(_configuration);
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("minimal");

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
                output.AppendLine(args.Data);
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
                output.AppendLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var log = output.ToString();
        if (process.ExitCode != 0)
            return PreviewBuildResult.Failure(log);

        var targetPath = ResolveTargetAssemblyPath();
        if (!File.Exists(targetPath))
            return PreviewBuildResult.Failure(log + Environment.NewLine + "Build succeeded but target assembly was not found: " + targetPath);

        return PreviewBuildResult.Success(targetPath, log);
    }

    private string ResolveTargetAssemblyPath()
    {
        var document = XDocument.Load(_projectPath);
        var root = document.Root ?? throw new InvalidOperationException("Invalid project file.");
        var assemblyName = ReadProperty(root, "AssemblyName");
        if (string.IsNullOrWhiteSpace(assemblyName))
            assemblyName = Path.GetFileNameWithoutExtension(_projectPath);

        var targetFramework = ReadProperty(root, "TargetFramework");
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            var targetFrameworks = ReadProperty(root, "TargetFrameworks");
            targetFramework = targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        }

        if (string.IsNullOrWhiteSpace(targetFramework))
            throw new InvalidOperationException("Project must declare TargetFramework or TargetFrameworks.");

        return Path.Combine(_projectDirectory, "bin", _configuration, targetFramework, assemblyName + ".dll");
    }

    private static string ReadProperty(XElement root, string name)
    {
        foreach (var propertyGroup in root.Elements("PropertyGroup"))
        {
            var value = propertyGroup.Element(name)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }
}

internal sealed class PreviewBuildResult
{
    private PreviewBuildResult(bool succeeded, string? assemblyPath, string log)
    {
        Succeeded = succeeded;
        AssemblyPath = assemblyPath;
        Log = log;
    }

    public bool Succeeded { get; }

    public string? AssemblyPath { get; }

    public string Log { get; }

    public static PreviewBuildResult Success(string assemblyPath, string log)
    {
        return new PreviewBuildResult(true, assemblyPath, log);
    }

    public static PreviewBuildResult Failure(string log)
    {
        return new PreviewBuildResult(false, null, log);
    }
}
