using Duxel.Windows.App;
using Nuri.SimplyShare.Features.Chat;
using Nuri.SimplyShare.Features.Discovery;
using Nuri.SimplyShare.Features.Settings;

namespace Nuri.SimplyShare.App;

internal sealed class DuxelSimplyShareHost : ISimplyShareHost, IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, DuxelModelessWindow> _chatWindows = new();
    private DuxelModelessWindow? _settingsWindow;
    private IntPtr _mainWindowHandle;
    private bool _disposed;

    public void AttachMainWindow(IntPtr windowHandle)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            _mainWindowHandle = windowHandle;
        }
    }

    public void OpenSettings()
    {
        IntPtr ownerWindowHandle;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_settingsWindow is { IsClosed: false } existing)
            {
                existing.Restore();
                return;
            }

            ownerWindowHandle = _mainWindowHandle;
        }

        DuxelModelessWindow? created = null;
        created = NuriApplication.ShowModeless(
            close => new SettingsWindowComponent(close),
            title: "SimplyShare Settings",
            width: 520,
            height: 600,
            ownerWindowHandle: ownerWindowHandle,
            closed: () => ClearSettingsWindow(created));

        lock (_gate)
        {
            if (_disposed || created.IsClosed)
            {
                created.Dispose();
                return;
            }

            _settingsWindow = created;
        }
    }

    public void OpenChat(DeviceInfo device)
    {
        ArgumentNullException.ThrowIfNull(device);

        IntPtr ownerWindowHandle;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_chatWindows.TryGetValue(device.DeviceId, out var existing)
                && !existing.IsClosed)
            {
                existing.Restore();
                return;
            }

            ownerWindowHandle = _mainWindowHandle;
        }

        DuxelModelessWindow? created = null;
        created = NuriApplication.ShowModeless(
            _ => new ChatWindowComponent(device),
            title: $"SimplyShare - {device.Nickname}",
            width: 560,
            height: 700,
            ownerWindowHandle: ownerWindowHandle,
            closed: () => ClearChatWindow(device.DeviceId, created));

        lock (_gate)
        {
            if (_disposed || created.IsClosed)
            {
                created.Dispose();
                return;
            }

            _chatWindows[device.DeviceId] = created;
        }
    }

    public string[] SelectFiles(string title)
    {
        IntPtr ownerWindowHandle;
        lock (_gate)
        {
            ThrowIfDisposed();
            ownerWindowHandle = _mainWindowHandle;
        }

        return NativeFilePicker.SelectFiles(ownerWindowHandle, title);
    }

    public void Dispose()
    {
        DuxelModelessWindow? settingsWindow;
        DuxelModelessWindow[] chatWindows;
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            settingsWindow = _settingsWindow;
            chatWindows = _chatWindows.Values.ToArray();
            _settingsWindow = null;
            _chatWindows.Clear();
            _mainWindowHandle = IntPtr.Zero;
        }

        settingsWindow?.Dispose();
        foreach (var window in chatWindows)
            window.Dispose();
    }

    private void ClearSettingsWindow(DuxelModelessWindow? window)
    {
        var removed = false;
        lock (_gate)
        {
            if (ReferenceEquals(_settingsWindow, window))
            {
                _settingsWindow = null;
                removed = true;
            }
        }

        if (removed)
            window?.Dispose();
    }

    private void ClearChatWindow(string deviceId, DuxelModelessWindow? window)
    {
        var removed = false;
        lock (_gate)
        {
            if (_chatWindows.TryGetValue(deviceId, out var current)
                && ReferenceEquals(current, window))
            {
                _chatWindows.Remove(deviceId);
                removed = true;
            }
        }

        if (removed)
            window?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
