# Nuri.Duxel Theme Gallery Sample

Run the gallery:

```powershell
dotnet run --project "samples\Duxel\Nuri.DuxelThemeGallerySample\Nuri.DuxelThemeGallerySample.csproj" -c Debug
```

The root-factory overload supplies a host-scoped `DuxelThemeController` to the component. Preset buttons use it to switch Duxel's runtime `UiTheme` palette. The Nuri component and its hook state remain mounted, so entered text, selections, and counters survive theme changes.

The gallery covers the controls currently materialized by Nuri.Duxel: text, buttons, text/password inputs, checkbox/radio/toggle inputs, Grid, and Scroll.
