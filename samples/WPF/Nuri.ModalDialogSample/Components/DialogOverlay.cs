using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.ModalDialogSample.Components;

internal sealed class DialogOverlay : Component
{
    private readonly int _version;
    private readonly bool _confirmOpen;
    private readonly Action _close;
    private readonly Action _openConfirm;
    private readonly Action _closeConfirm;
    private readonly Action<string> _addLog;

    public DialogOverlay(
        int version,
        bool confirmOpen,
        Action close,
        Action openConfirm,
        Action closeConfirm,
        Action<string> addLog)
    {
        _version = version;
        _confirmOpen = confirmOpen;
        _close = close;
        _openConfirm = openConfirm;
        _closeConfirm = closeConfirm;
        _addLog = addLog;
    }

    public override IElement Render()
    {
        useEffect(() =>
        {
            _addLog($"dialog #{_version} mounted");
            return () => _addLog($"dialog #{_version} cleanup on unmount");
        }, []);

        var children = new List<IElement>
        {
            Backdrop(),
            DialogCard().Center()
        };

        if (_confirmOpen)
            children.Add(new ConfirmOverlay(_close, _closeConfirm, _addLog).Key($"confirm-{_version}"));

        return Overlay(OverlayTypes.Modal, children.ToArray())
            .Background("#00000066");
    }

    private IElement Backdrop()
    {
        return Grid(
                Text(" ")
            )
            .Background("#00000022");
    }

    private IElement DialogCard()
    {
        return Div(
                Text("설정 변경")
                    .FontSize(22)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#111827"),
                Text($"Dialog instance #{_version}")
                    .FontSize(12)
                    .FontColor("#6b7280")
                    .Margin(top: 4, bottom: 16),
                Text("이 박스는 조건부 렌더링으로 mount/unmount 됩니다. 닫으면 cleanup 로그가 남습니다.")
                    .FontSize(13)
                    .FontColor("#374151")
                    .Margin(bottom: 18),
                Grid(
                        Button("그냥 닫기", _close)
                            .Height(36)
                            .Column(0),
                        Button("Confirm 한 장 더", _openConfirm)
                            .Height(36)
                            .Background("#111827")
                            .FontColor("#ffffff")
                            .Brush("#111827")
                            .Thickness(1)
                            .Column(1)
                    )
                    .Columns(Star, Pixels(140))
            )
            .Width(420)
            .Padding(22)
            .Background("#ffffff")
            .Brush("#d1d5db")
            .Thickness(1)
            .CornerRadius(18);
    }
}
