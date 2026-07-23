using Nuri.SimplyShare.App;
using Nuri.SimplyShare.Shared;

namespace Nuri.SimplyShare.Features.Settings;

public sealed class SettingsWindowComponent : Component
{
    private readonly Action _close;

    public SettingsWindowComponent(Action close) => _close = close;

    public override IElement Render()
    {
        var initial = AppState.Settings.Value;
        var (nickname, setNickname) = useState(initial.Nickname);
        var (downloadPath, setDownloadPath) = useState(initial.DownloadPath);
        var (networkRange, setNetworkRange) = useState(initial.NetworkRange);
        var (discoveryPort, setDiscoveryPort) = useState(initial.DiscoveryPort.ToString());
        var (transferPort, setTransferPort) = useState(initial.TransferPort.ToString());
        var (message, setMessage) = useState("");
        var (saving, setSaving) = useState(false);

        async void Save()
        {
            if (string.IsNullOrWhiteSpace(nickname)
                || !int.TryParse(discoveryPort, out var parsedDiscoveryPort)
                || !int.TryParse(transferPort, out var parsedTransferPort)
                || parsedDiscoveryPort is < 1 or > 65535
                || parsedTransferPort is < 1 or > 65535)
            {
                setMessage(_ => "Enter a nickname and valid port numbers.");
                return;
            }

            setSaving(_ => true);
            setMessage(_ => "Saving and restarting network services...");
            try
            {
                await AppServices.SaveSettingsAsync(initial with
                {
                    Nickname = nickname.Trim(),
                    DownloadPath = downloadPath.Trim(),
                    NetworkRange = networkRange.Trim(),
                    DiscoveryPort = parsedDiscoveryPort,
                    TransferPort = parsedTransferPort
                });
                _close();
            }
            catch (Exception exception)
            {
                setMessage(_ => exception.Message);
                setSaving(_ => false);
            }
        }

        var saveButton = Button(saving ? "Saving..." : "Save settings", Save)
            .Height(42)
            .Background(Palette.Accent)
            .FontColor(Palette.White)
            .Brush(Palette.AccentDark)
            .Thickness(1);
        saveButton.SetProperty("IsEnabled", !saving);

        return Div(DivTypes.Scroll,
                Div(
                        Text("Settings").FontSize(28).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink),
                        Text("Network services restart after saving.").FontSize(12).FontColor(Palette.Muted).Margin(top: 5, bottom: 24),
                        Label("Nickname"),
                        Field(nickname, value => setNickname(_ => value)),
                        Label("Download folder"),
                        Field(downloadPath, value => setDownloadPath(_ => value)),
                        Label("Allowed IPv4 range (* or 192.168.0.*)"),
                        Field(networkRange, value => setNetworkRange(_ => value)),
                        Grid(
                                Div(Label("Discovery port"), Field(discoveryPort, value => setDiscoveryPort(_ => value))).Column(0),
                                Div(Label("Transfer port"), Field(transferPort, value => setTransferPort(_ => value))).Column(1))
                            .Columns(Star, Star)
                            .ColumnSpacing(12),
                        string.IsNullOrEmpty(message)
                            ? Div().Height(12)
                            : Text(message).FontSize(11).FontColor(Palette.Danger).Margin(top: 12),
                        saveButton.Margin(top: 16))
                    .Padding(28))
            .Background(Palette.Canvas);
    }

    private static IElement Label(string text)
    {
        return Text(text).FontSize(12).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink).Margin(top: 10, bottom: 6);
    }

    private static IElement Field(string value, Action<string> changed)
    {
        return TextBox(value, changed)
            .Height(38)
            .Padding(10, 0, 10, 0)
            .Background(Palette.White);
    }
}
