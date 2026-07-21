using Duxel.Core;

namespace Nuri.Duxel;

public sealed record NuriDuxelPerformanceOptions
{
    public Action<NuriDuxelFrameTiming>? FrameCompleted { get; init; }

    public Action<NuriDuxelResizeMessage>? ResizeMessageReceived { get; init; }

    public Action<string>? DuxelLog { get; init; }

    public bool LogDuxelStartupTimings { get; init; } = true;

    public int DuxelLogEveryNFrames { get; init; } = 1;
}

public readonly record struct NuriDuxelResizeMessage(
    long StopwatchTimestamp,
    UiVector2 ClientSize);
