using Duxel.Core;

namespace Nuri.Duxel;

public enum DuxelInputEventKind
{
    PointerMove,
    PointerDown,
    PointerUp,
    Wheel,
    KeyDown,
    KeyUp,
    TextInput,
    FocusGained,
    FocusLost,
    Resize
}

public readonly record struct DuxelInputEvent(
    long Sequence,
    long Timestamp,
    DuxelInputEventKind Kind,
    UiVector2 Position,
    UiVector2 Delta,
    int Code,
    bool IsRepeat,
    bool CapturedByNuri);

public readonly record struct DuxelScrollRegionSnapshot(
    string Id,
    UiRect Bounds,
    UiRect ScrollbarTrack,
    UiRect ScrollbarHandle,
    float Offset,
    float MaxOffset,
    int Order);

public sealed class DuxelInputEventQueue
{
    private readonly object _gate = new();
    private readonly List<DuxelInputEvent> _pending = new();
    private DuxelScrollRegionSnapshot[] _scrollRegions = [];
    private long _sequence;

    public bool HasPending
    {
        get
        {
            lock (_gate)
            {
                return _pending.Count > 0;
            }
        }
    }

    public void Enqueue(
        long timestamp,
        DuxelInputEventKind kind,
        UiVector2 position = default,
        UiVector2 delta = default,
        int code = 0,
        bool isRepeat = false,
        bool capturedByNuri = false)
    {
        lock (_gate)
        {
            var inputEvent = new DuxelInputEvent(
                ++_sequence,
                timestamp,
                kind,
                position,
                delta,
                code,
                isRepeat,
                capturedByNuri);
            if (_pending.Count > 0
                && IsCoalescible(kind)
                && _pending[^1].Kind == kind)
            {
                _pending[^1] = inputEvent;
                return;
            }

            _pending.Add(inputEvent);
        }
    }

    public DuxelInputEvent[] Drain()
    {
        lock (_gate)
        {
            if (_pending.Count == 0)
            {
                return [];
            }

            var events = _pending.ToArray();
            _pending.Clear();
            return events;
        }
    }

    public bool ShouldCaptureWheel(UiVector2 position)
    {
        lock (_gate)
        {
            return _scrollRegions.Any(region =>
                Contains(region.Bounds, position)
                && region.MaxOffset > 0f);
        }
    }

    public bool ShouldCapturePointer(UiVector2 position)
    {
        lock (_gate)
        {
            return _scrollRegions.Any(region =>
                region.MaxOffset > 0f
                && Contains(region.ScrollbarTrack, position));
        }
    }

    public bool TryGetScrollRegion(string id, out DuxelScrollRegionSnapshot region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        lock (_gate)
        {
            foreach (var candidate in _scrollRegions)
            {
                if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    region = candidate;
                    return true;
                }
            }
        }

        region = default;
        return false;
    }

    public IReadOnlyList<DuxelScrollRegionSnapshot> GetScrollRegions()
    {
        lock (_gate)
        {
            return _scrollRegions.ToArray();
        }
    }

    internal void PublishScrollRegions(IEnumerable<DuxelScrollRegionSnapshot> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        var snapshot = regions.ToArray();
        lock (_gate)
        {
            _scrollRegions = snapshot;
        }
    }

    private static bool IsCoalescible(DuxelInputEventKind kind)
    {
        return kind is DuxelInputEventKind.PointerMove or DuxelInputEventKind.Resize;
    }

    private static bool Contains(UiRect bounds, UiVector2 position)
    {
        return position.X >= bounds.X
            && position.X <= bounds.X + bounds.Width
            && position.Y >= bounds.Y
            && position.Y <= bounds.Y + bounds.Height;
    }
}
