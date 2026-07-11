using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.AsyncLoadingSample.Components;

public sealed class AsyncLoadingComponent : Component
{
    public override IElement Render()
    {
        var (requestId, setRequestId) = useState(0);
        var (state, setState) = useState(new LoadState(false, "Idle", Array.Empty<string>()));
        var stateRef = useLatest(state);

        void AddLog(string message)
        {
            var next = new[] { $"{DateTime.Now:HH:mm:ss} {message}" }.Concat(stateRef.Current.Logs).Take(10).ToArray();
            var updated = stateRef.Current with { Logs = next };
            stateRef.Current = updated;
            setState(_ => updated);
        }

        useEffect(() =>
        {
            if (requestId == 0)
                return null;

            var cancelled = false;
            var loading = stateRef.Current with { Loading = true, Result = $"Loading request #{requestId}..." };
            stateRef.Current = loading;
            setState(_ => loading);
            AddLog($"request #{requestId} started");

            _ = Task.Run(async () =>
            {
                await Task.Delay(900);
                if (cancelled)
                    return;

                var done = stateRef.Current with { Loading = false, Result = $"Request #{requestId} completed" };
                stateRef.Current = done;
                setState(_ => done);
            });

            return () =>
            {
                cancelled = true;
                AddLog($"request #{requestId} cleanup/cancel");
            };
        }, [requestId]);

        return Div(
                Text("Async / Loading").FontSize(26).FontWeight(FontWeightValue.Bold),
                Text("delayed state update, loading state, cancel/cleanup, stale response 방지 검증").FontColor("#6b7280").Margin(top: 6, bottom: 18),
                Div(
                        Text(state.Result).FontSize(18).FontWeight(FontWeightValue.Bold),
                        Text(state.Loading ? "Loading..." : "Ready").FontColor(state.Loading ? "#b45309" : "#047857").Margin(top: 8),
                        Grid(
                                Button("Start request", () => setRequestId(current => current + 1)).Height(36).Column(0),
                                Button("Start next quickly", () => setRequestId(current => current + 2)).Height(36).Column(1)
                            )
                            .Columns(Pixels(130), Pixels(150))
                            .Margin(top: 18)
                    )
                    .Padding(20)
                    .Background("#ffffff")
                    .Brush("#e5e7eb")
                    .Thickness(1)
                    .CornerRadius(16),
                Div(state.Logs.Select(log => (IElement)Text(log).FontSize(12).FontColor("#374151").Margin(top: 8)).ToArray())
                    .Padding(18)
                    .Margin(top: 16)
                    .Background("#ffffff")
                    .Brush("#e5e7eb")
                    .Thickness(1)
                    .CornerRadius(16))
            .Padding(24)
            .Background("#f3f4f6");
    }
}

internal sealed record LoadState(bool Loading, string Result, string[] Logs);
