# Duxel Performance Analysis

Use two harnesses to separate Nuri CPU work from the Duxel Windows/Vulkan path.

## Headless CPU comparison

```powershell
dotnet run --project "perf\Nuri.DuxelPerformance\Nuri.DuxelPerformance.csproj" -c Release -- --label current --size 1000
```

The first table retains the existing projection and patch-count scenarios. The first-frame table compares raw Duxel widget emission, projection of a prebuilt `VirtualEntry`, and a full Nuri component frame. All three stop at `GetDrawData`; they exclude Vulkan submission and present.

## Windows resize trace

Run each mode with the same size, resize count, VSync setting, display, DPI, and GPU:

```powershell
dotnet run --project "perf\Nuri.DuxelWindowsPerformance\Nuri.DuxelWindowsPerformance.csproj" -c Release -- --mode raw --size 1000 --resize-steps 60 --vsync true
dotnet run --project "perf\Nuri.DuxelWindowsPerformance\Nuri.DuxelWindowsPerformance.csproj" -c Release -- --mode projection --size 1000 --resize-steps 60 --vsync true
dotnet run --project "perf\Nuri.DuxelWindowsPerformance\Nuri.DuxelWindowsPerformance.csproj" -c Release -- --mode nuri --size 1000 --resize-steps 60 --vsync true
```

The harness alternates the native window between 1120x720 and 700x480, enables Duxel startup and per-frame logs, then prints p50/p95/p99. Nuri mode also reports runtime, projection, total frame, resize-to-projection latency, raw `WM_SIZE` count, and projected resize-frame count.

- Raw Duxel is slow: investigate Duxel frame scheduling, swapchain recreation, Vulkan submission, present, and the GPU/driver.
- Raw is fast but prebuilt projection is slow: investigate `DuxelVirtualEntryRenderer` layout and command generation.
- Prebuilt projection is fast but full Nuri is slow: investigate component render, virtual-tree creation, diff, commit, and effects.
- All CPU phases are fast but presentation is visibly late: correlate the Duxel log timestamps with PresentMon or PIX. Nuri cannot observe GPU completion through the current Duxel `0.2.11-preview` public API.

For application-specific tracing, pass `NuriDuxelPerformanceOptions` to `NuriApplication.Run`. Collection is opt-in; without an observer, `NuriDuxelScreen` does not read performance timestamps.

```csharp
NuriApplication.Run(
    root,
    performance: new NuriDuxelPerformanceOptions
    {
        FrameCompleted = timing => Record(timing),
        ResizeMessageReceived = message => Record(message),
        DuxelLog = Console.WriteLine
    });
```
