using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.SettingsPreferencesSample.Components;

internal sealed class ValidationSummary : Component
{
    private readonly string[] _errors;

    public ValidationSummary(string[] errors)
    {
        _errors = errors;
    }

    public override IElement Render()
    {
        if (_errors.Length == 0)
        {
            return Div(
                    Text("저장할 수 있는 상태입니다.")
                        .FontSize(13)
                        .FontColor("#047857")
                )
                .Padding(14)
                .Background("#ecfdf5")
                .Brush("#a7f3d0")
                .Thickness(1)
                .CornerRadius(14);
        }

        return Div(
                Text("확인할 항목")
                    .FontSize(13)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#be123c"),
                Div(_errors.Select(error => (IElement)Text($"- {error}")
                        .FontSize(12)
                        .FontColor("#9f1239")
                        .Margin(top: 6))
                    .ToArray())
            )
            .Padding(14)
            .Background("#fff1f2")
            .Brush("#fecdd3")
            .Thickness(1)
            .CornerRadius(14);
    }
}
