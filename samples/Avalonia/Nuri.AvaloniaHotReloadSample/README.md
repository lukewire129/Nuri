# Nuri Avalonia Hot Reload Sample

Minimal Avalonia renderer smoke test for Nuri Core DSL and C# Hot Reload rebuild behavior.

Avalonia is retained as a regression baseline. The next UI backend development priority is Duxel.

Run it with:

```powershell
dotnet watch --project "samples\Nuri.AvaloniaHotReloadSample\Nuri.AvaloniaHotReloadSample.csproj" run -c Debug
```

Things to try while it is running:

- Change text in `HotReloadProbeComponent`.
- Change colors such as `#2563eb` or `#93c5fd`.
- Change font sizes or margins.
- Click the button before and after edits to confirm state/event flow still works.

This sample intentionally uses only the first Avalonia renderer slice: `Div`, `Text`, `Button`, basic styling, and click events.
