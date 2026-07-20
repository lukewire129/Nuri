using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Duxel.Core;

namespace Nuri.Duxel;

[SupportedOSPlatform("windows")]
internal sealed class WindowsInputEventBridge : IDisposable
{
    private const uint WmSetFocus = 0x0007;
    private const uint WmKillFocus = 0x0008;
    private const uint WmSize = 0x0005;
    private const uint WmCancelMode = 0x001F;
    private const uint WmSetCursor = 0x0020;
    private const uint WmCaptureChanged = 0x0215;
    private const uint WmNcLeftButtonDown = 0x00A1;
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
    private const int HitLeft = 10;
    private const int HitRight = 11;
    private const int HitTop = 12;
    private const int HitTopLeft = 13;
    private const int HitTopRight = 14;
    private const int HitBottom = 15;
    private const int HitBottomLeft = 16;
    private const int HitBottomRight = 17;
    private const int SystemMetricMinimumTrackWidth = 34;
    private const int SystemMetricMinimumTrackHeight = 35;
    private const uint SetWindowPositionNoZOrder = 0x0004;
    private const uint SetWindowPositionNoActivate = 0x0010;
    private const int CursorSizeNorthSouth = 32645;
    private const int CursorSizeWestEast = 32644;
    private const int CursorSizeNorthwestSoutheast = 32642;
    private const int CursorSizeNortheastSouthwest = 32643;
    private static long _nextSubclassId;

    private readonly DuxelInputEventQueue _events;
    private readonly Action _requestFrame;
    private readonly Func<float>? _contentScaleProvider;
    private readonly SubclassProc _windowProc;
    private readonly nuint _subclassId;
    private nint _windowHandle;
    private float _clientWidth;
    private float _clientHeight;
    private bool _installed;
    private bool _pointerCaptured;
    private int _resizeHitTest;
    private Point _resizeStartCursor;
    private Rect _resizeStartWindow;

    public WindowsInputEventBridge(
        DuxelInputEventQueue events,
        Action requestFrame,
        Func<float>? contentScaleProvider = null)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _requestFrame = requestFrame ?? throw new ArgumentNullException(nameof(requestFrame));
        _contentScaleProvider = contentScaleProvider;
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
        if (!_installed)
        {
            return;
        }

        _ = RemoveWindowSubclass(_windowHandle, _windowProc, _subclassId);
        _installed = false;
        _windowHandle = 0;
        Volatile.Write(ref _clientWidth, 0f);
        Volatile.Write(ref _clientHeight, 0f);
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

        if (_resizeHitTest != 0)
        {
            switch (message)
            {
                case WmMouseMove:
                    ResizeWindow(windowHandle);
                    return 0;
                case WmLeftButtonUp:
                    EndWindowResize(releaseCapture: true);
                    return 0;
                case WmSetCursor:
                    SetResizeCursor(_resizeHitTest);
                    return 1;
                case WmCancelMode:
                case WmCaptureChanged:
                    EndWindowResize(releaseCapture: false);
                    break;
            }
        }

        switch (message)
        {
            case WmNcLeftButtonDown:
                if (BeginWindowResize(windowHandle, unchecked((int)wParam)))
                {
                    return 0;
                }
                break;
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
                Enqueue(
                    timestamp,
                    DuxelInputEventKind.KeyDown,
                    code: unchecked((int)wParam),
                    isRepeat: (lParam.ToInt64() & (1L << 30)) != 0);
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
            case WmSize:
                var clientSize = ClientSize(windowHandle, lParam);
                UpdateClientSize(clientSize);
                Enqueue(
                    timestamp,
                    DuxelInputEventKind.Resize,
                    delta: clientSize);
                break;
            case WmNcDestroy:
                EndWindowResize(releaseCapture: false);
                _ = RemoveWindowSubclass(windowHandle, _windowProc, _subclassId);
                _installed = false;
                _windowHandle = 0;
                break;
        }

        return captured ? 0 : DefSubclassProc(windowHandle, message, wParam, lParam);
    }

    private bool BeginWindowResize(nint windowHandle, int hitTest)
    {
        if (!IsResizeHitTest(hitTest)
            || IsZoomed(windowHandle)
            || !GetWindowRect(windowHandle, out _resizeStartWindow)
            || !GetCursorPos(out _resizeStartCursor))
        {
            return false;
        }

        _resizeHitTest = hitTest;
        _ = SetCapture(windowHandle);
        SetResizeCursor(hitTest);
        return true;
    }

    private void ResizeWindow(nint windowHandle)
    {
        if (!GetCursorPos(out var cursor))
        {
            return;
        }

        var deltaX = cursor.X - _resizeStartCursor.X;
        var deltaY = cursor.Y - _resizeStartCursor.Y;
        var left = _resizeStartWindow.Left;
        var top = _resizeStartWindow.Top;
        var right = _resizeStartWindow.Right;
        var bottom = _resizeStartWindow.Bottom;

        if (_resizeHitTest is HitLeft or HitTopLeft or HitBottomLeft)
        {
            left += deltaX;
        }
        else if (_resizeHitTest is HitRight or HitTopRight or HitBottomRight)
        {
            right += deltaX;
        }

        if (_resizeHitTest is HitTop or HitTopLeft or HitTopRight)
        {
            top += deltaY;
        }
        else if (_resizeHitTest is HitBottom or HitBottomLeft or HitBottomRight)
        {
            bottom += deltaY;
        }

        var dpi = GetDpiForWindow(windowHandle);
        var effectiveDpi = dpi > 0 ? dpi : 96u;
        var minimumWidth = GetSystemMetricsForDpi(SystemMetricMinimumTrackWidth, effectiveDpi);
        var minimumHeight = GetSystemMetricsForDpi(SystemMetricMinimumTrackHeight, effectiveDpi);
        if (right - left < minimumWidth)
        {
            if (_resizeHitTest is HitLeft or HitTopLeft or HitBottomLeft)
            {
                left = right - minimumWidth;
            }
            else
            {
                right = left + minimumWidth;
            }
        }

        if (bottom - top < minimumHeight)
        {
            if (_resizeHitTest is HitTop or HitTopLeft or HitTopRight)
            {
                top = bottom - minimumHeight;
            }
            else
            {
                bottom = top + minimumHeight;
            }
        }

        _ = SetWindowPos(
            windowHandle,
            0,
            left,
            top,
            right - left,
            bottom - top,
            SetWindowPositionNoZOrder | SetWindowPositionNoActivate);
        SetResizeCursor(_resizeHitTest);
    }

    private void EndWindowResize(bool releaseCapture)
    {
        if (_resizeHitTest == 0)
        {
            return;
        }

        _resizeHitTest = 0;
        if (releaseCapture)
        {
            _ = ReleaseCapture();
        }
    }

    private static bool IsResizeHitTest(int hitTest)
    {
        return hitTest is >= HitLeft and <= HitBottomRight;
    }

    private static void SetResizeCursor(int hitTest)
    {
        var cursorId = hitTest switch
        {
            HitLeft or HitRight => CursorSizeWestEast,
            HitTop or HitBottom => CursorSizeNorthSouth,
            HitTopLeft or HitBottomRight => CursorSizeNorthwestSoutheast,
            HitTopRight or HitBottomLeft => CursorSizeNortheastSouthwest,
            _ => 0
        };
        if (cursorId != 0)
        {
            _ = SetCursor(LoadCursor(0, cursorId));
        }
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
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(nint windowHandle);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "LoadCursorW")]
    private static extern nint LoadCursor(nint instance, int cursorName);

    [DllImport("user32.dll")]
    private static extern nint SetCursor(nint cursor);

    [DllImport("user32.dll")]
    private static extern nint SetCapture(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();
}
