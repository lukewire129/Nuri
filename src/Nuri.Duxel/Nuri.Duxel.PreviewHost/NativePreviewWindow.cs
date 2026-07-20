using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Nuri.Duxel.PreviewHost;

internal sealed class NativePreviewWindow
{
    private const int GwlStyle = -16;
    private const long WsChild = 0x40000000L;
    private const long WsPopup = 0x80000000L;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;
    private const int SwRestore = 9;

    private IntPtr _windowHandle;
    private IntPtr _parentHandle;
    private bool _embedded;

    public IntPtr WindowHandle => Volatile.Read(ref _windowHandle);

    public void Attach(IntPtr windowHandle, bool embedded, IntPtr parentHandle)
    {
        Volatile.Write(ref _windowHandle, windowHandle);
        _embedded = embedded && parentHandle != IntPtr.Zero;
        Volatile.Write(ref _parentHandle, _embedded ? parentHandle : IntPtr.Zero);
        if (!embedded || parentHandle == IntPtr.Zero)
            return;

        var style = GetWindowLongPtr(windowHandle, GwlStyle).ToInt64();
        style &= ~(WsPopup | WsCaption | WsThickFrame | WsSysMenu | WsMinimizeBox | WsMaximizeBox);
        style |= WsChild;
        SetWindowLongPtr(windowHandle, GwlStyle, new IntPtr(style));
        SetParent(windowHandle, parentHandle);

        if (GetClientRect(parentHandle, out var parentRect))
        {
            SetWindowPos(
                windowHandle,
                IntPtr.Zero,
                0,
                0,
                Math.Max(1, parentRect.Right - parentRect.Left),
                Math.Max(1, parentRect.Bottom - parentRect.Top),
                SwpNoZOrder | SwpNoActivate | SwpFrameChanged | SwpShowWindow);
        }
    }

    public float CalculateFitScale(int logicalWidth, int logicalHeight)
    {
        var windowHandle = WindowHandle;
        var parentHandle = Volatile.Read(ref _parentHandle);
        if (!_embedded
            || windowHandle == IntPtr.Zero
            || parentHandle == IntPtr.Zero
            || logicalWidth <= 0
            || logicalHeight <= 0
            || !GetClientRect(parentHandle, out var parentRect))
        {
            return 1f;
        }

        var dpiScale = GetWindowDpiScale(windowHandle);
        var availableWidth = Math.Max(1, parentRect.Right - parentRect.Left);
        var availableHeight = Math.Max(1, parentRect.Bottom - parentRect.Top);
        return MathF.Min(
            availableWidth / (logicalWidth * dpiScale),
            availableHeight / (logicalHeight * dpiScale));
    }

    public void ApplyPreviewScale(float previewScale, int logicalWidth, int logicalHeight)
    {
        var windowHandle = WindowHandle;
        var parentHandle = Volatile.Read(ref _parentHandle);
        if (!_embedded
            || windowHandle == IntPtr.Zero
            || parentHandle == IntPtr.Zero
            || !GetClientRect(parentHandle, out var parentRect))
        {
            return;
        }

        var dpiScale = GetWindowDpiScale(windowHandle);
        var width = Math.Max(1, (int)MathF.Round(logicalWidth * dpiScale * previewScale));
        var height = Math.Max(1, (int)MathF.Round(logicalHeight * dpiScale * previewScale));
        var parentWidth = Math.Max(1, parentRect.Right - parentRect.Left);
        var parentHeight = Math.Max(1, parentRect.Bottom - parentRect.Top);
        var x = (parentWidth - width) / 2;
        var y = (parentHeight - height) / 2;
        SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            x,
            y,
            width,
            height,
            SwpNoZOrder | SwpNoActivate | SwpShowWindow);
    }

    public void Focus()
    {
        var windowHandle = WindowHandle;
        if (windowHandle == IntPtr.Zero)
            return;

        ShowWindow(windowHandle, SwRestore);
        SetForegroundWindow(windowHandle);
        SetFocus(windowHandle);
    }

    public byte[]? CaptureJpeg()
    {
        var windowHandle = WindowHandle;
        if (windowHandle == IntPtr.Zero || !IsWindowVisible(windowHandle))
            return null;

        if (!GetClientRect(windowHandle, out var rect))
            return null;

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
            return null;

        var origin = new NativePoint();
        if (!ClientToScreen(windowHandle, ref origin))
            return null;

        using var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                origin.X,
                origin.Y,
                0,
                0,
                new Size(width, height),
                CopyPixelOperation.SourceCopy);
        }

        using var stream = new MemoryStream();
        var codec = ImageCodecInfo.GetImageEncoders().First(encoder => encoder.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 80L);
        bitmap.Save(stream, codec, parameters);
        return stream.ToArray();
    }

    private static IntPtr GetWindowLongPtr(IntPtr windowHandle, int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(windowHandle, index)
            : GetWindowLongPtr32(windowHandle, index);
    }

    private static IntPtr SetWindowLongPtr(IntPtr windowHandle, int index, IntPtr value)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(windowHandle, index, value)
            : SetWindowLongPtr32(windowHandle, index, value);
    }

    private static float GetWindowDpiScale(IntPtr windowHandle)
    {
        var dpi = GetDpiForWindow(windowHandle);
        return dpi > 0 ? dpi / 96f : 1f;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr windowHandle, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr windowHandle, ref NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr childWindow, IntPtr parentWindow);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr windowHandle, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr32(IntPtr windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr32(IntPtr windowHandle, int index, IntPtr value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr windowHandle, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr windowHandle);
}
