using Nuri.SimplyShare.App;
using Nuri.SimplyShare.Features.Discovery;
using Nuri.SimplyShare.Shared;

namespace Nuri.SimplyShare.Features.Devices;

internal sealed class DevicesPage : Component
{
    public override IElement Render()
    {
        var devices = useStore(AppState.Devices);
        var settings = useStore(AppState.Settings);

        return new PageLayout(
            "Nearby devices",
            $"Signed in as {settings.Nickname}. Devices disappear after 16 seconds without a heartbeat.",
            devices.Count == 0
                ? new DiscoveryEmptyState()
                : Div(devices.Select(device => (IElement)new DeviceCard(device).Key(device.DeviceId)).ToArray()));
    }
}

internal sealed class DiscoveryEmptyState : Component
{
    public override IElement Render()
    {
        return new SurfaceCard(
            Div(
                    Text("Scanning").FontSize(12).FontWeight(FontWeightValue.Bold).FontColor(Palette.Accent).Center())
                .Width(82)
                .Padding(8)
                .Background(Palette.SoftAccent)
                .CornerRadius(12),
            Text("No other SimplyShare device is visible yet.")
                .FontSize(20)
                .FontWeight(FontWeightValue.Bold)
                .FontColor(Palette.Ink)
                .Margin(top: 18),
            Text("Run this application on another PC in the same allowed IPv4 range. Windows Firewall may ask for private-network access.")
                .FontColor(Palette.Muted)
                .Margin(top: 8));
    }
}

internal sealed class DeviceCard : Component
{
    private readonly DeviceInfo _device;

    public DeviceCard(DeviceInfo device) => _device = device;

    public override IElement Render()
    {
        var (hovered, setHovered) = useState(false);
        return Div(
                Grid(
                    Div(DivTypes.Row,
                            Div(Text(Initials(_device.Nickname)).FontSize(15).FontWeight(FontWeightValue.Bold).FontColor(Palette.White).Center())
                                .Size(46, 46)
                                .Background(Palette.Success)
                                .CornerRadius(23)
                                .Margin(right: 14),
                            Div(
                                Text(_device.Nickname).FontSize(17).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink),
                                Text($"{_device.IpAddress}:{_device.Port}").FontSize(11).FontColor(Palette.Muted).Margin(top: 4)))
                        .Column(0),
                    Button("Open chat", () => AppServices.Host.OpenChat(_device))
                        .Width(112)
                        .Height(38)
                        .Background(Palette.Ink)
                        .FontColor(Palette.White)
                        .Brush(Palette.Ink)
                        .Thickness(1)
                        .Column(1))
                    .Columns(Star, Auto))
            .Padding(20)
            .Margin(left: hovered ? 7 : 0, bottom: 14)
            .Background(hovered ? "#FFFAF7" : Palette.White)
            .Brush(hovered ? "#F0B29D" : Palette.Border)
            .Thickness(1)
            .CornerRadius(14)
            .TranslateX(hovered ? 4 : 0)
            .OnHover(value => setHovered(_ => value))
            .Transition(TimeSpan.FromMilliseconds(180), EasingValue.CubicOut);
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }
}
