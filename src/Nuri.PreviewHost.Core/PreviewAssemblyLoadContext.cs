using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Nuri.PreviewHost;

public sealed class PreviewAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver? _resolver;
    private readonly string _mainAssemblyDirectory;
    private readonly HashSet<string> _sharedAssemblyNames;

    public PreviewAssemblyLoadContext(
        string mainAssemblyPath,
        IEnumerable<string>? sharedAssemblyNames = null)
        : base(isCollectible: true)
    {
        _mainAssemblyDirectory = Path.GetDirectoryName(mainAssemblyPath) ?? Directory.GetCurrentDirectory();
        _sharedAssemblyNames = new HashSet<string>(
            sharedAssemblyNames ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        var depsFilePath = Path.ChangeExtension(mainAssemblyPath, ".deps.json");
        if (File.Exists(depsFilePath))
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name != null && _sharedAssemblyNames.Contains(assemblyName.Name))
            return null;

        if (_resolver == null)
        {
            var candidatePath = Path.Combine(_mainAssemblyDirectory, assemblyName.Name + ".dll");
            return File.Exists(candidatePath) ? LoadFromAssemblyPath(candidatePath) : null;
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath == null ? null : LoadFromAssemblyPath(assemblyPath);
    }

    public static string ShadowCopy(string assemblyPath)
    {
        var sourceDirectory = Path.GetDirectoryName(assemblyPath) ?? throw new InvalidOperationException("Assembly path has no directory.");
        var destinationDirectory = Path.Combine(Path.GetTempPath(), "NuriPreviewHost", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory))
        {
            var extension = Path.GetExtension(file);
            if (!IsRuntimeFile(extension))
                continue;

            File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), overwrite: true);
        }

        return Path.Combine(destinationDirectory, Path.GetFileName(assemblyPath));
    }

    private static bool IsRuntimeFile(string extension)
    {
        return extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".deps.json", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase);
    }
}
