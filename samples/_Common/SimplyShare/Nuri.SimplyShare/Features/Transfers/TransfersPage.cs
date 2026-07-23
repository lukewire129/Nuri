using Nuri.SimplyShare.App;
using Nuri.SimplyShare.Shared;

namespace Nuri.SimplyShare.Features.Transfers;

internal sealed class TransfersPage : Component
{
    public override IElement Render()
    {
        var transfers = useStore(AppState.Transfers);

        return new PageLayout(
            "Activity",
            "Encrypted transfers from this session.",
            transfers.Count == 0
                ? new SurfaceCard(
                    Text("No transfers yet").FontSize(18).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink),
                    Text("Open a nearby device and send a message or file.").FontColor(Palette.Muted).Margin(top: 8))
                : Div(transfers.Select(item => (IElement)new TransferRow(item).Key(item.Id)).ToArray()));
    }
}

internal sealed class TransferRow : Component
{
    private readonly TransferItem _item;

    public TransferRow(TransferItem item) => _item = item;

    public override IElement Render()
    {
        var (hovered, setHovered) = useState(false);
        var statusColor = _item.Status switch
        {
            TransferStatus.Completed => Palette.Success,
            TransferStatus.Failed => Palette.Danger,
            _ => Palette.Warning
        };
        var detail = _item.Error ?? $"{Formatters.FileSize(_item.BytesTransferred)} / {Formatters.FileSize(_item.TotalBytes)}";

        return Div(
            Grid(
                    Div(
                            Text(_item.Name).FontSize(15).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink),
                            Text($"{_item.Direction} with {_item.Peer}").FontSize(11).FontColor(Palette.Muted).Margin(top: 4))
                        .Column(0),
                    Text(_item.Status.ToString()).FontSize(11).FontWeight(FontWeightValue.Bold).FontColor(statusColor).End().Column(1))
                .Columns(Star, Auto),
            Div(
                    Div().Height(6).Width(Math.Max(2, 500 * _item.Progress)).Background(statusColor).CornerRadius(3))
                .Height(6)
                .Background(Palette.Border)
                .CornerRadius(3)
                .Margin(top: 14),
            Text(detail).FontSize(10).FontColor(Palette.Muted).Margin(top: 7))
            .Padding(20)
            .Margin(left: hovered ? 5 : 0, bottom: 14)
            .Background(hovered ? "#FAFCFD" : Palette.White)
            .Brush(Palette.Border)
            .Thickness(1)
            .CornerRadius(14)
            .OnHover(value => setHovered(_ => value))
            .Transition(TimeSpan.FromMilliseconds(160), EasingValue.CubicOut);
    }
}
