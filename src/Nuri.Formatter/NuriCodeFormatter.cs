using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nuri.Formatting;

public static class NuriCodeFormatter
{
    private const int IndentSize = 4;
    private static readonly HashSet<string> ContainerFactories = new HashSet<string>(StringComparer.Ordinal)
    {
        "Column",
        "Div",
        "Grid",
        "Panel",
        "Row",
        "Scroll",
        "Stack"
    };

    public static string Format(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var newLine = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var changes = new List<SourceChange>();
        var spacingStarts = new HashSet<int>();

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(IsNuriRenderMethod))
        {
            if (method.Body == null)
            {
                continue;
            }

            AddParagraphSpacing(source, method.Body, newLine, changes, spacingStarts);

            foreach (var returnStatement in method.Body.DescendantNodes().OfType<ReturnStatementSyntax>())
            {
                if (returnStatement.Expression is not InvocationExpressionSyntax expression ||
                    BelongsToNestedFunction(returnStatement, method) ||
                    returnStatement.SemicolonToken.IsMissing ||
                    expression.ContainsDiagnostics ||
                    ContainsProtectedTrivia(expression))
                {
                    continue;
                }

                var indent = GetLineIndent(source, returnStatement.SpanStart);
                var expressionIndent = indent + new string(' ', IndentSize);
                var formattedExpression = DslExpressionWriter.Format(expression, expressionIndent, newLine);
                var replacement = "return" + newLine + formattedExpression + ";";
                var length = returnStatement.SemicolonToken.Span.End - returnStatement.SpanStart;

                changes.Add(new SourceChange(returnStatement.SpanStart, length, replacement));
            }
        }

        foreach (var change in changes
                     .OrderByDescending(change => change.Start)
                     .ThenByDescending(change => change.Length))
        {
            source = source.Remove(change.Start, change.Length).Insert(change.Start, change.Text);
        }

        return source;
    }

    private static bool IsNuriRenderMethod(MethodDeclarationSyntax method)
    {
        if (method.Identifier.ValueText != "Render" ||
            !method.Modifiers.Any(SyntaxKind.OverrideKeyword))
        {
            return false;
        }

        var returnType = method.ReturnType.ToString();
        return returnType == "IElement" || returnType.EndsWith(".IElement", StringComparison.Ordinal);
    }

    private static bool BelongsToNestedFunction(ReturnStatementSyntax statement, MethodDeclarationSyntax method)
    {
        var owner = statement.Ancestors()
            .FirstOrDefault(node => node is MethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);
        return !ReferenceEquals(owner, method);
    }

    private static bool ContainsProtectedTrivia(SyntaxNode node)
    {
        return node.DescendantTrivia(descendIntoTrivia: true).Any(trivia =>
            trivia.IsDirective ||
            trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

    private static void AddParagraphSpacing(
        string source,
        BlockSyntax body,
        string newLine,
        ICollection<SourceChange> changes,
        ISet<int> spacingStarts)
    {
        for (var index = 1; index < body.Statements.Count; index++)
        {
            var previous = body.Statements[index - 1];
            var current = body.Statements[index];
            if (!NeedsParagraphBreak(previous, current))
            {
                continue;
            }

            var gapStart = previous.Span.End;
            var gapLength = current.SpanStart - gapStart;
            if (gapLength < 0 || !IsWhitespaceOnly(source, gapStart, gapLength) || !spacingStarts.Add(gapStart))
            {
                continue;
            }

            var indent = GetLineIndent(source, current.SpanStart);
            changes.Add(new SourceChange(gapStart, gapLength, newLine + newLine + indent));
        }
    }

    private static bool NeedsParagraphBreak(StatementSyntax previous, StatementSyntax current)
    {
        var previousKind = Classify(previous);
        var currentKind = Classify(current);

        return currentKind == StatementKind.Return ||
               currentKind == StatementKind.LocalFunction ||
               previousKind == StatementKind.LocalFunction ||
               previousKind == StatementKind.Hook && currentKind != StatementKind.Hook;
    }

    private static StatementKind Classify(StatementSyntax statement)
    {
        if (statement is ReturnStatementSyntax)
        {
            return StatementKind.Return;
        }

        if (statement is LocalFunctionStatementSyntax)
        {
            return StatementKind.LocalFunction;
        }

        if (statement.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
                GetInvokedName(invocation).StartsWith("use", StringComparison.Ordinal)))
        {
            return StatementKind.Hook;
        }

        return StatementKind.Other;
    }

    private static string GetInvokedName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            _ => string.Empty
        };
    }

    private static bool IsWhitespaceOnly(string source, int start, int length)
    {
        for (var index = start; index < start + length; index++)
        {
            if (!char.IsWhiteSpace(source[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetLineIndent(string source, int position)
    {
        var lineStart = position;
        while (lineStart > 0 && source[lineStart - 1] != '\n' && source[lineStart - 1] != '\r')
        {
            lineStart--;
        }

        var index = lineStart;
        while (index < position && (source[index] == ' ' || source[index] == '\t'))
        {
            index++;
        }

        return source.Substring(lineStart, index - lineStart);
    }

    private enum StatementKind
    {
        Other,
        Hook,
        LocalFunction,
        Return
    }

    private readonly struct SourceChange
    {
        public SourceChange(int start, int length, string text)
        {
            Start = start;
            Length = length;
            Text = text;
        }

        public int Start { get; }

        public int Length { get; }

        public string Text { get; }
    }

    private static class DslExpressionWriter
    {
        public static string Format(InvocationExpressionSyntax expression, string indent, string newLine)
        {
            var builder = new StringBuilder();
            WriteInvocation(builder, expression, indent, newLine);
            return builder.ToString();
        }

        private static void WriteInvocation(
            StringBuilder builder,
            InvocationExpressionSyntax invocation,
            string indent,
            string newLine)
        {
            if (TrySplitFluentChain(invocation, out var root, out var calls))
            {
                WriteInvocationCore(builder, root, indent, newLine);
                foreach (var call in calls)
                {
                    var callIndent = IsContainerInvocation(root)
                        ? indent
                        : indent + new string(' ', IndentSize);
                    builder.Append(newLine);
                    builder.Append(callIndent);
                    builder.Append('.');
                    builder.Append(call.Name.WithoutTrivia().ToString());
                    WriteArgumentList(builder, call.Arguments, callIndent, newLine, forceMultiline: false);
                }

                return;
            }

            WriteInvocationCore(builder, invocation, indent, newLine);
        }

        private static void WriteInvocationCore(
            StringBuilder builder,
            InvocationExpressionSyntax invocation,
            string indent,
            string newLine)
        {
            builder.Append(indent);
            builder.Append(invocation.Expression.WithoutTrivia().NormalizeWhitespace().ToFullString());
            WriteArgumentList(builder, invocation.ArgumentList, indent, newLine, IsContainerInvocation(invocation));
        }

        private static void WriteArgumentList(
            StringBuilder builder,
            ArgumentListSyntax arguments,
            string indent,
            string newLine,
            bool forceMultiline)
        {
            if (arguments.Arguments.Count == 0)
            {
                builder.Append("()");
                return;
            }

            if (!forceMultiline)
            {
                builder.Append(arguments.WithoutTrivia().NormalizeWhitespace().ToFullString());
                return;
            }

            var argumentIndent = indent + new string(' ', IndentSize);
            builder.Append('(');
            for (var index = 0; index < arguments.Arguments.Count; index++)
            {
                builder.Append(newLine);
                WriteArgument(builder, arguments.Arguments[index], argumentIndent, newLine);
                if (index < arguments.Arguments.Count - 1)
                {
                    builder.Append(',');
                }
            }

            builder.Append(newLine);
            builder.Append(indent);
            builder.Append(')');
        }

        private static void WriteArgument(
            StringBuilder builder,
            ArgumentSyntax argument,
            string indent,
            string newLine)
        {
            builder.Append(indent);
            if (argument.NameColon != null)
            {
                builder.Append(argument.NameColon.Name.Identifier.ValueText);
                builder.Append(": ");
            }

            if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
            {
                builder.Append(argument.RefKindKeyword.ValueText);
                builder.Append(' ');
            }

            if (argument.Expression is InvocationExpressionSyntax nestedInvocation)
            {
                var nested = new StringBuilder();
                WriteInvocation(nested, nestedInvocation, indent, newLine);
                var nestedText = nested.ToString();
                builder.Append(nestedText.Substring(indent.Length));
                return;
            }

            var normalized = argument.Expression.WithoutTrivia().NormalizeWhitespace("    ", newLine).ToFullString();
            builder.Append(IndentContinuationLines(normalized, indent, newLine));
        }

        private static string IndentContinuationLines(string value, string indent, string newLine)
        {
            return value.Replace(newLine, newLine + indent);
        }

        private static bool IsContainerInvocation(InvocationExpressionSyntax invocation)
        {
            var name = invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                _ => string.Empty
            };

            return ContainerFactories.Contains(name);
        }

        private static bool TrySplitFluentChain(
            InvocationExpressionSyntax invocation,
            out InvocationExpressionSyntax root,
            out IReadOnlyList<FluentCall> calls)
        {
            var reversed = new List<FluentCall>();
            var current = invocation;

            while (current.Expression is MemberAccessExpressionSyntax member &&
                   member.Expression is InvocationExpressionSyntax receiver)
            {
                reversed.Add(new FluentCall(member.Name, current.ArgumentList));
                current = receiver;
            }

            reversed.Reverse();
            root = current;
            calls = reversed;
            return reversed.Count > 0;
        }

        private readonly struct FluentCall
        {
            public FluentCall(SimpleNameSyntax name, ArgumentListSyntax arguments)
            {
                Name = name;
                Arguments = arguments;
            }

            public SimpleNameSyntax Name { get; }

            public ArgumentListSyntax Arguments { get; }
        }
    }
}
