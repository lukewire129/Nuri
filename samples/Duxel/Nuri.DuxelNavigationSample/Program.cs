using Duxel.Core;
using Nuri.Duxel;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Navigation;
using Nuri.UI.Values;

NuriApplication.Run<NavigationSampleApp>(
    title: "Nuri Duxel Navigation",
    width: 900,
    height: 640,
    theme: UiTheme.Nord);

internal sealed class NavigationSampleApp : Component
{
    public override IElement Render()
    {
        var (navigation, navigator) = useNavigation("home");

        return Div(
                DivTypes.Scroll,
                Div(
                    Text("Duxel screen navigation")
                        .FontSize(28)
                        .FontColor("#F8FAFC"),
                    Text("Core routing with Navigate, Replace, and GoBack.")
                        .FontColor("#94A3B8")
                        .Margin(top: 6, bottom: 18),
                    Div(
                            DivTypes.Row,
                            new NavigationButton("home", "Home", navigation.CurrentRoute, navigator)
                                .Key("nav-home"),
                            new NavigationButton("details", "Details", navigation.CurrentRoute, navigator)
                                .Key("nav-details"),
                            new NavigationButton("settings", "Settings", navigation.CurrentRoute, navigator)
                                .Key("nav-settings"))
                        .Spacing(10),
                    Div(
                            DivTypes.Row,
                            Button("Back", navigator.GoBack)
                                .Size(90, 34)
                                .Background(navigator.CanGoBack ? "#334155" : "#1E293B"),
                            Text($"Current: {navigation.CurrentRoute} | Back stack: {navigation.BackStack.Count}")
                                .FontColor("#CBD5E1"))
                        .Spacing(12)
                        .Margin(top: 14, bottom: 18),
                    Router(
                        navigation,
                        Route("home", () => new HomeScreen(navigator)),
                        Route("details", () => new DetailsScreen(navigator)),
                        Route("settings", () => new SettingsScreen(navigator))))
                    .Spacing(12))
            .Padding(24)
            .Background("#0B1120");
    }
}

internal sealed class NavigationButton : Component
{
    private readonly string _route;
    private readonly string _label;
    private readonly string _currentRoute;
    private readonly Navigator _navigator;

    public NavigationButton(string route, string label, string currentRoute, Navigator navigator)
    {
        _route = route;
        _label = label;
        _currentRoute = currentRoute;
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var active = string.Equals(_route, _currentRoute, StringComparison.OrdinalIgnoreCase);

        return Button(_label, () => _navigator.Navigate(_route))
            .Size(120, 38)
            .Background(active ? "#2563EB" : "#1E293B")
            .FontColor("#F8FAFC");
    }
}

internal sealed class HomeScreen : Component
{
    private readonly Navigator _navigator;

    public HomeScreen(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (count, setCount) = useState(0);

        return Div(new ScreenSurface(
            "Home",
            "Navigate pushes this route onto the history stack.",
            Text($"Home-local counter: {count}")
                .FontSize(20)
                .FontColor("#FDE68A"),
            Div(
                    DivTypes.Row,
                    Button("Increment", () => setCount(current => current + 1))
                        .Size(110, 36),
                    Button("Open details", () => _navigator.Navigate("details"))
                        .Size(130, 36))
                .Spacing(10),
            Text("Return after leaving this route to see that an unmounted screen starts with fresh local state.")
                .FontColor("#94A3B8")));
    }
}

internal sealed class DetailsScreen : Component
{
    private readonly Navigator _navigator;

    public DetailsScreen(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (name, setName) = useState("Nuri");

        return Div(new ScreenSurface(
            "Details",
            "This screen owns controlled input state.",
            Text("Display name").FontColor("#CBD5E1"),
            TextBox(name, value => setName(_ => value))
                .Width(360),
            Text($"Hello, {name}.")
                .FontSize(18)
                .FontColor("#F8FAFC"),
            Div(
                    DivTypes.Row,
                    Button("Navigate settings", () => _navigator.Navigate("settings"))
                        .Size(150, 36),
                    Button("Replace with home", () => _navigator.Replace("home"))
                        .Size(150, 36))
                .Spacing(10),
            Text("Replace changes the current route without adding it to the back stack.")
                .FontColor("#94A3B8")));
    }
}

internal sealed class SettingsScreen : Component
{
    private readonly Navigator _navigator;

    public SettingsScreen(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (notifications, setNotifications) = useState(true);

        return Div(new ScreenSurface(
            "Settings",
            "GoBack restores the previous route from the navigation history.",
            CheckBox("Enable notifications", value => setNotifications(_ => value))
                .Checked(notifications),
            Text($"Notifications are {(notifications ? "enabled" : "disabled")}.")
                .FontColor("#F8FAFC"),
            Div(
                    DivTypes.Row,
                    Button("Go back", _navigator.GoBack)
                        .Size(110, 36),
                    Button("Replace with home", () => _navigator.Replace("home"))
                        .Size(150, 36))
                .Spacing(10)));
    }
}

internal sealed class ScreenSurface : Component
{
    private readonly string _title;
    private readonly string _description;
    private readonly IElement[] _children;

    public ScreenSurface(string title, string description, params IElement[] children)
    {
        _title = title;
        _description = description;
        _children = children;
    }

    public override IElement Render()
    {
        var (entered, setEntered) = useState(false);

        useEffect(() => setEntered(_ => true), []);

        return Div(
                Text(_title)
                    .FontSize(24)
                    .FontColor("#F8FAFC"),
                Text(_description)
                    .FontColor("#94A3B8")
                    .Margin(top: 4, bottom: 18),
                Div(_children).Spacing(14))
            .Padding(22)
            .Background("#111827")
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(14)
            .Opacity(entered ? 1.0 : 0.05)
            .Transition(TimeSpan.FromMilliseconds(220), EasingValue.CubicOut);
    }
}
