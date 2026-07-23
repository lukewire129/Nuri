using Nuri.SimplyShare.Features.Discovery;
using Nuri.SimplyShare.Features.Settings;
using Nuri.SimplyShare.Features.Transfers;

namespace Nuri.SimplyShare.App;

public static class AppServices
{
    private static readonly object SyncRoot = new();
    private static readonly SemaphoreSlim LifecycleGate = new(1, 1);
    private static CancellationTokenSource? _lifetime;
    private static Task _startTask = Task.CompletedTask;

    public static ISimplyShareHost Host { get; private set; } = null!;

    public static SettingsRepository SettingsRepository { get; private set; } = null!;
    public static DiscoveryService Discovery { get; private set; } = null!;
    public static SecureTransferService Transfers { get; private set; } = null!;

    public static void Initialize(ISimplyShareHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        Host = host;
        SettingsRepository = new SettingsRepository();
        var settings = SettingsRepository.Load();
        AppState.Settings.Set(settings);

        _lifetime = new CancellationTokenSource();
        CreateNetworkServices(settings);
        _startTask = StartNetworkServicesAsync(Transfers, Discovery, _lifetime.Token);
    }

    public static async Task SaveSettingsAsync(AppSettings settings)
    {
        await LifecycleGate.WaitAsync();
        try
        {
            SettingsRepository.Save(settings);
            AppState.Settings.Set(settings);
            AppState.Status.Set("Restarting network services...");

            CancellationToken token;
            lock (SyncRoot)
            {
                token = _lifetime?.Token ?? CancellationToken.None;
            }

            await _startTask;
            await StopNetworkServicesAsync(Discovery, Transfers);
            token.ThrowIfCancellationRequested();
            CreateNetworkServices(settings);
            _startTask = StartNetworkServicesAsync(Transfers, Discovery, token);
            await _startTask;
        }
        finally
        {
            LifecycleGate.Release();
        }
    }

    public static void Dispose()
    {
        lock (SyncRoot)
        {
            _lifetime?.Cancel();
        }

        DisposeAsync().GetAwaiter().GetResult();
    }

    private static void CreateNetworkServices(AppSettings settings)
    {
        Discovery = new DiscoveryService(settings);
        Transfers = new SecureTransferService(settings);

        Discovery.DevicesChanged += AppState.SetDevices;
        Discovery.Error += message => AppState.Status.Set(message);
        Transfers.TransferChanged += AppState.UpsertTransfer;
        Transfers.TextReceived += (sender, deviceId, text) =>
        {
            AppState.AddMessage(deviceId, Features.Chat.ChatMessage.ReceivedText(text));
            AppState.Status.Set($"Message received from {sender}");
        };
        Transfers.FileReceived += (sender, deviceId, path) =>
        {
            AppState.AddMessage(deviceId, Features.Chat.ChatMessage.ReceivedFile(path));
            AppState.Status.Set($"File received from {sender}: {Path.GetFileName(path)}");
        };
    }

    private static async Task StartNetworkServicesAsync(
        SecureTransferService transfers,
        DiscoveryService discovery,
        CancellationToken cancellationToken)
    {
        try
        {
            await transfers.StartAsync(cancellationToken);
            await discovery.StartAsync(cancellationToken);
            AppState.Status.Set($"Online as {AppState.Settings.Value.Nickname}");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            AppState.Status.Set($"Network startup failed: {exception.Message}");
        }
    }

    private static async Task DisposeAsync()
    {
        await LifecycleGate.WaitAsync();
        try
        {
            await _startTask;
            await StopNetworkServicesAsync(Discovery, Transfers);

            lock (SyncRoot)
            {
                _lifetime?.Dispose();
                _lifetime = null;
            }
        }
        finally
        {
            LifecycleGate.Release();
        }
    }

    private static Task StopNetworkServicesAsync(
        DiscoveryService discovery,
        SecureTransferService transfers)
    {
        return Task.WhenAll(
            discovery.DisposeAsync().AsTask(),
            transfers.DisposeAsync().AsTask());
    }
}
