using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.ModalDialogSample.Components;

internal sealed class ConfirmOverlay : Component
{
    private readonly Action _closeDialog;
    private readonly Action _closeConfirm;
    private readonly Action<string> _addLog;

    public ConfirmOverlay(Action closeDialog, Action closeConfirm, Action<string> addLog)
    {
        _closeDialog = closeDialog;
        _closeConfirm = closeConfirm;
        _addLog = addLog;
    }

    public override IElement Render()
    {
        useEffect(() =>
        {
            _addLog("confirm overlay mounted above dialog");
            return () => _addLog("confirm overlay cleanup on unmount");
        }, []);

        return Overlay(OverlayTypes.Modal,
                Grid(Text(" ")).Background("#00000044"),
                Div(
                        Text("정말 닫을까요?")
                            .FontSize(19)
                            .FontWeight(FontWeightValue.Bold)
                            .FontColor("#111827"),
                        Text("이 Confirm은 Dialog 위에 올라간 두 번째 overlay입니다.")
                            .FontSize(13)
                            .FontColor("#6b7280")
                            .Margin(top: 8, bottom: 18),
                        Grid(
                                Button("취소", _closeConfirm)
                                    .Height(34)
                                    .Column(0),
                                Button("닫기", _closeDialog)
                                    .Height(34)
                                    .Background("#be123c")
                                    .FontColor("#ffffff")
                                    .Brush("#be123c")
                                    .Thickness(1)
                                    .Column(1)
                            )
                            .Columns(Star, Pixels(90))
                    )
                    .Width(340)
                    .Padding(20)
                    .Background("#ffffff")
                    .Brush("#fecdd3")
                    .Thickness(1)
                    .CornerRadius(16)
                    .Center()
            );
    }
}
