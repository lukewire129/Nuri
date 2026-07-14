# Nuri Multi-Window Sample

This WPF sample validates root registration, local hook-state isolation, shared `Store` updates, and cleanup when an individual window closes.

Run it with:

```powershell
dotnet run --project "samples\WPF\Nuri.MultiWindowSample\Nuri.MultiWindowSample.csproj" -c Release
```

Open two or more counter windows. Local increments should affect only their owning window, while shared increments should update the launcher and every open counter window. After closing one counter window, the remaining windows must continue updating normally and the console should contain exactly one cleanup message for the closed window. Closing the launcher MainWindow should close every remaining counter window and balance all mount/cleanup messages.
