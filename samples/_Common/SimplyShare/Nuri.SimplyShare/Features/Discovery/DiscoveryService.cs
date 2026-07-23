using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using Nuri.SimplyShare.Features.Settings;

namespace Nuri.SimplyShare.Features.Discovery;

public sealed class DiscoveryService : IAsyncDisposable
{
    private readonly AppSettings _settings;
    private readonly ConcurrentDictionary<string, DeviceInfo> _devices = new();
    private CancellationTokenSource? _lifetime;
    private UdpClient? _listener;
    private Task? _listenTask;
    private Task? _heartbeatTask;
    private Task? _cleanupTask;

    public DiscoveryService(AppSettings settings) => _settings = settings;

    public event Action<IEnumerable<DeviceInfo>>? DevicesChanged;
    public event Action<string>? Error;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new UdpClient();
        _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Client.Bind(new IPEndPoint(IPAddress.Any, _settings.DiscoveryPort));
        _listener.EnableBroadcast = true;

        _listenTask = ListenAsync(_lifetime.Token);
        _heartbeatTask = HeartbeatAsync(_lifetime.Token);
        _cleanupTask = CleanupAsync(_lifetime.Token);
        await BroadcastAsync("discovery", cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var lifetime = _lifetime;
        if (lifetime is null)
            return;

        _lifetime = null;
        lifetime.Cancel();
        var listener = _listener;
        _listener = null;
        listener?.Dispose();
        var tasks = new[] { _listenTask, _heartbeatTask, _cleanupTask }.Where(task => task is not null).Cast<Task>();
        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
        }

        lifetime.Dispose();
        _listenTask = null;
        _heartbeatTask = null;
        _cleanupTask = null;
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var received = await _listener!.ReceiveAsync(cancellationToken);
                if (!_settings.Allows(received.RemoteEndPoint.Address))
                    continue;

                var packet = JsonSerializer.Deserialize<DiscoveryPacket>(received.Buffer);
                if (packet is null || packet.DeviceId == _settings.DeviceId)
                    continue;

                if (packet.Type == "goodbye")
                {
                    _devices.TryRemove(packet.DeviceId, out _);
                    Publish();
                    continue;
                }

                var isNew = !_devices.ContainsKey(packet.DeviceId);
                _devices[packet.DeviceId] = new DeviceInfo(
                    packet.DeviceId,
                    packet.Nickname,
                    received.RemoteEndPoint.Address.ToString(),
                    packet.Port,
                    DateTime.UtcNow);
                Publish();

                if (isNew)
                    await BroadcastAsync("heartbeat", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception exception)
            {
                Error?.Invoke($"Discovery error: {exception.Message}");
                await Task.Delay(500, cancellationToken);
            }
        }
    }

    private async Task HeartbeatAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await BroadcastAsync("heartbeat", cancellationToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
            var threshold = DateTime.UtcNow - TimeSpan.FromSeconds(16);
            foreach (var pair in _devices)
            {
                if (pair.Value.LastSeen < threshold)
                    _devices.TryRemove(pair.Key, out _);
            }
            Publish();
        }
    }

    private async Task BroadcastAsync(string type, CancellationToken cancellationToken)
    {
        var packet = new DiscoveryPacket(type, _settings.DeviceId, _settings.Nickname, _settings.TransferPort, "1.0");
        var payload = JsonSerializer.SerializeToUtf8Bytes(packet);
        var targets = GetBroadcastTargets().ToArray();

        if (targets.Length == 0)
            targets = [new IPEndPoint(IPAddress.Broadcast, _settings.DiscoveryPort)];

        foreach (var target in targets)
            await _listener!.SendAsync(payload, target, cancellationToken);
    }

    private IEnumerable<IPEndPoint> GetBroadcastTargets()
    {
        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (network.OperationalStatus != OperationalStatus.Up || network.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var address in network.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork || !_settings.Allows(address.Address))
                    continue;

                var ip = address.Address.GetAddressBytes();
                var mask = address.IPv4Mask.GetAddressBytes();
                var broadcast = new byte[4];
                for (var i = 0; i < broadcast.Length; i++)
                    broadcast[i] = (byte)(ip[i] | ~mask[i]);
                yield return new IPEndPoint(new IPAddress(broadcast), _settings.DiscoveryPort);
            }
        }
    }

    private void Publish() => DevicesChanged?.Invoke(_devices.Values.ToArray());
}
