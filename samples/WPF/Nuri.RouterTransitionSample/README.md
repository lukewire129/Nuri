# Nuri WPF Router Transition Sample

Run the sample with:

```powershell
dotnet run --project "samples\WPF\Nuri.RouterTransitionSample\Nuri.RouterTransitionSample.csproj" -c Debug
```

The sample uses the standard `Router`. Its parent component composes the fade-out, keyed route replacement, fade-in, and cancellation policy from `useState`, `useEffect`, and a neutral opacity transition.
