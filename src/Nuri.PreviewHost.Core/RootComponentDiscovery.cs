using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nuri.PreviewHost;

public static class RootComponentDiscovery
{
    public static ComponentDescriptor ResolveRoot(string projectPath, IReadOnlyList<ComponentDescriptor> components)
    {
        if (components.Count == 0)
            throw new InvalidOperationException(
                $"No previewable Nuri components were found in '{projectPath}'. " +
                "Open a C# file in the intended Nuri project or select it as the Startup Project. " +
                "A previewable component must inherit Nuri.UI.Dsl.Component and have a public parameterless constructor.");

        var candidateNames = FindNuriApplicationRootNames(projectPath);
        foreach (var candidateName in candidateNames)
        {
            var match = FindComponent(components, candidateName);
            if (match != null)
                return match;
        }

        if (components.Count == 1)
            return components[0];

        throw new InvalidOperationException(
            "Could not determine the StartupProject root component. Add a NuriApplication.Run<T>, Show<T>, or Attach<T> call, or keep only one previewable component.");
    }

    private static IReadOnlyList<string> FindNuriApplicationRootNames(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        var names = new List<string>();

        foreach (var sourceFile in EnumerateSourceFiles(projectDirectory))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile), path: sourceFile);
            var root = tree.GetRoot();
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                    continue;

                if (memberAccess.Name is not GenericNameSyntax genericName)
                    continue;

                if (!IsNuriApplicationMethod(genericName.Identifier.ValueText))
                    continue;

                if (!IsNuriApplicationReceiver(memberAccess.Expression.ToString()))
                    continue;

                var typeArgument = genericName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArgument == null)
                    continue;

                var name = typeArgument.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }
        }

        return names;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string projectDirectory)
    {
        return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsUnderBuildOutput(path))
            .Where(path => !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static ComponentDescriptor? FindComponent(IReadOnlyList<ComponentDescriptor> components, string candidateName)
    {
        var matches = components
            .Where(component =>
                string.Equals(component.FullName, candidateName, StringComparison.Ordinal)
                || string.Equals(component.DisplayName, candidateName, StringComparison.Ordinal)
                || component.FullName.EndsWith("." + candidateName, StringComparison.Ordinal))
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    private static bool IsNuriApplicationMethod(string methodName)
    {
        return string.Equals(methodName, "Run", StringComparison.Ordinal)
            || string.Equals(methodName, "Show", StringComparison.Ordinal)
            || string.Equals(methodName, "Attach", StringComparison.Ordinal);
    }

    private static bool IsNuriApplicationReceiver(string receiver)
    {
        return string.Equals(receiver, "NuriApplication", StringComparison.Ordinal)
            || receiver.EndsWith(".NuriApplication", StringComparison.Ordinal);
    }

    private static bool IsUnderBuildOutput(string path)
    {
        return path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
