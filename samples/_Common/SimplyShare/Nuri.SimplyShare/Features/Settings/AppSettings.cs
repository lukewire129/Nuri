using System.Net;

namespace Nuri.SimplyShare.Features.Settings;

public sealed record AppSettings(
    string DeviceId,
    string Nickname,
    int DiscoveryPort,
    int TransferPort,
    string DownloadPath,
    string NetworkRange)
{
    public static AppSettings CreateDefault()
    {
        return new AppSettings(
            Guid.NewGuid().ToString("N")[..12],
            Environment.UserName,
            52525,
            52526,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "SimplyShare"),
            "*");
    }

    public bool Allows(IPAddress address)
    {
        if (NetworkRange is "" or "*")
            return true;

        var pattern = NetworkRange.Trim();
        var value = address.MapToIPv4().ToString();
        if (pattern.EndsWith(".*", StringComparison.Ordinal))
            return value.StartsWith(pattern[..^1], StringComparison.Ordinal);

        return string.Equals(value, pattern, StringComparison.Ordinal);
    }
}
