using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Duxel.Core;
using Nuri.Runtime.Diagnostics;

namespace Nuri.Duxel;

[SupportedOSPlatform("windows")]
internal sealed class WindowsInputEventBridge : IDisposable
{
    private const uint WmSetFocus = 0x0007;
    private const uint WmKillFocus = 0x0008;
    private const uint WmSize = 0x0005;
    private const uint WmSizing = 0x0214;
    private const uint WmNcDestroy = 0x0082;
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const uint WmChar = 0x0102;
    private const uint WmSysKeyDown = 0x0104;
    private const uint WmSysKeyUp = 0x0105;
    private const uint WmMouseMove = 0x0200;
    private const uint WmLeftButtonDown = 0x0201;
    private const uint WmLeftButtonUp = 0x0202;
    private const uint WmRightButtonDown = 0x0204;
    private const uint WmRightButtonUp = 0x0205;
    private const uint WmMiddleButtonDown = 0x0207;
    private const uint WmMiddleButtonUp = 0x0208;
    private const uint WmMouseWheel = 0x020A;
    private const uint WmMouseHorizontalWheel = 0x020E;
    private static long _nextSubclassId;

    private readonly DuxelInputEventQueue _events;
    private readonly Action _requestFrame;
    private readonly Func<float>? _contentScaleProvider;
    private readonly Action<NuriDuxelResizeMessage>? _resizeMessageObserver;
    private readonly DebugKey? _debugKey;
    private readonly Action? _debugShortcut;
    private readonly SubclassProc _windowProc;
    private readonly nuint _subclassId;
    private nint _windowHandle;
    private float _clientWidth;
    private float _clientHeight;
    private bool _installed;
    private int _disposed;
    private bool _pointerCaptured;

    public WindowsInputEventBridge(
        DuxelInputEventQueue events,
        Action requestFrame,
        Func<float>? contentScaleProvider = null,
        Action<NuriDuxelResizeMessage>? resizeMessageObserver = null,
        DebugKey? debugKey = null,
        Action? debugShortcut = null)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _requestFrame = requestFrame ?? throw new ArgumentNullException(nameof(requestFrame));
        _contentScaleProvider = contentScaleProvider;
        _resizeMessageObserver = resizeMessageObserver;
        _debugKey = debugKey;
        _debugShortcut = debugShortcut;
        _windowProc = WindowProc;
        _subclassId = unchecked((nuint)Interlocked.Increment(ref _nextSubclassId));
    }

    public UiVector2? ClientAreaSize
    {
        get
        {
            var width = Volatile.Read(ref _clientWidth);
            var height = Volatile.Read(ref _clientHeight);
            return width > 0f && height > 0f ? new UiVector2(width, height) : null;
        }
    }

    public void Attach(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowHandle));
        }

        if (_installed)
        {
            throw new InvalidOperationException("The Nuri input bridge is already attached.");
        }

        if (!SetWindowSubclass(windowHandle, _windowProc, _subclassId, 0))
        {
            throw new InvalidOperationException(
                $"SetWindowSubclass failed with Win32 error {Marshal.GetLastWin32Error()}.");
        }

        _windowHandle = windowHandle;
        _installed = true;
        UpdateClientSize(windowHandle);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (_installed)
        {
            _ = RemoveWindowSubclass(_windowHandle, _windowProc, _subclassId);
            _installed = false;
            _windowHandle = 0;
            Volatile.Write(ref _clientWidth, 0f);
            Volatile.Write(ref _clientHeight, 0f);
        }
    }

    private nint WindowProc(
        nint windowHandle,
        uint message,
        nuint wParam,
        nint lParam,
        nuint subclassId,
        nuint referenceData)
    {
        _ = subclassId;
        _ = referenceData;
        var timestamp = Stopwatch.GetTimestamp();
        var captured = false;

        switch (message)
        {
            case WmMouseMove:
                captured = _pointerCaptured;
                Enqueue(
                    timestamp,
                    DuxelInputEventKind.PointerMove,
                    ClientPoint(windowHandle, lParam),
                    capturedByNuri: captured);
                break;
            case WmLeftButtonDown:
            {
                var position = ClientPoint(windowHandle, lParam);
                captured = _events.ShouldCapturePointer(position);
                _pointerCaptured = captured;
                if (captured)
                {
                    _ = SetCapture(windowHandle);
                }
                Enqueue(
                    timestamp,
                    DuxelInputEventKind.PointerDown,
                    position,
                    code: 0,
                    capturedByNuri: captured);
                break;
            }
            case WmLeftButtonUp:
                captured = _pointerCaptured;
                Enqueue(
                    timestamp,
                    DuxelInputEventKind.PointerUp,
                    ClientPoint(windowHandle, lParam),
                    code: 0,
                    capturedByNuri: captured);
                if (captured)
                {
                    _ = ReleaseCapture();
                }
                _pointerCaptured = false;
                break;
            case WmRightButtonDown:
                Enqueue(timestamp, DuxelInputEventKind.PointerDown, ClientPoint(windowHandle, lParam), code: 1);
                break;
            case WmRightButtonUp:
                Enqueue(timestamp, DuxelInputEventKind.PointerUp, ClientPoint(windowHandle, lParam), code: 1);
                break;
            case WmMiddleButtonDown:
                Enqueue(timestamp, DuxelInputEventKind.PointerDown, ClientPoint(windowHandle, lParam), code: 2);
                break;
            case WmMiddleButtonUp:
                Enqueue(timestamp, DuxelInputEventKind.PointerUp, ClientPoint(windowHandle, lParam), code: 2);
                break;
            case WmMouseWheel:
            case WmMouseHorizontalWheel:
            {
                var position = ScreenPoint(windowHandle, lParam);
                var delta = WheelDelta(wParam);
                var wheelDelta = message == WmMouseWheel
                    ? new UiVector2(0f, delta)
                    : new UiVector2(delta, 0f);
                // Capture vertical wheel samples by event-time position only. The published
                // offset is from the previous frame, so making a directional decision here
                // can drop a rapid down/up reversal before the renderer drains the queue.
                captured = message == WmMouseWheel
                    && _events.ShouldCaptureWheel(position);
                Enqueue(
                    timestamp,
                    DuxelInputEventKind.Wheel,
                    position,
                    wheelDelta,
                    capturedByNuri: captured);
                break;
            }
            case WmKeyDown:
            case WmSysKeyDown:
                var isRepeat = (lParam.ToInt64() & (1L << 30)) != 0;
                if (!isRepeat && IsDebugShortcut(wParam))
                {
                    _debugShortcut?.Invoke();
                    return 0;
                }
                Enqueue(
                    timestamp,
                    DuxelInputEventKind.KeyDown,
                    code: unchecked((int)wParam),
                    isRepeat: isRepeat);
                break;
            case WmKeyUp:
            case WmSysKeyUp:
                Enqueue(timestamp, DuxelInputEventKind.KeyUp, code: unchecked((int)wParam));
                break;
            case WmChar:
                Enqueue(timestamp, DuxelInputEventKind.TextInput, code: unchecked((int)wParam));
                break;
            case WmSetFocus:
                Enqueue(timestamp, DuxelInputEventKind.FocusGained);
                break;
            case WmKillFocus:
                Enqueue(timestamp, DuxelInputEventKind.FocusLost);
                break;
            case WmSizing:
                UpdateProposedClientSize(windowHandle, lParam);
                // Preserve preview sizing, then forward WM_SIZING so Duxel 0.2.12 can
                // present the matching frame before Windows applies the bounds. Consuming
                // it here makes Windows stretch the old frame, which appears as zooming.
                break;
            case WmSize:
                var clientSize = ClientSize(windowHandle, lParam);
                UpdateClientSize(clientSize);
                _resizeMessageObserver?.Invoke(new NuriDuxelResizeMessage(timestamp, clientSize));
                _events.Enqueue(
                    timestamp,
                    DuxelInputEventKind.Resize,
                    delta: clientSize);
                // DefSubclassProc forwards WM_SIZE to Duxel next. Duxel first publishes
                // the new platform size and then requests the frame; requesting here
                // would let the render thread present one frame with the previous size.
                break;
            case WmNcDestroy:
                _ = RemoveWindowSubclass(windowHandle, _windowProc, _subclassId);
                _installed = false;
                _windowHandle = 0;
                break;
        }

        return captured ? 0 : DefSubclassProc(windowHandle, message, wParam, lParam);
    }

    private bool IsDebugShortcut(nuint virtualKey)
    {
        return _debugKey is { } key
            && _debugShortcut is not null
            && virtualKey == unchecked((nuint)(0x70 + (int)key - 1));
    }

    private void Enqueue(
        long timestamp,
        DuxelInputEventKind kind,
        UiVector2 position = default,
        UiVector2 delta = default,
        int code = 0,
        bool isRepeat = false,
        bool capturedByNuri = false)
    {
        _events.Enqueue(timestamp, kind, position, delta, code, isRepeat, capturedByNuri);
        _requestFrame();
    }

    private UiVector2 ClientPoint(nint windowHandle, nint lParam)
    {
        var packed = lParam.ToInt64();
        return ToLogical(
            windowHandle,
            new Point(
                unchecked((short)(packed & 0xffff)),
                unchecked((short)((packed >> 16) & 0xffff))));
    }

    private UiVector2 ScreenPoint(nint windowHandle, nint lParam)
    {
        var packed = lParam.ToInt64();
        var point = new Point(
            unchecked((short)(packed & 0xffff)),
            unchecked((short)((packed >> 16) & 0xffff)));
        _ = ScreenToClient(windowHandle, ref point);
        return ToLogical(windowHandle, point);
    }

    private UiVector2 ClientSize(nint windowHandle, nint lParam)
    {
        var packed = lParam.ToInt64();
        var scale = EffectiveContentScale(windowHandle);
        return new UiVector2(
            (ushort)(packed & 0xffff) / scale,
            (ushort)((packed >> 16) & 0xffff) / scale);
    }

    private void UpdateClientSize(nint windowHandle)
    {
        if (!GetClientRect(windowHandle, out var rect))
        {
            return;
        }

        var scale = EffectiveContentScale(windowHandle);
        UpdateClientSize(new UiVector2(
            MathF.Max(0f, rect.Right - rect.Left) / scale,
            MathF.Max(0f, rect.Bottom - rect.Top) / scale));
    }

    private void UpdateProposedClientSize(nint windowHandle, nint sizingRectPointer)
    {
        if (sizingRectPointer == 0
            || !GetWindowRect(windowHandle, out var currentWindowRect)
            || !GetClientRect(windowHandle, out var currentClientRect))
        {
            return;
        }

        var sizingRect = Marshal.PtrToStructure<Rect>(sizingRectPointer);
        var nonClientWidth = Math.Max(
            0,
            currentWindowRect.Right - currentWindowRect.Left
                - (currentClientRect.Right - currentClientRect.Left));
        var nonClientHeight = Math.Max(
            0,
            currentWindowRect.Bottom - currentWindowRect.Top
                - (currentClientRect.Bottom - currentClientRect.Top));
        var scale = EffectiveContentScale(windowHandle);
        UpdateClientSize(new UiVector2(
            Math.Max(0, sizingRect.Right - sizingRect.Left - nonClientWidth) / scale,
            Math.Max(0, sizingRect.Bottom - sizingRect.Top - nonClientHeight) / scale));
    }


    private void UpdateClientSize(UiVector2 size)
    {
        Volatile.Write(ref _clientWidth, size.X);
        Volatile.Write(ref _clientHeight, size.Y);
    }

    private UiVector2 ToLogical(nint windowHandle, Point point)
    {
        var scale = EffectiveContentScale(windowHandle);
        return new UiVector2(point.X / scale, point.Y / scale);
    }

    private float EffectiveContentScale(nint windowHandle)
    {
        var dpi = GetDpiForWindow(windowHandle);
        var platformScale = dpi > 0 ? dpi / 96f : 1f;
        var previewScale = _contentScaleProvider is null
            ? 1f
            : Math.Clamp(_contentScaleProvider(), 0.05f, 4f);
        return platformScale * previewScale;
    }

    private static float WheelDelta(nuint wParam)
    {
        return unchecked((short)((wParam.ToUInt64() >> 16) & 0xffff)) / 120f;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint SubclassProc(
        nint windowHandle,
        uint message,
        nuint wParam,
        nint lParam,
        nuint subclassId,
        nuint referenceData);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(
        nint windowHandle,
        SubclassProc subclassProc,
        nuint subclassId,
        nuint referenceData);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(
        nint windowHandle,
        SubclassProc subclassProc,
        nuint subclassId);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(
        nint windowHandle,
        uint message,
        nuint wParam,
        nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(nint windowHandle, ref Point point);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint windowHandle, out Rect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint windowHandle, out Rect rect);

    [DllImport("user32.dll")]
    private static extern nint SetCapture(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();
}
