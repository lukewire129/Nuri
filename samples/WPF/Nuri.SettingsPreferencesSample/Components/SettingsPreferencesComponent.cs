using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.SettingsPreferencesSample.Components;

public sealed class SettingsPreferencesComponent : Component
{
    public override IElement Render()
    {
        var (state, setState) = useState(new PreferenceState(
            ProfileName: "Nuri User",
            EmailNotifications: true,
            PushNotifications: false,
            WeeklyDigest: true,
            BetaFeatures: false,
            Theme: ThemeChoice.System,
            Density: DensityChoice.Comfortable,
            RequireConfirm: true,
            TriedSubmit: false));

        var stateRef = useLatest(state);

        void Update(Func<PreferenceState, PreferenceState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        void Save()
        {
            Update(current => current with { TriedSubmit = true });
        }

        var errors = Validate(state);
        var canSave = errors.Length == 0;

        return Grid(Rows(Auto, Star),
                Header(canSave).Row(0),
                Div(DivTypes.Scroll,
                    Div(
                        new PreferenceSection("프로필", "controlled input과 기본 validation을 확인합니다.",
                            Text("표시 이름")
                                .FontSize(12)
                                .FontColor("#374151")
                                .Margin(bottom: 6),
                            TextBox(state.ProfileName, value => Update(current => current with { ProfileName = value }))
                                .Key("profile-name")
                                .Height(36)
                                .Padding(10, 0, 10, 0)
                                .TextStart()
                                .TextVCenter(),
                            state.TriedSubmit && string.IsNullOrWhiteSpace(state.ProfileName)
                                ? Text("표시 이름은 필수입니다.")
                                    .FontSize(12)
                                    .FontColor("#be123c")
                                    .Margin(top: 8)
                                : Text("저장 시 빈 값 validation을 확인합니다.")
                                    .FontSize(12)
                                    .FontColor("#6b7280")
                                    .Margin(top: 8)),
                        new PreferenceSection("알림", "checkbox와 toggle 상태 동기화를 확인합니다.",
                            CheckBox("이메일 알림 받기", value => Update(current => current with { EmailNotifications = value }))
                                .Checked(state.EmailNotifications)
                                .Margin(bottom: 10),
                            CheckBox("푸시 알림 받기", value => Update(current => current with { PushNotifications = value }))
                                .Checked(state.PushNotifications)
                                .Margin(bottom: 10),
                            ToggleButton(state.WeeklyDigest ? "주간 요약 켜짐" : "주간 요약 꺼짐", value => Update(current => current with { WeeklyDigest = value }))
                                .Checked(state.WeeklyDigest)
                                .Height(34)),
                        new PreferenceSection("화면", "radio group과 toggle을 섞어서 설정 그룹을 구성합니다.",
                            Text("테마")
                                .FontSize(12)
                                .FontColor("#374151")
                                .Margin(bottom: 8),
                            Div(DivTypes.Row,
                                RadioButton("시스템", selected => { if (selected) Update(current => current with { Theme = ThemeChoice.System }); })
                                    .Group("theme")
                                    .Checked(state.Theme == ThemeChoice.System)
                                    .Margin(right: 18),
                                RadioButton("라이트", selected => { if (selected) Update(current => current with { Theme = ThemeChoice.Light }); })
                                    .Group("theme")
                                    .Checked(state.Theme == ThemeChoice.Light)
                                    .Margin(right: 18),
                                RadioButton("다크", selected => { if (selected) Update(current => current with { Theme = ThemeChoice.Dark }); })
                                    .Group("theme")
                                    .Checked(state.Theme == ThemeChoice.Dark)),
                            Text("밀도")
                                .FontSize(12)
                                .FontColor("#374151")
                                .Margin(top: 16, bottom: 8),
                            Div(DivTypes.Row,
                                RadioButton("여유", selected => { if (selected) Update(current => current with { Density = DensityChoice.Comfortable }); })
                                    .Group("density")
                                    .Checked(state.Density == DensityChoice.Comfortable)
                                    .Margin(right: 18),
                                RadioButton("촘촘", selected => { if (selected) Update(current => current with { Density = DensityChoice.Compact }); })
                                    .Group("density")
                                    .Checked(state.Density == DensityChoice.Compact)),
                            ToggleButton(state.BetaFeatures ? "실험 기능 사용" : "실험 기능 미사용", value => Update(current => current with { BetaFeatures = value }))
                                .Checked(state.BetaFeatures)
                                .Height(34)
                                .Margin(top: 16)),
                        new PreferenceSection("저장 조건", "form grouping과 submit validation 흐름을 확인합니다.",
                            CheckBox("변경 전 확인 절차를 유지합니다", value => Update(current => current with { RequireConfirm = value }))
                                .Checked(state.RequireConfirm)
                                .Margin(bottom: 12),
                            new ValidationSummary(state.TriedSubmit ? errors : Array.Empty<string>()),
                            Grid(
                                    Text(PreviewText(state))
                                        .FontSize(12)
                                        .FontColor("#6b7280")
                                        .VCenter()
                                        .Column(0),
                                    Button(canSave ? "저장" : "검증", Save)
                                        .Height(36)
                                        .Background("#111827")
                                        .FontColor("#ffffff")
                                        .Brush("#111827")
                                        .Thickness(1)
                                        .Column(1)
                                )
                                .Columns(Star, Pixels(88))
                                .Margin(top: 14))
                    ))
                    .Row(1)
            )
            .Padding(24)
            .Background("#f3f4f6");
    }

    private static IElement Header(bool canSave)
    {
        return Grid(
                Div(
                        Text("Settings / Preferences")
                            .FontSize(26)
                            .FontWeight(FontWeightValue.Bold)
                            .FontColor("#111827"),
                        Text("checkbox, radio, toggle, form grouping, validation 검증 샘플입니다.")
                            .FontSize(13)
                            .FontColor("#6b7280")
                            .Margin(top: 6)
                    )
                    .Column(0),
                Text(canSave ? "valid" : "needs check")
                    .FontSize(12)
                    .FontColor(canSave ? "#047857" : "#be123c")
                    .End()
                    .VCenter()
                    .Column(1)
            )
            .Columns(Star, Pixels(120))
            .Margin(bottom: 18);
    }

    private static string[] Validate(PreferenceState state)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(state.ProfileName))
            errors.Add("표시 이름을 입력하세요.");

        if (!state.EmailNotifications && !state.PushNotifications && state.WeeklyDigest)
            errors.Add("주간 요약을 켜려면 이메일 또는 푸시 알림 중 하나가 필요합니다.");

        if (state.BetaFeatures && !state.RequireConfirm)
            errors.Add("실험 기능을 쓰려면 변경 전 확인 절차가 필요합니다.");

        return errors.ToArray();
    }

    private static string PreviewText(PreferenceState state)
    {
        return $"테마: {ThemeLabel(state.Theme)} · 밀도: {DensityLabel(state.Density)} · 알림: {(state.EmailNotifications || state.PushNotifications ? "켜짐" : "꺼짐")}";
    }

    private static string ThemeLabel(ThemeChoice theme)
    {
        return theme switch
        {
            ThemeChoice.Light => "라이트",
            ThemeChoice.Dark => "다크",
            _ => "시스템"
        };
    }

    private static string DensityLabel(DensityChoice density)
    {
        return density == DensityChoice.Compact ? "촘촘" : "여유";
    }
}
