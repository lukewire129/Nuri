# Nuri Formatting

`Nuri.Formatter` provides conservative formatting for Nuri UI trees in C#.
The Visual Studio extension registers a C# editor save command handler and runs the formatter immediately before a `.cs` document is saved.

## Scope

Formatting applies only to block-bodied `override IElement Render()` methods.
It does not reorder statements or fluent calls, and it does not format ordinary C# methods.
A return expression containing comments or preprocessor directives is left unchanged.

## Rules

- Put the `return` keyword on its own line.
- Indent the returned Nuri expression by four spaces.
- Expand child arguments of container factories such as `Div(...)` and `Grid(...)` one per line.
- Keep control content and its primary event together, such as `Button("Save", Save)`, even when the event is a lambda.
- Put container fluent calls at the same indentation as the container's closing parenthesis.
- Indent control fluent calls by four spaces under their receiver.
- Separate hooks, local calculations, local functions, and the returned UI with one blank line.
- Preserve the document's existing LF or CRLF line endings.
- Produce the same output when formatting an already formatted document.

```csharp
public override IElement Render()
{
    var (user, setUser) = useState(defaultUser);

    return
        Grid(
            Column(
                Text("Profile"),
                Text(user.Name)
            ),
            Button("Save", Save)
        );
}
```

The initial Visual Studio integration is intentionally always-on for eligible Nuri `Render()` methods. A settings surface can be added after the rules have been validated against real projects.
