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

The sample selects Duxel's `UiTheme.Nord` preset through `NuriApplication.Run(..., theme: ...)`. Omit `theme` to follow the Windows light/dark app setting, or choose another Duxel preset such as `UiTheme.Dracula`, `UiTheme.CatppuccinMocha`, or `UiTheme.GitHubDark`.
