using System.Runtime.InteropServices;

namespace Nuri.Duxel;

internal sealed class FirstFrameWindowVisibilityGate : IDisposable
{
    private const int SwHide = 0;
    private const int SwShow = 5;
    private static readonly TimeSpan FallbackDelay = TimeSpan.FromSeconds(5);

    private readonly object _gate = new();
    private Timer? _fallbackTimer;
    private nint _windowHandle;
    private bool _released;
    private bool _disposed;

    public void Attach(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return;
        }

        lock (_gate)
        {
            if (_disposed || _released)
            {
                return;
            }

            _windowHandle = windowHandle;
            _ = ShowWindow(windowHandle, SwHide);
            _fallbackTimer = new Timer(
                static state => ((FirstFrameWindowVisibilityGate)state!).Release(),
                this,
                FallbackDelay,
                Timeout.InfiniteTimeSpan);
        }
    }

    public void Release()
    {
        Timer? fallbackTimer;
        nint windowHandle;
        lock (_gate)
        {
            if (_disposed || _released)
            {
                return;
            }

            _released = true;
            fallbackTimer = _fallbackTimer;
            _fallbackTimer = null;
            windowHandle = _windowHandle;
        }

        fallbackTimer?.Dispose();
        if (windowHandle != nint.Zero)
        {
            _ = ShowWindowAsync(windowHandle, SwShow);
        }
    }

    public void Dispose()
    {
        Timer? fallbackTimer;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            fallbackTimer = _fallbackTimer;
            _fallbackTimer = null;
        }

        fallbackTimer?.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint windowHandle, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(nint windowHandle, int command);
}
