using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Nuri.VisualStudioPreview;

internal static class PreviewRendererSourceDiscovery
{
    private static readonly Regex WpfQualifiedCall = new Regex(
        @"\b(?:global::)?Nuri\.WPF\.NuriApplication\s*\.\s*(?:Create|Run|Show)\s*(?:<|\()",
        RegexOptions.Compiled);

    private static readonly Regex DuxelQualifiedCall = new Regex(
        @"\b(?:global::)?Nuri\.Duxel\.NuriApplication\s*\.\s*Run\s*(?:<|\()",
        RegexOptions.Compiled);

    private static readonly Regex WpfUsing = new Regex(
        @"\busing\s+(?:global::)?Nuri\.WPF\s*;",
        RegexOptions.Compiled);

    private static readonly Regex DuxelUsing = new Regex(
        @"\busing\s+(?:global::)?Nuri\.Duxel\s*;",
        RegexOptions.Compiled);

    private static readonly Regex WpfUniqueCreateCall = new Regex(
        @"\bNuriApplication\s*\.\s*Create\s*(?:<|\()",
        RegexOptions.Compiled);

    private static readonly Regex UnqualifiedRunOrShowCall = new Regex(
        @"\bNuriApplication\s*\.\s*(?:Run|Show)\s*(?:<|\()",
        RegexOptions.Compiled);

    public static string? Detect(string projectPath)
    {
        string projectDirectory;
        try
        {
            projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? Environment.CurrentDirectory;
        }
        catch (Exception)
        {
            return null;
        }

        var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourcePath in EnumerateSourceFiles(projectDirectory))
        {
            string source;
            try
            {
                source = File.ReadAllText(sourcePath);
            }
            catch (Exception)
            {
                continue;
            }

            if (WpfQualifiedCall.IsMatch(source) || WpfUniqueCreateCall.IsMatch(source))
                detected.Add("wpf");

            if (DuxelQualifiedCall.IsMatch(source))
                detected.Add("duxel");

            if (!UnqualifiedRunOrShowCall.IsMatch(source))
                continue;

            var usesWpf = WpfUsing.IsMatch(source);
            var usesDuxel = DuxelUsing.IsMatch(source);
            if (usesWpf && !usesDuxel)
                detected.Add("wpf");
            else if (usesDuxel && !usesWpf)
                detected.Add("duxel");
        }

        return detected.Count == 1 ? First(detected) : null;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string projectDirectory)
    {
        var pending = new Stack<string>();
        pending.Push(projectDirectory);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            string[] files;
            string[] directories;
            try
            {
                files = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly);
                directories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var file in files)
                yield return file;

            foreach (var childDirectory in directories)
            {
                var name = Path.GetFileName(childDirectory);
                if (string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                pending.Push(childDirectory);
            }
        }
    }

    private static string? First(HashSet<string> values)
    {
        foreach (var value in values)
            return value;

        return null;
    }
}
