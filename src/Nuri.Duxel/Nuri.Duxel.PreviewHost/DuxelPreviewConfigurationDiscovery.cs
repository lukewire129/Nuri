using System.Reflection;
using Duxel.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nuri.Duxel.PreviewHost;

internal static class DuxelPreviewConfigurationDiscovery
{
    public static UiTheme? DiscoverTheme(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        foreach (var sourceFile in EnumerateSourceFiles(projectDirectory))
        {
            SyntaxNode root;
            try
            {
                root = CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile), path: sourceFile).GetRoot();
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (!IsNuriApplicationRun(invocation.Expression))
                    continue;

                var namedTheme = invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
                    string.Equals(argument.NameColon?.Name.Identifier.ValueText, "theme", StringComparison.Ordinal));
                if (TryResolveTheme(namedTheme?.Expression, out var namedThemeValue))
                    return namedThemeValue;

                var firstPositional = invocation.ArgumentList.Arguments.FirstOrDefault(argument => argument.NameColon is null);
                if (TryResolveTheme(firstPositional?.Expression, out var positionalThemeValue))
                    return positionalThemeValue;
            }
        }

        return null;
    }

    private static bool IsNuriApplicationRun(ExpressionSyntax expression)
    {
        if (expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var methodName = memberAccess.Name switch
        {
            GenericNameSyntax genericName => genericName.Identifier.ValueText,
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            _ => null
        };
        if (!string.Equals(methodName, "Run", StringComparison.Ordinal))
            return false;

        var receiver = memberAccess.Expression.ToString();
        return string.Equals(receiver, "NuriApplication", StringComparison.Ordinal)
            || receiver.EndsWith(".NuriApplication", StringComparison.Ordinal);
    }

    private static bool TryResolveTheme(ExpressionSyntax? expression, out UiTheme theme)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        if (expression is null)
            return Fail(out theme);

        var memberPath = expression.ToString().Replace("global::", string.Empty).Split('.');
        var typeIndex = Array.FindIndex(memberPath, part =>
            string.Equals(part, nameof(UiTheme), StringComparison.Ordinal)
            || string.Equals(part, nameof(UiCompiledDesign), StringComparison.Ordinal));
        if (typeIndex < 0)
            return Fail(out theme);

        var rootType = string.Equals(memberPath[typeIndex], nameof(UiTheme), StringComparison.Ordinal)
            ? typeof(UiTheme)
            : typeof(UiCompiledDesign);
        object? value = null;
        var valueType = rootType;
        var isStatic = true;
        const BindingFlags publicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        for (var index = typeIndex + 1; index < memberPath.Length; index++)
        {
            var memberName = memberPath[index];
            var property = valueType.GetProperty(memberName, publicMembers);
            if (property is not null)
            {
                value = property.GetValue(isStatic ? null : value);
            }
            else
            {
                var field = valueType.GetField(memberName, publicMembers);
                if (field is null)
                    return Fail(out theme);
                value = field.GetValue(isStatic ? null : value);
            }

            if (value is null)
                return Fail(out theme);

            valueType = value.GetType();
            isStatic = false;
        }

        if (value is UiTheme resolvedTheme)
        {
            theme = resolvedTheme;
            return true;
        }

        return Fail(out theme);
    }

    private static bool Fail(out UiTheme theme)
    {
        theme = default;
        return false;
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

    private static bool IsUnderBuildOutput(string path)
    {
        return path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
