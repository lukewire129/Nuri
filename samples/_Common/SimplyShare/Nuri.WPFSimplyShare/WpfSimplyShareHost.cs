using Microsoft.Win32;
using System.Windows;
using Nuri.SimplyShare.Features.Chat;
using Nuri.SimplyShare.Features.Discovery;
using Nuri.SimplyShare.Features.Settings;

namespace Nuri.SimplyShare.App;

internal sealed class WpfSimplyShareHost : ISimplyShareHost, IDisposable
{
    private Window? _settingsWindow;
    private readonly Dictionary<string, Window> _chatWindows = new();

    public void OpenSettings()
    {
        if (Activate(_settingsWindow))
            return;

        var window = CreateWindow("SimplyShare Settings", 520, 600);
        _settingsWindow = window;
        NuriApplication.Attach(window, new SettingsWindowComponent(window.Close));
        window.Closed += (_, _) => _settingsWindow = null;
        window.Show();
    }

    public void OpenChat(DeviceInfo device)
    {
        if (_chatWindows.TryGetValue(device.DeviceId, out var existing) && Activate(existing))
            return;

        var window = CreateWindow($"SimplyShare - {device.Nickname}", 560, 700);
        _chatWindows[device.DeviceId] = window;
        NuriApplication.Attach(window, new ChatWindowComponent(device));
        window.Closed += (_, _) => _chatWindows.Remove(device.DeviceId);
        window.Show();
    }

    public string[] SelectFiles(string title)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = title
        };
        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    public void Dispose()
    {
        if (_settingsWindow?.IsLoaded == true)
            _settingsWindow.Close();

        foreach (var window in _chatWindows.Values.ToArray())
        {
            if (window.IsLoaded)
                window.Close();
        }

        _settingsWindow = null;
        _chatWindows.Clear();
    }

    private static Window CreateWindow(string title, double width, double height)
    {
        return new Window
        {
            Title = title,
            Width = width,
            Height = height,
            MinWidth = Math.Min(width, 420),
            MinHeight = Math.Min(height, 480),
            Owner = Application.Current?.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
    }

    private static bool Activate(Window? window)
    {
        if (window is null || !window.IsLoaded)
            return false;

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
        return true;
    }
}
