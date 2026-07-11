using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.ModalDialogSample.Components;

public sealed class ModalDialogComponent : Component
{
    public override IElement Render()
    {
        var (state, setState) = useState(new ModalState(false, false, 0, Array.Empty<string>()));
        var stateRef = useLatest(state);

        void Update(Func<ModalState, ModalState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        void AddLog(string message)
        {
            Update(current => current with
            {
                Logs = new[] { $"{DateTime.Now:HH:mm:ss}  {message}" }.Concat(current.Logs).Take(10).ToArray()
            });
        }

        void OpenDialog()
        {
            Update(current => current with
            {
                DialogOpen = true,
                DialogVersion = current.DialogVersion + 1,
                Logs = new[] { $"{DateTime.Now:HH:mm:ss}  open dialog requested" }.Concat(current.Logs).Take(10).ToArray()
            });
        }

        void CloseDialog()
        {
            Update(current => current with { DialogOpen = false, ConfirmOpen = false });
        }

        void OpenConfirm()
        {
            Update(current => current with { ConfirmOpen = true });
        }

        void CloseConfirm()
        {
            Update(current => current with { ConfirmOpen = false });
        }

        var rootChildren = new List<IElement>
        {
            MainContent(state, OpenDialog).Row(0)
        };

        if (state.DialogOpen)
        {
            rootChildren.Add(
                Overlay(OverlayTypes.Modal,
                        new DialogOverlay(
                            state.DialogVersion,
                            state.ConfirmOpen,
                            CloseDialog,
                            OpenConfirm,
                            CloseConfirm,
                            AddLog).Key($"dialog-{state.DialogVersion}"))
                    .Row(0));
        }

        return Grid(Rows(Star), rootChildren.ToArray())
            .Background("#f3f4f6");
    }

    private static IElement MainContent(ModalState state, Action openDialog)
    {
        return Grid(Columns(Star, Pixels(300)),
                Div(
                        Text("Modal / Dialog")
                            .FontSize(28)
                            .FontWeight(FontWeightValue.Bold)
                            .FontColor("#111827"),
                        Text("mount/unmount, cleanup, overlay layering 검증 샘플입니다.")
                            .FontSize(13)
                            .FontColor("#6b7280")
                            .Margin(top: 6, bottom: 22),
                        Div(
                                Text("검증 포인트")
                                    .FontSize(17)
                                    .FontWeight(FontWeightValue.Bold)
                                    .FontColor("#111827"),
                                Text("1. 열기 버튼으로 DialogOverlay가 mount됩니다.").Margin(top: 10),
                                Text("2. 닫기 버튼 또는 confirm close로 unmount cleanup 로그가 남습니다.").Margin(top: 6),
                                Text("3. Dialog 위에 Confirm overlay를 한 장 더 올려 layering을 확인합니다.").Margin(top: 6),
                                Button("Dialog 열기", openDialog)
                                    .Height(38)
                                    .Margin(top: 18)
                                    .Background("#111827")
                                    .FontColor("#ffffff")
                                    .Brush("#111827")
                                    .Thickness(1)
                            )
                            .Padding(20)
                            .Background("#ffffff")
                            .Brush("#e5e7eb")
                            .Thickness(1)
                            .CornerRadius(16)
                    )
                    .Padding(24)
                    .Column(0),
                ActivityLog(state.Logs).Column(1)
            );
    }

    private static IElement ActivityLog(string[] logs)
    {
        return Div(
                Text("Lifecycle log")
                    .FontSize(16)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#111827"),
                logs.Length == 0
                    ? Text("아직 로그가 없습니다.")
                        .FontSize(12)
                        .FontColor("#6b7280")
                        .Margin(top: 12)
                    : Div(logs.Select(log => (IElement)Text(log)
                            .FontSize(12)
                            .FontColor("#374151")
                            .Margin(top: 8))
                        .ToArray())
            )
            .Padding(18)
            .Margin(0, 24, 24, 24)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }
}

internal sealed record ModalState(bool DialogOpen, bool ConfirmOpen, int DialogVersion, string[] Logs);
