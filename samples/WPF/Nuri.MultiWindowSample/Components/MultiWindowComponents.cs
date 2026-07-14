using System.Threading;
using Nuri.Runtime;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.WPF;

namespace Nuri.MultiWindowSample.Components;

public sealed class MultiWindowLauncherComponent : Component
{
    public override IElement Render()
    {
        var (openedWindowCount, setOpenedWindowCount) = useState(0);
        var sharedCount = useStore(MultiWindowState.SharedCount);

        void OpenCounterWindow()
        {
            NuriApplication.Show<CounterWindowComponent>(
                "Nuri Counter Window",
                width: 460,
                height: 430);
            setOpenedWindowCount(current => current + 1);
        }

        return Div(
                Text("Multi-Window Lifecycle")
                    .FontSize(30)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc"),
                Text("Each window owns local hook state while every root observes the same Store.")
                    .FontSize(14)
                    .FontColor("#94a3b8")
                    .Margin(top: 8, bottom: 24),
                StatusCard("Windows opened from this launcher", openedWindowCount.ToString()),
                StatusCard("Shared count", sharedCount.ToString())
                    .Margin(top: 12),
                Button("Open counter window", OpenCounterWindow)
                    .Height(44)
                    .Padding(18, 0, 18, 0)
                    .Margin(top: 24)
                    .Background("#2563eb")
                    .FontColor("#ffffff")
                    .Brush("#1d4ed8")
                    .Thickness(1),
                Button("Increment shared count", MultiWindowState.IncrementShared)
                    .Height(44)
                    .Padding(18, 0, 18, 0)
                    .Margin(top: 10)
                    .Background("#0f766e")
                    .FontColor("#ffffff")
                    .Brush("#115e59")
                    .Thickness(1))
            .Padding(32)
            .Background("#0b1120");
    }

    private static Div StatusCard(string label, string value)
    {
        return Div(
                Text(label)
                    .FontSize(13)
                    .FontColor("#94a3b8"),
                Text(value)
                    .FontSize(26)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc")
                    .Margin(top: 6))
            .Padding(16)
            .Background("#111827")
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(12);
    }
}

public sealed class CounterWindowComponent : Component
{
    private static int _nextWindowNumber;
    private readonly int _windowNumber = Interlocked.Increment(ref _nextWindowNumber);

    public override IElement Render()
    {
        var (localCount, setLocalCount) = useState(0);
        var sharedCount = useStore(MultiWindowState.SharedCount);

        useEffect(() =>
        {
            Console.WriteLine($"[MultiWindow] mounted window {_windowNumber}");
            return () => Console.WriteLine($"[MultiWindow] cleaned window {_windowNumber}");
        }, []);

        return Div(
                Text($"Counter Window #{_windowNumber}")
                    .FontSize(28)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc"),
                Text("Close this window and keep using the others to verify root cleanup.")
                    .FontSize(13)
                    .FontColor("#94a3b8")
                    .Margin(top: 8, bottom: 24),
                CounterCard("Local to this window", localCount),
                CounterCard("Shared by every window", sharedCount)
                    .Margin(top: 12),
                Button("Increment local", () => setLocalCount(current => current + 1))
                    .Height(42)
                    .Margin(top: 22)
                    .Background("#7c3aed")
                    .FontColor("#ffffff")
                    .Brush("#6d28d9")
                    .Thickness(1),
                Button("Increment shared", MultiWindowState.IncrementShared)
                    .Height(42)
                    .Margin(top: 10)
                    .Background("#0f766e")
                    .FontColor("#ffffff")
                    .Brush("#115e59")
                    .Thickness(1))
            .Padding(28)
            .Background("#0b1120");
    }

    private static Div CounterCard(string label, int value)
    {
        return Div(
                Text(label)
                    .FontSize(13)
                    .FontColor("#94a3b8"),
                Text(value.ToString())
                    .FontSize(28)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc")
                    .Margin(top: 6))
            .Padding(16)
            .Background("#111827")
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(12);
    }
}

internal static class MultiWindowState
{
    public static readonly Store<int> SharedCount = new Store<int>(0);

    public static void IncrementShared()
    {
        SharedCount.Set(SharedCount.Value + 1);
    }
}
