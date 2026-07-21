using Nuri.Formatting;

namespace Nuri.FormatterTests;

internal static class Program
{
    private static void Main()
    {
        FormatsNestedRenderTree();
        SeparatesHookAndReturnParagraphs();
        FormatsFluentCallsWithoutReorderingThem();
        IndentsNestedFluentCallsUnderTheirReceiver();
        KeepsControlContentAndEventTogether();
        IgnoresNonNuriMethods();
        PreservesCommentedExpressions();
        PreservesIncompleteExpressions();
        PreservesLineEndingsAndIsIdempotent();
        Console.WriteLine("Nuri.FormatterTests passed.");
    }

    private static void FormatsNestedRenderTree()
    {
        const string source = """
            public override IElement Render()
            {
                return Grid(Column(Text("Profile"), Text(user.Name)), Button("Save", Save));
            }
            """;
        const string expected = """
            public override IElement Render()
            {
                return
                    Grid(
                        Column(
                            Text("Profile"),
                            Text(user.Name)
                        ),
                        Button("Save", Save)
                    );
            }
            """;

        AssertEqual(expected, NuriCodeFormatter.Format(source), "Nested Nuri elements should use structural indentation.");
    }

    private static void SeparatesHookAndReturnParagraphs()
    {
        const string source = """
            public override IElement Render()
            {
                var (user, setUser) = useState(defaultUser);
                var title = user.Name;
                return Text(title);
            }
            """;
        const string expected = """
            public override IElement Render()
            {
                var (user, setUser) = useState(defaultUser);

                var title = user.Name;

                return
                    Text(title);
            }
            """;

        AssertEqual(expected, NuriCodeFormatter.Format(source), "Hooks, local calculations, and UI returns should form paragraphs.");
    }

    private static void FormatsFluentCallsWithoutReorderingThem()
    {
        const string source = """
            public override IElement Render()
            {
                return Grid(Text("Profile"), Button("Save", Save)).Rows(Auto, Star).Padding(12).Key("profile");
            }
            """;
        const string expected = """
            public override IElement Render()
            {
                return
                    Grid(
                        Text("Profile"),
                        Button("Save", Save)
                    )
                    .Rows(Auto, Star)
                    .Padding(12)
                    .Key("profile");
            }
            """;

        AssertEqual(expected, NuriCodeFormatter.Format(source), "Fluent calls should wrap in their original order.");
    }

    private static void IndentsNestedFluentCallsUnderTheirReceiver()
    {
        const string source = """
            public override IElement Render()
            {
                return Div(Text("Nuri.Duxel layout integration!!!!").FontSize(24).FontColor("#7DD3FC"), Button("Save", Save));
            }
            """;
        const string expected = """
            public override IElement Render()
            {
                return
                    Div(
                        Text("Nuri.Duxel layout integration!!!!")
                            .FontSize(24)
                            .FontColor("#7DD3FC"),
                        Button("Save", Save)
                    );
            }
            """;

        AssertEqual(expected, NuriCodeFormatter.Format(source), "Nested fluent calls should be indented under their receiver.");
    }

    private static void KeepsControlContentAndEventTogether()
    {
        const string source = """
            public override IElement Render()
            {
                return Div(Button("Increment", () => setCount(current => enabled ? current + 1 : current)).Size(120, 34).Background("#0369A1")).Padding(16).Spacing(14);
            }
            """;
        const string expected = """
            public override IElement Render()
            {
                return
                    Div(
                        Button("Increment", () => setCount(current => enabled ? current + 1 : current))
                            .Size(120, 34)
                            .Background("#0369A1")
                    )
                    .Padding(16)
                    .Spacing(14);
            }
            """;

        AssertEqual(expected, NuriCodeFormatter.Format(source), "Control content and its primary event should remain one signature-shaped call.");
    }

    private static void IgnoresNonNuriMethods()
    {
        const string source = """
            public object Build()
            {
                return Grid(Text("Profile"));
            }
            """;

        AssertEqual(source, NuriCodeFormatter.Format(source), "Ordinary C# methods must remain untouched.");
    }

    private static void PreservesCommentedExpressions()
    {
        const string source = """
            public override IElement Render()
            {
                return Grid(
                    // The title stays first.
                    Text("Profile"),
                    Button("Save", Save));
            }
            """;

        AssertEqual(source, NuriCodeFormatter.Format(source), "Comments inside UI expressions must not be moved.");
    }

    private static void PreservesIncompleteExpressions()
    {
        const string source = """
            public override IElement Render()
            {
                return Grid(Text("Profile")
            }
            """;

        AssertEqual(source, NuriCodeFormatter.Format(source), "Incomplete UI expressions must remain editable.");
    }

    private static void PreservesLineEndingsAndIsIdempotent()
    {
        const string source = "public override IElement Render()\r\n{\r\n    return Grid(Text(\"Profile\"), Button(\"Save\", Save));\r\n}";
        var once = NuriCodeFormatter.Format(source);
        var twice = NuriCodeFormatter.Format(once);

        AssertEqual(once, twice, "Formatting should be idempotent.");
        AssertEqual(false, once.Replace("\r\n", string.Empty).Contains('\n'), "Formatting should preserve CRLF line endings.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
        }
    }
}
