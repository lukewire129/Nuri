using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Nuri.WPF.PreviewHost;

internal sealed class PreviewAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PreviewAssemblyLoadContext(string mainAssemblyPath)
        : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is "Nuri" or "Nuri.WPF")
            return null;

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
