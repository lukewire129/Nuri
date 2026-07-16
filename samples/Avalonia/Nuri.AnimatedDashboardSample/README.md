# Nuri Animated Dashboard Sample

This sample uses one Core-neutral component with the WPF, Avalonia, and Duxel renderers.

This WPF/Avalonia path remains a regression baseline. New backend animation expansion should target Duxel first.

```powershell
dotnet run --project "samples\Avalonia\Nuri.AnimatedDashboardSample\Nuri.AnimatedDashboardSample.csproj" -c Release
dotnet run --project "samples\Avalonia\Nuri.AnimatedDashboardSample\Nuri.AnimatedDashboardSample.csproj" -c Release -- --avalonia
dotnet run --project "samples\Duxel\Nuri.DuxelAnimatedDashboardSample\Nuri.DuxelAnimatedDashboardSample.csproj" -c Release
```

Use the button repeatedly to exercise opacity transition interruption and replacement.
