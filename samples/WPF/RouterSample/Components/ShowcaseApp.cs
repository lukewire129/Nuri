using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace RouterSample.Components
{
    public sealed class ShowcaseApp : Component
    {
        public override IElement Render()
        {
            var (route, setRoute) = useState("counter");
            var (settingsRoute, setSettingsRoute) = useState("profile");

            return Div(DivTypes.Row,
                new SampleSidebar(route, setRoute),
                Div(DivTypes.Scroll,
                    Router(route,
                        Route("counter", () => new CounterPage()),
                        Route("form", () => new FormPage()),
                        Route("list", () => new KeyedListPage()),
                        Route("effects", () => new EffectsPage()),
                        Route("settings", () => new SettingsPage(settingsRoute, setSettingsRoute))))
                    .Background("#F6F4F0")
                    .Padding(28)
                    .Width(760))
                .Background("#F6F4F0");
        }
    }

    internal sealed class SampleSidebar : Component
    {
        private readonly string _selected;
        private readonly Action<string> _navigate;

        public SampleSidebar(string selected, Action<string> navigate)
        {
            _selected = selected;
            _navigate = navigate;
        }

        public override IElement Render()
        {
            return Div(
                Text("Nuri").FontSize(26).FontWeight(FontWeightValue.Bold).FontColor("#111827"),
                Text("WPF-first samples").FontSize(13).FontColor("#6B7280").Margin(top: 4, bottom: 28),
                new NavButton("counter", "Counter", _selected, _navigate),
                new NavButton("form", "Form", _selected, _navigate),
                new NavButton("list", "Keyed List", _selected, _navigate),
                new NavButton("effects", "Effects", _selected, _navigate),
                new NavButton("settings", "Nested Router", _selected, _navigate),
                Text("Small components, explicit keys, and platform-neutral routing.")
                    .FontSize(12)
                    .FontColor("#6B7280")
                    .Margin(top: 28))
                .Width(240)
                .Padding(24)
                .Background("#FFFFFF");
        }
    }

    internal sealed class NavButton : Component
    {
        private readonly string _route;
        private readonly string _label;
        private readonly string _selected;
        private readonly Action<string> _navigate;

        public NavButton(string route, string label, string selected, Action<string> navigate)
        {
            _route = route;
            _label = label;
            _selected = selected;
            _navigate = navigate;
        }

        public override IElement Render()
        {
            var active = string.Equals(_route, _selected, StringComparison.Ordinal);
            return Button(_label, () => _navigate(_route))
                .Key(_route)
                .Padding(12)
                .Margin(bottom: 8)
                .TextStart()
                .Background(active ? "#111827" : "#F3F4F6")
                .FontColor(active ? "#FFFFFF" : "#111827");
        }
    }

    internal sealed class CounterPage : Component
    {
        public override IElement Render()
        {
            var (count, setCount) = useState(0);

            return new PageFrame("Counter", "State lives in the page. Display and actions are separate components.",
                new CounterCard(count, () => setCount(count - 1), () => setCount(0), () => setCount(count + 1)));
        }
    }

    internal sealed class CounterCard : Component
    {
        private readonly int _count;
        private readonly Action _decrement;
        private readonly Action _reset;
        private readonly Action _increment;

        public CounterCard(int count, Action decrement, Action reset, Action increment)
        {
            _count = count;
            _decrement = decrement;
            _reset = reset;
            _increment = increment;
        }

        public override IElement Render()
        {
            return new Card(
                new Metric("Current value", _count.ToString()),
                Div(DivTypes.Row,
                    new QuietButton("-", _decrement),
                    new QuietButton("Reset", _reset),
                    new PrimaryButton("+", _increment))
                    .Margin(top: 20));
        }
    }

    internal sealed class FormPage : Component
    {
        public override IElement Render()
        {
            var (name, setName) = useState("Nuri");
            var (newsletter, setNewsletter) = useState(true);
            var (plan, setPlan) = useState("team");

            return new PageFrame("Form", "Controlled inputs keep form state visible and predictable.",
                new Card(
                    new FieldLabel("Name"),
                    TextBox(name, setName).Padding(10).Margin(bottom: 16).Width(320),
                    CheckBox("Send occasional updates", setNewsletter)
                        .Checked(newsletter)
                        .Margin(bottom: 16),
                    new FieldLabel("Plan"),
                    Div(DivTypes.Row,
                        RadioButton("Solo", selected => { if (selected) setPlan("solo"); }).Group("plan").Checked(plan == "solo").Margin(right: 16),
                        RadioButton("Team", selected => { if (selected) setPlan("team"); }).Group("plan").Checked(plan == "team").Margin(right: 16),
                        RadioButton("Enterprise", selected => { if (selected) setPlan("enterprise"); }).Group("plan").Checked(plan == "enterprise")),
                    Text($"Preview: {name}, {plan}, updates {(newsletter ? "on" : "off")}")
                        .FontColor("#374151")
                        .Margin(top: 22)));
        }
    }

    internal sealed class KeyedListPage : Component
    {
        public override IElement Render()
        {
            var (items, setItems) = useState(new List<TaskItem>
            {
                new TaskItem("design", "Refine sample layout"),
                new TaskItem("router", "Add nested router"),
                new TaskItem("tests", "Cover keyed moves")
            });
            var (next, setNext) = useState(1);

            void AddItem()
            {
                setItems(items.Concat(new[] { new TaskItem($"new-{next}", $"New task {next}") }).ToList());
                setNext(next + 1);
            }

            void MoveFirstToEnd()
            {
                if (items.Count < 2)
                    return;

                setItems(items.Skip(1).Concat(items.Take(1)).ToList());
            }

            return new PageFrame("Keyed List", "Explicit keys preserve subtree identity during add, remove, and move.",
                new Card(
                    Div(DivTypes.Row,
                        new PrimaryButton("Add", AddItem),
                        new QuietButton("Move first to end", MoveFirstToEnd))
                        .Margin(bottom: 16),
                    Div(items.Select(item => (IElement)new TaskRow(item, () => setItems(items.Where(candidate => candidate.Id != item.Id).ToList())).Key(item.Id)).ToArray())));
        }
    }

    internal sealed class EffectsPage : Component
    {
        public override IElement Render()
        {
            var (visible, setVisible) = useState(true);
            var (version, setVersion) = useState(1);
            var (log, setLog) = useState(new List<string>());

            void AddLog(string message)
            {
                setLog(new[] { message }.Concat(log).Take(5).ToList());
            }

            return new PageFrame("Effects", "Effect setup runs after commit. Cleanup runs before rerun and on unmount.",
                new Card(
                    Div(DivTypes.Row,
                        new QuietButton(visible ? "Unmount probe" : "Mount probe", () => setVisible(!visible)),
                        new PrimaryButton("Change dependency", () => setVersion(version + 1)))
                        .Margin(bottom: 16),
                    visible ? new EffectProbe(version, AddLog) : Text("Probe is unmounted.").FontColor("#6B7280"),
                    Div(log.Select(entry => (IElement)Text(entry).FontSize(13).FontColor("#374151").Margin(top: 8)).ToArray())
                        .Margin(top: 20)));
        }
    }

    internal sealed class SettingsPage : Component
    {
        private readonly string _route;
        private readonly Action<string> _navigate;

        public SettingsPage(string route, Action<string> navigate)
        {
            _route = route;
            _navigate = navigate;
        }

        public override IElement Render()
        {
            return new PageFrame("Nested Router", "The settings page owns a second router for local navigation.",
                new Card(
                    Div(DivTypes.Row,
                        new SmallTab("profile", "Profile", _route, _navigate),
                        new SmallTab("theme", "Theme", _route, _navigate))
                        .Margin(bottom: 18),
                    Router(_route,
                        Route("profile", () => new SettingsPanel("Profile", "Nested routes are just component selection.")),
                        Route("theme", () => new SettingsPanel("Theme", "A future platform can reuse the same router.")))));
        }
    }

    internal sealed class EffectProbe : Component
    {
        private readonly int _version;
        private readonly Action<string> _addLog;

        public EffectProbe(int version, Action<string> addLog)
        {
            _version = version;
            _addLog = addLog;
        }

        public override IElement Render()
        {
            useEffect(() =>
            {
                _addLog($"effect setup v{_version}");
                return () => _addLog($"effect cleanup v{_version}");
            }, _version);

            return Text($"Probe mounted. Dependency version: {_version}").FontColor("#111827");
        }
    }

    internal sealed class TaskRow : Component
    {
        private readonly TaskItem _item;
        private readonly Action _remove;

        public TaskRow(TaskItem item, Action remove)
        {
            _item = item;
            _remove = remove;
        }

        public override IElement Render()
        {
            return Div(DivTypes.Row,
                Text(_item.Title).FontColor("#111827").Width(360),
                new QuietButton("Remove", _remove))
                .Padding(12)
                .Margin(bottom: 8)
                .Background("#F9FAFB");
        }
    }

    internal sealed class PageFrame : Component
    {
        private readonly string _title;
        private readonly string _description;
        private readonly IElement[] _children;

        public PageFrame(string title, string description, params IElement[] children)
        {
            _title = title;
            _description = description;
            _children = children;
        }

        public override IElement Render()
        {
            return Div(
                Text(_title).FontSize(30).FontWeight(FontWeightValue.Bold).FontColor("#111827"),
                Text(_description).FontSize(14).FontColor("#6B7280").Margin(top: 6, bottom: 24),
                Div(_children));
        }
    }

    internal sealed class Card : Component
    {
        private readonly IElement[] _children;

        public Card(params IElement[] children)
        {
            _children = children;
        }

        public override IElement Render()
        {
            return Div(_children)
                .Padding(24)
                .Background("#FFFFFF")
                .Brush("#E5E7EB")
                .Thickness(1)
                .CornerRadius(14);
        }
    }

    internal sealed class Metric : Component
    {
        private readonly string _label;
        private readonly string _value;

        public Metric(string label, string value)
        {
            _label = label;
            _value = value;
        }

        public override IElement Render()
        {
            return Div(
                Text(_label).FontSize(13).FontColor("#6B7280"),
                Text(_value).FontSize(44).FontWeight(FontWeightValue.Bold).FontColor("#111827"));
        }
    }

    internal sealed class FieldLabel : Component
    {
        private readonly string _text;

        public FieldLabel(string text)
        {
            _text = text;
        }

        public override IElement Render()
        {
            return Text(_text).FontSize(13).FontWeight(FontWeightValue.Bold).FontColor("#374151").Margin(bottom: 8);
        }
    }

    internal sealed class PrimaryButton : Component
    {
        private readonly string _label;
        private readonly Action _action;

        public PrimaryButton(string label, Action action)
        {
            _label = label;
            _action = action;
        }

        public override IElement Render()
        {
            return Button(_label, _action).Padding(10).Margin(right: 8).Background("#111827").FontColor("#FFFFFF");
        }
    }

    internal sealed class QuietButton : Component
    {
        private readonly string _label;
        private readonly Action _action;

        public QuietButton(string label, Action action)
        {
            _label = label;
            _action = action;
        }

        public override IElement Render()
        {
            return Button(_label, _action).Padding(10).Margin(right: 8).Background("#F3F4F6").FontColor("#111827");
        }
    }

    internal sealed class SmallTab : Component
    {
        private readonly string _route;
        private readonly string _label;
        private readonly string _selected;
        private readonly Action<string> _navigate;

        public SmallTab(string route, string label, string selected, Action<string> navigate)
        {
            _route = route;
            _label = label;
            _selected = selected;
            _navigate = navigate;
        }

        public override IElement Render()
        {
            var active = string.Equals(_route, _selected, StringComparison.Ordinal);
            return Button(_label, () => _navigate(_route))
                .Padding(10)
                .Margin(right: 8)
                .Background(active ? "#E5E7EB" : "#FFFFFF")
                .FontColor("#111827");
        }
    }

    internal sealed class SettingsPanel : Component
    {
        private readonly string _title;
        private readonly string _body;

        public SettingsPanel(string title, string body)
        {
            _title = title;
            _body = body;
        }

        public override IElement Render()
        {
            return Div(
                Text(_title).FontSize(18).FontWeight(FontWeightValue.Bold).FontColor("#111827"),
                Text(_body).FontColor("#6B7280").Margin(top: 8));
        }
    }

    internal sealed class TaskItem
    {
        public TaskItem(string id, string title)
        {
            Id = id;
            Title = title;
        }

        public string Id { get; }

        public string Title { get; }
    }
}
