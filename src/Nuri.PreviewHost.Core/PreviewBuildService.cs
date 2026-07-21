using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Nuri.PreviewHost;

public sealed class PreviewBuildService
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

    public async Task<PreviewBuildResult> BuildAsync(CancellationToken cancellationToken, bool preferRoslyn = true)
    {
        if (preferRoslyn)
        {
            var roslynResult = await BuildWithRoslynAsync(cancellationToken).ConfigureAwait(false);
            if (roslynResult.Succeeded)
                return roslynResult;

            var dotnetResult = await BuildWithDotNetAsync(cancellationToken).ConfigureAwait(false);
            if (dotnetResult.Succeeded)
            {
                return PreviewBuildResult.Success(
                    dotnetResult.AssemblyPath!,
                    "Roslyn compile failed, fell back to dotnet build." + Environment.NewLine + roslynResult.Log + Environment.NewLine + dotnetResult.Log);
            }

            return PreviewBuildResult.Failure(
                "Roslyn compile failed and dotnet build also failed." + Environment.NewLine + roslynResult.Log + Environment.NewLine + dotnetResult.Log);
        }

        return await BuildWithDotNetAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<PreviewBuildResult> BuildWithRoslynAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(() => CompileWithRoslyn(cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PreviewBuildResult.Failure("Roslyn compile threw an exception." + Environment.NewLine + ex);
        }
    }

    private PreviewBuildResult CompileWithRoslyn(CancellationToken cancellationToken)
    {
        var model = PreviewCompilationModel.Load(_projectPath, _configuration);
        var syntaxTrees = new List<SyntaxTree>();
        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse,
            kind: SourceCodeKind.Regular);

        if (model.ImplicitUsingsEnabled)
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                CreateGlobalUsingsSource(),
                parseOptions,
                path: "<NuriPreviewGlobalUsings>.g.cs",
                encoding: Encoding.UTF8));

        foreach (var sourceFile in model.SourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = File.ReadAllText(sourceFile);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(text, parseOptions, path: sourceFile, encoding: Encoding.UTF8));
        }

        var compilationOptions = new CSharpCompilationOptions(model.OutputKind)
            .WithOptimizationLevel(OptimizationLevel.Debug)
            .WithNullableContextOptions(model.NullableEnabled ? NullableContextOptions.Enable : NullableContextOptions.Disable)
            .WithAssemblyIdentityComparer(AssemblyIdentityComparer.Default);

        var compilation = CSharpCompilation.Create(
            model.AssemblyName,
            syntaxTrees,
            model.MetadataReferences,
            compilationOptions);

        var outputDirectory = Path.Combine(Path.GetTempPath(), "NuriPreviewHost", "Roslyn", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        var assemblyPath = Path.Combine(outputDirectory, model.AssemblyName + ".dll");
        var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");

        using var dllStream = File.Create(assemblyPath);
        using var pdbStream = File.Create(pdbPath);
        var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);
        var emitResult = compilation.Emit(dllStream, pdbStream, options: emitOptions, cancellationToken: cancellationToken);

        CopyDependencyAssemblies(model.DependencyAssemblyPaths, outputDirectory);

        var log = CreateRoslynLog(model.ProjectPath, emitResult.Diagnostics);
        if (!emitResult.Success)
            return PreviewBuildResult.Failure(log);

        return PreviewBuildResult.Success(assemblyPath, log);
    }

    private async Task<PreviewBuildResult> BuildWithDotNetAsync(CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _projectDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
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

    private static string CreateGlobalUsingsSource()
    {
        return """
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
""";
    }

    private static string CreateRoslynLog(string projectPath, IEnumerable<Diagnostic> diagnostics)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Roslyn compile succeeded for " + projectPath);

        foreach (var diagnostic in diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden))
        {
            builder.AppendLine(diagnostic.ToString());
        }

        return builder.ToString().TrimEnd();
    }

    private static void CopyDependencyAssemblies(IEnumerable<string> dependencyAssemblyPaths, string outputDirectory)
    {
        foreach (var dependencyAssemblyPath in dependencyAssemblyPaths)
        {
            if (!File.Exists(dependencyAssemblyPath))
                continue;

            var destinationPath = Path.Combine(outputDirectory, Path.GetFileName(dependencyAssemblyPath));
            File.Copy(dependencyAssemblyPath, destinationPath, overwrite: true);
        }
    }
}

public sealed class PreviewBuildResult
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

internal sealed class PreviewCompilationModel
{
    private PreviewCompilationModel(
        string projectPath,
        string projectDirectory,
        string assemblyName,
        string targetFramework,
        OutputKind outputKind,
        bool nullableEnabled,
        bool implicitUsingsEnabled,
        IReadOnlyList<string> sourceFiles,
        IReadOnlyList<MetadataReference> metadataReferences,
        IReadOnlyList<string> dependencyAssemblyPaths)
    {
        ProjectPath = projectPath;
        ProjectDirectory = projectDirectory;
        AssemblyName = assemblyName;
        TargetFramework = targetFramework;
        OutputKind = outputKind;
        NullableEnabled = nullableEnabled;
        ImplicitUsingsEnabled = implicitUsingsEnabled;
        SourceFiles = sourceFiles;
        MetadataReferences = metadataReferences;
        DependencyAssemblyPaths = dependencyAssemblyPaths;
    }

    public string ProjectPath { get; }

    public string ProjectDirectory { get; }

    public string AssemblyName { get; }

    public string TargetFramework { get; }
    public OutputKind OutputKind { get; }

    public bool NullableEnabled { get; }

    public bool ImplicitUsingsEnabled { get; }

    public IReadOnlyList<string> SourceFiles { get; }

    public IReadOnlyList<MetadataReference> MetadataReferences { get; }

    public IReadOnlyList<string> DependencyAssemblyPaths { get; }

    public static PreviewCompilationModel Load(string projectPath, string configuration)
    {
        var document = XDocument.Load(projectPath);
        var root = document.Root ?? throw new InvalidOperationException("Invalid project file.");
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        var assemblyName = ReadProperty(root, "AssemblyName");
        if (string.IsNullOrWhiteSpace(assemblyName))
            assemblyName = Path.GetFileNameWithoutExtension(projectPath);

        var targetFramework = ReadTargetFramework(root);
        var outputKind = ReadOutputKind(root);
        var nullableEnabled = string.Equals(ReadProperty(root, "Nullable"), "enable", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ReadProperty(root, "Nullable"), "annotations", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ReadProperty(root, "Nullable"), "warnings", StringComparison.OrdinalIgnoreCase);
        var implicitUsingsEnabled = string.Equals(ReadProperty(root, "ImplicitUsings"), "enable", StringComparison.OrdinalIgnoreCase);

        var sourceFiles = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsUnderBuildOutput(path))
            .Where(path => !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var references = CreateMetadataReferences(
            projectPath,
            root,
            configuration,
            targetFramework,
            assemblyName,
            out var dependencyAssemblyPaths);
        return new PreviewCompilationModel(
            projectPath,
            projectDirectory,
            assemblyName,
            targetFramework,
            outputKind,
            nullableEnabled,
            implicitUsingsEnabled,
            sourceFiles,
            references,
            dependencyAssemblyPaths);
    }

    private static IReadOnlyList<MetadataReference> CreateMetadataReferences(
        string projectPath,
        XElement root,
        string configuration,
        string targetFramework,
        string assemblyName,
        out IReadOnlyList<string> dependencyAssemblyPaths)
    {
        var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dependencyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddTrustedPlatformAssemblies(referencePaths);
        AddProjectReferences(projectPath, root, configuration, targetFramework, referencePaths, dependencyPaths);
        AddProjectOutputAssemblies(
            Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory(),
            configuration,
            targetFramework,
            assemblyName,
            referencePaths,
            dependencyPaths);
        dependencyAssemblyPaths = dependencyPaths.ToArray();

        return referencePaths
            .Where(File.Exists)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    private static void AddTrustedPlatformAssemblies(ISet<string> referencePaths)
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(tpa))
            return;

        foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (File.Exists(path))
                referencePaths.Add(path);
        }
    }

    private static void AddProjectReferences(
        string projectPath,
        XElement root,
        string configuration,
        string targetFramework,
        ISet<string> referencePaths,
        ISet<string> dependencyPaths)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        foreach (var projectReference in root.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
        {
            var include = projectReference.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
                continue;

            var referenceProjectPath = Path.GetFullPath(Path.Combine(projectDirectory, include));
            var referenceAssemblyPath = TryResolveProjectAssembly(referenceProjectPath, configuration, targetFramework);
            if (referenceAssemblyPath != null)
            {
                referencePaths.Add(referenceAssemblyPath);
                dependencyPaths.Add(referenceAssemblyPath);
            }
        }
    }

    private static void AddProjectOutputAssemblies(
        string projectDirectory,
        string configuration,
        string targetFramework,
        string assemblyName,
        ISet<string> referencePaths,
        ISet<string> dependencyPaths)
    {
        var outputDirectory = Path.Combine(projectDirectory, "bin", configuration, targetFramework);
        if (!Directory.Exists(outputDirectory))
            return;

        foreach (var assemblyPath in Directory.EnumerateFiles(outputDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(
                    Path.GetFileNameWithoutExtension(assemblyPath),
                    assemblyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = Path.GetFileName(assemblyPath);
            if (referencePaths.Any(existingPath =>
                    string.Equals(Path.GetFileName(existingPath), fileName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            referencePaths.Add(assemblyPath);
            dependencyPaths.Add(assemblyPath);
        }
    }

    private static OutputKind ReadOutputKind(XElement root)
    {
        return ReadProperty(root, "OutputType").ToUpperInvariant() switch
        {
            "EXE" => OutputKind.ConsoleApplication,
            "WINEXE" => OutputKind.WindowsApplication,
            _ => OutputKind.DynamicallyLinkedLibrary
        };
    }

    private static string? TryResolveProjectAssembly(string projectPath, string configuration, string targetFramework)
    {
        if (!File.Exists(projectPath))
            return null;

        var document = XDocument.Load(projectPath);
        var root = document.Root ?? throw new InvalidOperationException("Invalid project file.");
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        var assemblyName = ReadProperty(root, "AssemblyName");
        if (string.IsNullOrWhiteSpace(assemblyName))
            assemblyName = Path.GetFileNameWithoutExtension(projectPath);

        var referenceTargetFramework = ReadTargetFramework(root, targetFramework);
        return Path.Combine(projectDirectory, "bin", configuration, referenceTargetFramework, assemblyName + ".dll");
    }

    private static string ReadTargetFramework(XElement root, string preferredTargetFramework = "")
    {
        var targetFramework = ReadProperty(root, "TargetFramework");
        if (!string.IsNullOrWhiteSpace(targetFramework))
            return targetFramework;

        var targetFrameworks = ReadProperty(root, "TargetFrameworks");
        if (string.IsNullOrWhiteSpace(targetFrameworks))
            throw new InvalidOperationException("Project must declare TargetFramework or TargetFrameworks.");

        var candidates = targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!string.IsNullOrWhiteSpace(preferredTargetFramework))
        {
            var preferred = candidates.FirstOrDefault(candidate =>
                string.Equals(candidate, preferredTargetFramework, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(preferred))
                return preferred;
        }

        return candidates[0];
    }

    private static bool IsUnderBuildOutput(string path)
    {
        return path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
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
