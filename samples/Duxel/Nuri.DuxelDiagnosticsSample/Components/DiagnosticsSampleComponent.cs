using Nuri.Runtime;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.DuxelDiagnosticsSample.Components;

public sealed class DiagnosticsSampleComponent : Component
{
    public override IElement Render()
    {
        var (detailVisible, setDetailVisible) = useState(true);

        return Div(
                DivTypes.Scroll,
                Div(
                    Text("Duxel Runtime Diagnostics Sample")
                        .FontSize(26)
                        .FontWeight(FontWeightValue.Bold)
                        .FontColor("#F8FAFC"),
                    Text("Press F12 to open the Duxel diagnostics window.")
                        .FontColor("#CBD5E1")
                        .Margin(top: 6, bottom: 18),
                    Div(
                            DivTypes.Row,
                            Button(detailVisible ? "Unmount detail" : "Mount detail", () =>
                                    setDetailVisible(current => !current))
                                .Size(150, 38),
                            Button("Increment store", DiagnosticsActions.Increment)
                                .Size(140, 38),
                            Button("Write console log", DiagnosticsActions.WriteLog)
                                .Size(160, 38))
                        .Spacing(10),
                    Div(
                            new StoreSummaryComponent(),
                            detailVisible
                                ? new DetailComponent().Key("diagnostics-detail")
                                : Text("Detail component is unmounted.")
                                    .FontColor("#94A3B8"))
                        .Spacing(12)
                        .Margin(top: 18),
                    Text("Inspect component hooks, Store subscriptions, render counts, patches, and console events.")
                        .FontColor("#94A3B8")
                        .Margin(top: 18))
                    .Spacing(12))
            .Padding(24)
            .Background("#0F172A");
    }
}

internal static class DiagnosticsStore
{
    public static readonly Store<int> Count = new(0);
}

internal static class DiagnosticsActions
{
    public static void Increment()
    {
        DiagnosticsStore.Count.Set(DiagnosticsStore.Count.Value + 1);
        Console.WriteLine($"Duxel diagnostics store count: {DiagnosticsStore.Count.Value}");
    }

    public static void WriteLog()
    {
        Console.WriteLine($"Duxel diagnostics sample log at {DateTime.Now:HH:mm:ss}");
    }
}

internal sealed class StoreSummaryComponent : Component
{
    public override IElement Render()
    {
        var count = useStore(DiagnosticsStore.Count);
        return Div(
                Text("Shared Store").FontWeight(FontWeightValue.Bold).FontColor("#F8FAFC"),
                Text($"Count: {count}").FontSize(22).FontColor("#FDE68A"))
            .Padding(16)
            .Background("#1E293B")
            .Brush("#475569")
            .Thickness(1)
            .CornerRadius(10);
    }
}

internal sealed class DetailComponent : Component
{
    public override IElement Render()
    {
        useEffect(() =>
        {
            Console.WriteLine("Duxel diagnostics detail mounted.");
            return () => Console.WriteLine("Duxel diagnostics detail unmounted.");
        }, []);

        var mountedAt = useMemo(() => DateTime.Now.ToString("HH:mm:ss"), []);
        var (localCount, setLocalCount) = useState(0);

        return Div(
                Text("Keyed detail component").FontWeight(FontWeightValue.Bold).FontColor("#F8FAFC"),
                Text($"Mounted at: {mountedAt}").FontColor("#CBD5E1"),
                Text($"Local state: {localCount}").FontColor("#CBD5E1"),
                Button("Increment local state", () => setLocalCount(current => current + 1))
                    .Size(170, 36))
            .Spacing(8)
            .Padding(16)
            .Background("#1E293B")
            .Brush("#475569")
            .Thickness(1)
            .CornerRadius(10);
    }
}
