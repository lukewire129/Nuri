using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.TabsNavigationSample.Components;

public sealed class TabsNavigationComponent : Component
{
    public override IElement Render()
    {
        var (active, setActive) = useState("profile");
        var (keepMounted, setKeepMounted) = useState(false);
        var (logs, setLogs) = useState(Array.Empty<string>());
        var logsRef = useLatest(logs);

        void AddLog(string message)
        {
            var next = new[] { $"{DateTime.Now:HH:mm:ss} {message}" }.Concat(logsRef.Current).Take(12).ToArray();
            logsRef.Current = next;
            setLogs(next);
        }

        var panels = keepMounted
            ? new IElement[]
            {
                Panel("profile", active, AddLog),
                Panel("billing", active, AddLog),
                Panel("security", active, AddLog)
            }
            : new[] { Panel(active, active, AddLog) };

        return Grid(Rows(Auto, Star),
                Div(
                    Text("Tabs / Navigation").FontSize(26).FontWeight(FontWeightValue.Bold),
                    Text("tab mount 유지 여부, unmount cleanup, nested state, route key 검증").FontColor("#6b7280").Margin(top: 6, bottom: 16),
                    Div(DivTypes.Row,
                        Tab("Profile", "profile", active, setActive),
                        Tab("Billing", "billing", active, setActive),
                        Tab("Security", "security", active, setActive),
                        ToggleButton(keepMounted ? "Keep mounted" : "Unmount inactive", setKeepMounted).Checked(keepMounted).Height(34).Margin(left: 16))
                ).Row(0),
                Grid(
                        Div(panels).Column(0),
                        Div(DivTypes.Scroll,
                                logs.Length == 0
                                    ? Text("No lifecycle logs yet.").FontColor("#6b7280")
                                    : Div(logs.Select(log => (IElement)Text(log).FontSize(12).FontColor("#374151").Margin(bottom: 6)).ToArray()))
                            .Padding(16)
                            .Background("#ffffff")
                            .Brush("#e5e7eb")
                            .Thickness(1)
                            .CornerRadius(16)
                            .Column(1)
                    )
                    .Columns(Star, Pixels(280))
                    .Row(1))
            .Padding(24)
            .Background("#f3f4f6");
    }

    private static IElement Tab(string label, string key, string active, Action<string> setActive)
    {
        var selected = key == active;
        return Button(label, () => setActive(key)).Height(34).Margin(right: 8).Background(selected ? "#dbeafe" : "#ffffff").Brush(selected ? "#93c5fd" : "#d1d5db").Thickness(1);
    }

    private static IElement Panel(string key, string active, Action<string> addLog)
    {
        return new TabPanel(key, active == key, addLog).Key(key);
    }
}

internal sealed class TabPanel : Component
{
    private readonly string _key;
    private readonly bool _visible;
    private readonly Action<string> _addLog;

    public TabPanel(string key, bool visible, Action<string> addLog)
    {
        _key = key;
        _visible = visible;
        _addLog = addLog;
    }

    public override IElement Render()
    {
        var (draft, setDraft) = useState($"{_key} local state");

        useEffect(() =>
        {
            _addLog($"{_key} mounted");
            return () => _addLog($"{_key} cleanup");
        }, []);

        return Div(
                Text(_visible ? _key.ToUpperInvariant() : _key + " (hidden but mounted)").FontSize(20).FontWeight(FontWeightValue.Bold),
                TextBox(draft, setDraft).Key("draft-" + _key).Height(36).Padding(10, 0, 10, 0).TextStart().TextVCenter().Margin(top: 14),
                Text("Switch tabs and check whether this state survives.").FontColor("#6b7280").Margin(top: 10))
            .Padding(20)
            .Margin(bottom: _visible ? 0 : 8, right: 16)
            .Background(_visible ? "#ffffff" : "#f9fafb")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }
}
