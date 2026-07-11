# Nuri Command Palette Sample

Small Command Palette sample for validating Nuri state updates, controlled `TextBox` input, keyboard events, filtered list rendering, keyed children, and keyboard-driven interaction.

Run it with:

```powershell
dotnet run --project "samples\Nuri.CommandPaletteSample\Nuri.CommandPaletteSample.csproj" -c Release
```

Behavior:

- Type in the search box to filter command titles.
- Use Up/Down to move the selected command.
- Press Enter to execute the selected command.
- Press Esc to clear the query and reset selection.

The sample intentionally keeps all behavior in `CommandPaletteComponent` so the state flow is easy to read.

## Known Issues

Core:

- `OnKeyDown` currently exposes only a minimal `KeyboardKey` enum for this sample (`Up`, `Down`, `Enter`, `Escape`, `Unknown`). It does not yet model modifiers, text input, repeat state, or handled/prevent-default semantics.
- `AutoFocus()` is represented as a neutral property, but Core does not define lifecycle timing semantics beyond carrying the property in the virtual tree.

WPF adapter:

- `AutoFocus()` is materialized by focusing the control after WPF `Loaded`; this is adapter-specific behavior and future renderers need their own materialization.
- Controlled `TextBox` updates now avoid assigning the same text value repeatedly to reduce caret and redundant `TextChanged` issues. More complete caret/selection preservation is still not modeled in Core.
