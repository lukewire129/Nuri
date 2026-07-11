using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Navigation;
using Nuri.UI.Values;

namespace RouterSample.Components
{
    public sealed class ShowcaseApp : Component
    {
        public override IElement Render()
        {
            var (navigation, navigator) = useNavigation("counter");

            return Div(DivTypes.Row,
                new SampleSidebar(navigation, navigator),
                Div(DivTypes.Scroll,
                    Router(navigation,
                        Route("counter", () => new CounterPage()),
                        Route("form", () => new FormPage()),
                        Route("list", () => new KeyedListPage()),
                        Route("navigation", () => new NavigationPage()),
                        Route("effects", () => new EffectsPage()),
                        Route("settings", () => new SettingsPage())))
                    .Background("#F6F4F0")
                    .Padding(28)
                    .Width(760))
                .Background("#F6F4F0");
        }
    }

    internal sealed class SampleSidebar : Component
    {
        private readonly string _selected;
        private readonly Navigator _navigator;

        public SampleSidebar(NavigationState navigation, Navigator navigator)
        {
            _selected = navigation.CurrentRoute;
            _navigator = navigator;
        }

        public override IElement Render()
        {
            return Div(
                Text("Nuri").FontSize(26).FontWeight(FontWeightValue.Bold).FontColor("#111827"),
                Text("WPF-first samples").FontSize(13).FontColor("#6B7280").Margin(top: 4, bottom: 28),
                new NavButton("counter", "Counter", _selected, _navigator),
                new NavButton("form", "Form", _selected, _navigator),
                new NavButton("list", "Keyed List", _selected, _navigator),
                new NavButton("navigation", "useNavigation", _selected, _navigator),
                new NavButton("effects", "Effects", _selected, _navigator),
                new NavButton("settings", "Nested Router", _selected, _navigator),
                new QuietButton("Back", _navigator.GoBack)
                    .Margin(top: 12)
                    .Background(_navigator.CanGoBack ? "#F3F4F6" : "#FFFFFF"),
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
        private readonly Navigator _navigator;

        public NavButton(string route, string label, string selected, Navigator navigator)
        {
            _route = route;
            _label = label;
            _selected = selected;
            _navigator = navigator;
        }

        public override IElement Render()
        {
            var active = string.Equals(_route, _selected, StringComparison.Ordinal);
            return Button(_label, () => _navigator.Navigate(_route))
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
                new CounterCard(count, () => setCount(current => current - 1), () => setCount(_ => 0), () => setCount(current => current + 1)));
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
                    TextBox(name, value => setName(_ => value)).Padding(10).Margin(bottom: 16).Width(320),
                    CheckBox("Send occasional updates", value => setNewsletter(_ => value))
                        .Checked(newsletter)
                        .Margin(bottom: 16),
                    new FieldLabel("Plan"),
                    Div(DivTypes.Row,
                        RadioButton("Solo", selected => { if (selected) setPlan(_ => "solo"); }).Group("plan").Checked(plan == "solo").Margin(right: 16),
                        RadioButton("Team", selected => { if (selected) setPlan(_ => "team"); }).Group("plan").Checked(plan == "team").Margin(right: 16),
                        RadioButton("Enterprise", selected => { if (selected) setPlan(_ => "enterprise"); }).Group("plan").Checked(plan == "enterprise")),
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
                setItems(current => current.Concat(new[] { new TaskItem($"new-{next}", $"New task {next}") }).ToList());
                setNext(current => current + 1);
            }

            void MoveFirstToEnd()
            {
                if (items.Count < 2)
                    return;

                setItems(current => current.Skip(1).Concat(current.Take(1)).ToList());
            }

            return new PageFrame("Keyed List", "Explicit keys preserve subtree identity during add, remove, and move.",
                new Card(
                    Div(DivTypes.Row,
                        new PrimaryButton("Add", AddItem),
                        new QuietButton("Move first to end", MoveFirstToEnd))
                        .Margin(bottom: 16),
                    Div(items.Select(item => (IElement)new TaskRow(item, () => setItems(current => current.Where(candidate => candidate.Id != item.Id).ToList())).Key(item.Id)).ToArray())));
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
                setLog(current => new[] { message }.Concat(current).Take(5).ToList());
            }

            return new PageFrame("Effects", "Effect setup runs after commit. Cleanup runs before rerun and on unmount.",
                new Card(
                    Div(DivTypes.Row,
                        new QuietButton(visible ? "Unmount probe" : "Mount probe", () => setVisible(current => !current)),
                        new PrimaryButton("Change dependency", () => setVersion(current => current + 1)))
                        .Margin(bottom: 16),
                    visible ? new EffectProbe(version, AddLog) : Text("Probe is unmounted.").FontColor("#6B7280"),
                    Div(log.Select(entry => (IElement)Text(entry).FontSize(13).FontColor("#374151").Margin(top: 8)).ToArray())
                        .Margin(top: 20)));
        }
    }

    internal sealed class NavigationPage : Component
    {
        public override IElement Render()
        {
            var (navigation, navigator) = useNavigation("overview");

            return new PageFrame("useNavigation", "A local navigation state can drive any Router without platform APIs.",
                new Card(
                    new FieldLabel("Current route"),
                    Text(navigation.CurrentRoute).FontSize(22).FontWeight(FontWeightValue.Bold).FontColor("#111827"),
                    Text($"Back stack count: {navigation.BackStack.Count}").FontColor("#6B7280").Margin(top: 6, bottom: 18),
                    Div(DivTypes.Row,
                        new PrimaryButton("Navigate details", () => navigator.Navigate("details")),
                        new QuietButton("Replace summary", () => navigator.Replace("summary")),
                        new QuietButton("Go back", navigator.GoBack)
                            .Background(navigator.CanGoBack ? "#F3F4F6" : "#FFFFFF"))
                        .Margin(bottom: 22),
                    Router(navigation,
                        Route("overview", () => new NavigationPanel("Overview", "Navigate pushes the current route onto the back stack.")),
                        Route("details", () => new NavigationPanel("Details", "GoBack returns to the previous route.")),
                        Route("summary", () => new NavigationPanel("Summary", "Replace changes the current route without adding history."))),
                    new CodeBlock("var (navigation, navigator) = useNavigation(\"overview\");\n\nRouter(navigation,\n    Route(\"overview\", () => new OverviewPage()),\n    Route(\"details\", () => new DetailsPage()));\n\nnavigator.Navigate(\"details\");\nnavigator.GoBack();")));
        }
    }

    internal sealed class SettingsPage : Component
    {
        public override IElement Render()
        {
            var (navigation, navigator) = useNavigation("profile");

            return new PageFrame("Nested Router", "The settings page owns a second router for local navigation.",
                new Card(
                    Div(DivTypes.Row,
                        new SmallTab("profile", "Profile", navigation.CurrentRoute, navigator),
                        new SmallTab("theme", "Theme", navigation.CurrentRoute, navigator),
                        new QuietButton("Back", navigator.GoBack)
                            .Background(navigator.CanGoBack ? "#F3F4F6" : "#FFFFFF"))
                        .Margin(bottom: 18),
                    Router(navigation,
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
        private readonly Navigator _navigator;

        public SmallTab(string route, string label, string selected, Navigator navigator)
        {
            _route = route;
            _label = label;
            _selected = selected;
            _navigator = navigator;
        }

        public override IElement Render()
        {
            var active = string.Equals(_route, _selected, StringComparison.Ordinal);
            return Button(_label, () => _navigator.Navigate(_route))
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

    internal sealed class NavigationPanel : Component
    {
        private readonly string _title;
        private readonly string _body;

        public NavigationPanel(string title, string body)
        {
            _title = title;
            _body = body;
        }

        public override IElement Render()
        {
            return Div(
                Text(_title).FontSize(18).FontWeight(FontWeightValue.Bold).FontColor("#111827"),
                Text(_body).FontColor("#6B7280").Margin(top: 8))
                .Padding(16)
                .Background("#F9FAFB")
                .Margin(bottom: 18);
        }
    }

    internal sealed class CodeBlock : Component
    {
        private readonly string _code;

        public CodeBlock(string code)
        {
            _code = code;
        }

        public override IElement Render()
        {
            return Text(_code)
                .FontFamily("Consolas")
                .FontSize(13)
                .FontColor("#111827")
                .Padding(16)
                .Background("#F3F4F6");
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
