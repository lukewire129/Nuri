namespace Nuri.SimplyShare.Features.Discovery;

public sealed record DeviceInfo(
    string DeviceId,
    string Nickname,
    string IpAddress,
    int Port,
    DateTime LastSeen);

public sealed record DiscoveryPacket(
    string Type,
    string DeviceId,
    string Nickname,
    int Port,
    string Version);
