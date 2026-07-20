using Duxel.Core;
using Nuri.Duxel;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;

NuriApplication.Run(
    UiCompiledDesign.Windows11Dark.Theme,
    theme => new CounterComponent(theme),
    title: "Nuri + Duxel",
    width: 720,
    height: 580);

internal sealed class CounterComponent(UiTheme theme) : Component
{
    public override IElement Render()
    {
        var (count, setCount) = useState(0);
        var (name, setName) = useState("Duxel");
        var (enabled, setEnabled) = useState(true);

        return Div(
            Text("Nuri.Duxel layout integration")
                .FontSize(24)
                .FontColor("#7DD3FC"),
            Grid(
                Text("Name").Row(0).Column(0),
                TextBox(name, value => setName(_ => value)).Row(0).Column(1).Width(320),
                Text("Counter").Row(1).Column(0),
                Text($"{count}").Row(1).Column(1).FontSize(20).FontColor("#FDE68A"),
                Text("Enabled").Row(2).Column(0),
                CheckBox("Counter enabled", value => setEnabled(_ => value))
                    .Checked(enabled)
                    .Row(2)
                    .Column(1))
                .Columns(120, Star)
                .Rows(Auto,Auto, Auto)
                .Spacing(10)
                .Padding(8),
            Div(
                DivTypes.Row,
                Button("Increment", () => setCount(current => enabled ? current + 1 : current))
                    .Size(120, 34)
                    .Background("#0369A1"),
                Button("Reset", () => setCount(_ => 0))
                        .Size(90, 34))
                        .Spacing(12),
                Div(
                    DivTypes.Scroll,
                    Div(
                    Text($"Hello, {name}. The content below is rendered inside a Duxel child region."),
                    Text("Scroll validates clipping and vertical wheel input."),
                    Text("Nuri state changes still use dirty component subtree rendering."),
                    Text("Grid columns use Nuri pixel, star, and auto length descriptions."),
                    Text("Padding, spacing, font size, and solid foreground colors are scoped."),
                    Text($"Current counter value: {count}"),
                    Text("End of scroll content."))
                )
                .Padding(10)
                .Brush("#475569")
                .Thickness(1)
            )
            .Background(theme.WindowBg)
            .Padding(16)
            .Spacing(14);
    }
}
