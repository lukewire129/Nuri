using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.WPFAnimatedDashboardSample.Components;

public sealed class AnimatedDashboardComponent : Component
{
    public override IElement Render()
    {
        var (highlighted, setHighlighted) = useState(false);
        var duration = TimeSpan.FromMilliseconds(550);
        var cardMargin = highlighted ? 18 : 6;
        var cardBackground = highlighted ? "#1d4ed8" : "#111827";
        var valueColor = highlighted ? "#fef3c7" : "#f8fafc";
        var rotation = highlighted ? 4 : -2;
        var translation = highlighted ? 10 : -4;
        var scale = highlighted ? 1.08 : 0.98;

        return Div(
                Text("WPF Animated Dashboard")
                    .FontSize(30)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc"),
                Text("Rapidly toggle the dashboard to replace active Margin, color, Rotate, Translate, and Scale transitions.")
                    .FontSize(14)
                    .FontColor("#94a3b8")
                    .Margin(top: 8, bottom: 20),
                MetricCard("Active sessions", "1,284", cardMargin, cardBackground, valueColor, rotation, translation, scale, duration),
                MetricCard("Conversion", "18.6%", cardMargin, cardBackground, valueColor, -rotation, -translation, scale, duration),
                MetricCard("Latency", "42 ms", cardMargin, cardBackground, valueColor, rotation, translation, scale, duration),
                Button(highlighted ? "Reset dashboard" : "Highlight dashboard", () => setHighlighted(current => !current))
                    .Height(44)
                    .Padding(18, 0, 18, 0)
                    .Margin(top: 22)
                    .Background("#2563eb")
                    .FontColor("#ffffff")
                    .Brush("#1d4ed8")
                    .Thickness(1)
            )
            .Padding(26)
            .Background("#0b1120");
    }

    private static Div MetricCard(
        string title,
        string value,
        double margin,
        string background,
        string valueColor,
        double rotation,
        double translation,
        double scale,
        TimeSpan duration)
    {
        return Div(
                Text(title)
                    .FontSize(13)
                    .FontColor("#bfdbfe"),
                Text(value)
                    .FontSize(28)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor(valueColor)
                    .Rotate(rotation)
                    .Translate(translation, 0)
                    .Scale(scale)
                    .Transition(duration, EasingValue.CubicOut)
            )
            .Padding(18)
            .Margin(margin)
            .Background(background)
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(16)
            .Transition(duration, EasingValue.CubicOut);
    }
}
