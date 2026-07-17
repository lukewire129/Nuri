# Nuri.Duxel Sample

Run the Windows sample normally:

```powershell
dotnet run --project "samples\Duxel\Nuri.DuxelSample\Nuri.DuxelSample.csproj" -c Debug
```

Run it with C# Hot Reload enabled:

```powershell
dotnet watch --project "samples\Duxel\Nuri.DuxelSample\Nuri.DuxelSample.csproj" run -c Debug
```

Supported metadata updates request a Duxel frame and rebuild the Nuri root while preserving hook state for stable component identities.

The sample selects Duxel's `UiTheme.Nord` preset through the fixed-theme `NuriApplication.Run(theme, rootFactory, ...)` overload. The same `UiTheme` is passed to the root factory and applied to the Duxel host, so a component can use palette colors without a `DuxelThemeController`. Use the controller overload only when the application needs runtime theme changes. The existing `theme:` parameter remains available for roots that do not need the selected palette, and omitting it follows the Windows light/dark app setting.
