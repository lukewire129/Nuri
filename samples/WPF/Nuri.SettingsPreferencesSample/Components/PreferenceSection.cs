using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.SettingsPreferencesSample.Components;

internal sealed class PreferenceSection : Component
{
    private readonly string _title;
    private readonly string _description;
    private readonly IElement[] _children;

    public PreferenceSection(string title, string description, params IElement[] children)
    {
        _title = title;
        _description = description;
        _children = children;
    }

    public override IElement Render()
    {
        return Div(
                Text(_title)
                    .FontSize(17)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#111827"),
                Text(_description)
                    .FontSize(12)
                    .FontColor("#6b7280")
                    .Margin(top: 4, bottom: 14),
                Div(_children)
            )
            .Padding(18)
            .Margin(bottom: 14)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }
}
