using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Nuri.VisualStudioPreview;

internal static class PreviewHostCatalog
{
    private static readonly PreviewHostDefinition[] Hosts =
    {
        new PreviewHostDefinition(
            id: "duxel",
            displayName: "Duxel",
            referenceNames: new[] { "Nuri.Duxel", "Nuri.Duxel.Windows" },
            executableName: "Nuri.Duxel.PreviewHost.exe",
            installedDirectoryName: "DuxelPreviewHost",
            sourceProjectDirectoryName: Path.Combine("Nuri.Duxel", "Nuri.Duxel.PreviewHost"),
            targetFrameworkDirectories: new[]  { "net8.0-windows", "net9.0-windows" },
            isFallback: false),
        new PreviewHostDefinition(
            id: "wpf",
            displayName: "WPF",
            referenceNames: new[] { "Nuri.WPF" },
            executableName: "Nuri.WPF.PreviewHost.exe",
            installedDirectoryName: "PreviewHost",
            sourceProjectDirectoryName: "Nuri.WPF.PreviewHost",
            targetFrameworkDirectories: new[] { "net8.0-windows", "net9.0-windows" },
            isFallback: true)
    };

    public static PreviewHostSelection Select(string projectPath)
    {
        var matches = Hosts
            .Where(host => PreviewProjectReferenceGraph.ReferencesAny(projectPath, host.ReferenceNames))
            .ToArray();

        if (matches.Length == 1)
            return PreviewHostSelection.Success(matches[0]);

        if (matches.Length > 1)
        {
            var sourceRendererId = PreviewRendererSourceDiscovery.Detect(projectPath);
            var sourceMatch = matches.SingleOrDefault(host =>
                string.Equals(host.Id, sourceRendererId, StringComparison.OrdinalIgnoreCase));
            if (sourceMatch != null)
                return PreviewHostSelection.Success(sourceMatch);

            var directMatches = matches
                .Where(host => PreviewProjectReferenceGraph.DirectlyReferencesAny(projectPath, host.ReferenceNames))
                .ToArray();
            if (directMatches.Length == 1)
                return PreviewHostSelection.Success(directMatches[0]);

            return PreviewHostSelection.Failure(
                "Multiple Nuri preview renderers were detected: "
                + string.Join(", ", matches.Select(host => host.DisplayName))
                + ". Qualify the startup call with Nuri.WPF.NuriApplication or Nuri.Duxel.NuriApplication, "
                + "or keep only one direct renderer reference.");
        }

        var fallback = Hosts.SingleOrDefault(host => host.IsFallback);
        return fallback != null
            ? PreviewHostSelection.Success(fallback)
            : PreviewHostSelection.Failure("No supported Nuri preview renderer was detected for this project.");
    }
}

internal sealed class PreviewHostDefinition
{
    public PreviewHostDefinition(
        string id,
        string displayName,
        IReadOnlyList<string> referenceNames,
        string executableName,
        string installedDirectoryName,
        string sourceProjectDirectoryName,
        IReadOnlyList<string> targetFrameworkDirectories,
        bool isFallback)
    {
        Id = id;
        DisplayName = displayName;
        ReferenceNames = referenceNames;
        ExecutableName = executableName;
        InstalledDirectoryName = installedDirectoryName;
        SourceProjectDirectoryName = sourceProjectDirectoryName;
        TargetFrameworkDirectories = targetFrameworkDirectories;
        IsFallback = isFallback;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<string> ReferenceNames { get; }

    public string ExecutableName { get; }

    public string InstalledDirectoryName { get; }

    public string SourceProjectDirectoryName { get; }

    public IReadOnlyList<string> TargetFrameworkDirectories { get; }

    public bool IsFallback { get; }
}

internal sealed class PreviewHostSelection
{
    private PreviewHostSelection(PreviewHostDefinition? host, string? errorMessage)
    {
        Host = host;
        ErrorMessage = errorMessage;
    }

    public PreviewHostDefinition? Host { get; }

    public string? ErrorMessage { get; }

    public static PreviewHostSelection Success(PreviewHostDefinition host)
    {
        return new PreviewHostSelection(host, null);
    }

    public static PreviewHostSelection Failure(string errorMessage)
    {
        return new PreviewHostSelection(null, errorMessage);
    }
}

internal static class PreviewTargetFrameworkDiscovery
{
    public static IReadOnlyList<string> OrderDirectories(
        string projectPath,
        IReadOnlyList<string> candidates)
    {
        var preferred = DetectDirectory(projectPath);
        if (preferred == null || !candidates.Contains(preferred, StringComparer.OrdinalIgnoreCase))
            return candidates;

        return candidates
            .OrderBy(candidate => string.Equals(candidate, preferred, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToArray();
    }

    private static string? DetectDirectory(string projectPath)
    {
        try
        {
            var document = XDocument.Load(projectPath);
            var targetFramework = ReadProperty(document, "TargetFramework");
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                targetFramework = ReadProperty(document, "TargetFrameworks")
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Trim())
                    .FirstOrDefault();
            }

            if (targetFramework == null)
                return null;

            if (targetFramework.StartsWith("net9.0", StringComparison.OrdinalIgnoreCase))
                return "net9.0-windows";
            if (targetFramework.StartsWith("net8.0", StringComparison.OrdinalIgnoreCase))
                return "net8.0-windows";
        }
        catch (Exception)
        {
        }

        return null;
    }

    private static string ReadProperty(XDocument document, string name)
    {
        return document
            .Descendants()
            .FirstOrDefault(element => string.Equals(
                element.Name.LocalName,
                name,
                StringComparison.OrdinalIgnoreCase))
            ?.Value
            ?.Trim()
            ?? string.Empty;
    }
}

internal static class PreviewProjectReferenceGraph
{
    public static bool DirectlyReferencesAny(string projectPath, IReadOnlyList<string> referenceNames)
    {
        var names = new HashSet<string>(referenceNames, StringComparer.OrdinalIgnoreCase);

        string fullProjectPath;
        try
        {
            fullProjectPath = Path.GetFullPath(projectPath);
        }
        catch (Exception)
        {
            return false;
        }

        if (!File.Exists(fullProjectPath))
            return false;

        XDocument document;
        try
        {
            document = XDocument.Load(fullProjectPath);
        }
        catch (Exception)
        {
            return false;
        }

        var projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? Environment.CurrentDirectory;
        foreach (var item in document.Descendants())
        {
            var itemName = item.Name.LocalName;
            var include = item.Attribute("Include")?.Value ?? item.Attribute("Update")?.Value;
            if (string.IsNullOrWhiteSpace(include))
                continue;

            if ((string.Equals(itemName, "PackageReference", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(itemName, "Reference", StringComparison.OrdinalIgnoreCase))
                && MatchesReference(include!, names))
            {
                return true;
            }

            if (!string.Equals(itemName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
                continue;

            var referencedProjectPath = TryResolveProjectReferencePath(projectDirectory, include!);
            if (referencedProjectPath != null
                && MatchesReference(Path.GetFileNameWithoutExtension(referencedProjectPath), names))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ReferencesAny(string projectPath, IReadOnlyList<string> referenceNames)
    {
        var names = new HashSet<string>(referenceNames, StringComparer.OrdinalIgnoreCase);
        var visitedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return ReferencesAny(projectPath, names, visitedProjects);
    }

    private static bool ReferencesAny(
        string projectPath,
        ISet<string> referenceNames,
        ISet<string> visitedProjects)
    {
        string fullProjectPath;
        try
        {
            fullProjectPath = Path.GetFullPath(projectPath);
        }
        catch (Exception)
        {
            return false;
        }

        if (!visitedProjects.Add(fullProjectPath) || !File.Exists(fullProjectPath))
            return false;

        XDocument document;
        try
        {
            document = XDocument.Load(fullProjectPath);
        }
        catch (Exception)
        {
            return false;
        }

        var projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? Environment.CurrentDirectory;
        foreach (var item in document.Descendants())
        {
            var itemName = item.Name.LocalName;
            var include = item.Attribute("Include")?.Value ?? item.Attribute("Update")?.Value;
            if (string.IsNullOrWhiteSpace(include))
                continue;

            if ((string.Equals(itemName, "PackageReference", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(itemName, "Reference", StringComparison.OrdinalIgnoreCase))
                && MatchesReference(include!, referenceNames))
            {
                return true;
            }

            if (!string.Equals(itemName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
                continue;

            var referencedProjectPath = TryResolveProjectReferencePath(projectDirectory, include!);
            if (referencedProjectPath == null)
                continue;

            if (MatchesReference(Path.GetFileNameWithoutExtension(referencedProjectPath), referenceNames)
                || ReferencesAny(referencedProjectPath, referenceNames, visitedProjects))
            {
                return true;
            }
        }

        return false;
    }

    private static string? TryResolveProjectReferencePath(string projectDirectory, string include)
    {
        try
        {
            return Path.GetFullPath(Path.Combine(projectDirectory, include));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool MatchesReference(string include, ISet<string> referenceNames)
    {
        var commaIndex = include.IndexOf(',');
        var simpleName = commaIndex >= 0 ? include.Substring(0, commaIndex) : include;
        return referenceNames.Contains(simpleName.Trim());
    }
}
