using Duxel.Core;

namespace Nuri.Duxel;

public readonly record struct NuriDuxelFrameTiming(
    long FrameNumber,
    bool IsInitialFrame,
    bool HadRuntimeUpdate,
    bool HadResizeInput,
    int InputEventCount,
    int PatchCount,
    UiVector2 ViewportSize,
    TimeSpan RuntimeUpdateDuration,
    TimeSpan ProjectionDuration,
    TimeSpan CommitDuration,
    TimeSpan EffectDuration,
    TimeSpan TotalDuration,
    TimeSpan? ResizeToProjectionDuration);
