using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.AvaloniaHotReloadSample.Components;

public sealed class HotReloadProbeComponent : Component
{
    public override IElement Render()
    {
        var (count, setCount) = useState(0);
        var (count2, setCount2) = useState(0);

        return Div(
                Text("Avalonia renderer smoke test")
                    .FontSize(28)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc"),
                Text("Edit this text, color, or layout while the app is running. Nuri should rebuild this tree after C# Hot Reload applies metadata updates.")
                    .FontSize(14)
                    .FontColor("#cbd5e1")
                    .Margin(top: 12),
                Div(
                        Text($"Count: {count}")
                            .FontSize(44)
                            .FontWeight(FontWeightValue.Bold)
                            .FontColor("#93c5fd"),
                        Text ($"Count: {count2}")
                            .FontSize (44)
                            .FontWeight (FontWeightValue.Bold)
                            .FontColor ("#93c5fd"),
                        Text ("The button uses Core-neutral OnClick and the Avalonia adapter materializes it.")
                            .FontSize(13)
                            .FontColor("#94a3b8")
                            .Margin(top: 10),
                        Button("Increment", () => setCount(current => current + 1))
                            .Height(44)
                            .Padding(18, 0, 18, 0)
                            .Margin(top: 18)
                            .Background("#2563eb")
                            .FontColor("#ffffff")
                            .Brush("#1d4ed8")
                            .Thickness(1),
                        Button ("Increment1", () => setCount2(current => current + 1))
                            .Height (44)
                            .Padding (18, 0, 18, 0)
                            .Margin (top: 18)
                            .Background ("#2563eb")
                            .FontColor ("#ffffff")
                            .Brush ("#1d4ed8")
                            .Thickness (1)
                    )
                    .Padding(22)
                    .Margin(top: 24)
                    .Background("#111827")
                    .Brush("#334155")
                    .Thickness(1)
                    .CornerRadius(18),
                Text("Hot reload probe: change this sentence and save while running.")
                    .FontSize(12)
                    .FontColor("#64748b")
                    .Margin(top: 20)
            )
            .Padding(32)
            .Background("#0b1120");
    }
}
