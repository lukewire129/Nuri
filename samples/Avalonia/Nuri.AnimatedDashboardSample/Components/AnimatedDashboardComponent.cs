using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.AnimatedDashboardSample.Components;

public sealed class AnimatedDashboardComponent : Component
{
    public override IElement Render()
    {
        var (focused, setFocused) = useState(true);
        var cardOpacity = focused ? 1.0 : 0.35;

        return Div(
                Text("Animated Dashboard")
                    .FontSize(30)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc"),
                Text("The same Core component runs through the WPF and Avalonia renderers.")
                    .FontSize(14)
                    .FontColor("#94a3b8")
                    .Margin(top: 8, bottom: 24),
                DashboardCard("Active sessions", "1,284", "Updated now", cardOpacity),
                DashboardCard("Conversion", "18.6%", "+2.4% this week", cardOpacity)
                    .Margin(top: 14),
                DashboardCard("Latency", "42 ms", "p95 across regions", cardOpacity)
                    .Margin(top: 14),
                Button(focused ? "Dim dashboard" : "Focus dashboard", () => setFocused(current => !current))
                    .Height(44)
                    .Padding(18, 0, 18, 0)
                    .Margin(top: 24)
                    .Background("#2563eb")
                    .FontColor("#ffffff")
                    .Brush("#1d4ed8")
                    .Thickness(1)
            )
            .Padding(32)
            .Background("#0b1120");
    }

    private static Div DashboardCard(string title, string value, string detail, double opacity)
    {
        return Div(
                Text(title)
                    .FontSize(13)
                    .FontColor("#94a3b8"),
                Text(value)
                    .FontSize(28)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc")
                    .Margin(top: 6),
                Text(detail)
                    .FontSize(12)
                    .FontColor("#60a5fa")
                    .Margin(top: 6)
            )
            .Padding(20)
            .Background("#111827")
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(16)
            .Opacity(opacity)
            .Transition(TimeSpan.FromMilliseconds(650), EasingValue.CubicOut);
    }
}
