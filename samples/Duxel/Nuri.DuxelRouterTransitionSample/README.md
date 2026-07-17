# Nuri Duxel Router Transition Sample

Run the sample with:

```powershell
dotnet run --project "samples\Duxel\Nuri.DuxelRouterTransitionSample\Nuri.DuxelRouterTransitionSample.csproj" -c Debug
```

This Duxel host links the same platform-neutral `RouterTransitionComponent` used by the WPF sample. The component owns its fade-out, keyed route replacement, fade-in, and cancellation policy through `useState`, `useEffect`, and a neutral opacity transition.
