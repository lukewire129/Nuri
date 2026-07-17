using Duxel.Core;
using Nuri.Duxel;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Navigation;

NuriApplication.Run<RouterSampleApp>(
    title: "Nuri Duxel Router",
    width: 840,
    height: 600,
    theme: UiTheme.Nord);

internal sealed class RouterSampleApp : Component
{
    public override IElement Render()
    {
        var (navigation, navigator) = useNavigation("home");

        return Div(
                DivTypes.Scroll,
                Div(
                    Text("Standard Router")
                        .FontSize(28)
                        .FontColor("#F8FAFC"),
                    Text("Routes are replaced immediately without a transition phase.")
                        .FontColor("#94A3B8")
                        .Margin(top: 6, bottom: 18),
                    Div(
                            DivTypes.Row,
                            NavigationButton("home", "Home", navigation, navigator),
                            NavigationButton("profile", "Profile", navigation, navigator),
                            NavigationButton("activity", "Activity", navigation, navigator))
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
                        Route("home", () => new HomePage(navigator)),
                        Route("profile", () => new ProfilePage(navigator)),
                        Route("activity", () => new ActivityPage(navigator))))
                    .Spacing(12))
            .Padding(24)
            .Background("#0B1120");
    }

    private static IElement NavigationButton(
        string route,
        string label,
        NavigationState navigation,
        Navigator navigator)
    {
        var active = string.Equals(route, navigation.CurrentRoute, StringComparison.OrdinalIgnoreCase);

        return Button(label, () => navigator.Navigate(route))
            .Key("nav-" + route)
            .Size(120, 38)
            .Background(active ? "#0F766E" : "#1E293B")
            .FontColor("#F8FAFC");
    }
}

internal sealed class HomePage : Component
{
    private readonly Navigator _navigator;

    public HomePage(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (count, setCount) = useState(0);

        return Div(
                Text("Home")
                    .FontSize(24)
                    .FontColor("#F8FAFC"),
                Text("Navigate pushes the current route onto the back stack.")
                    .FontColor("#94A3B8")
                    .Margin(top: 4, bottom: 18),
                Text($"Local counter: {count}")
                    .FontSize(20)
                    .FontColor("#FDE68A"),
                Div(
                        DivTypes.Row,
                        Button("Increment", () => setCount(current => current + 1))
                            .Size(110, 36),
                        Button("Open profile", () => _navigator.Navigate("profile"))
                            .Size(130, 36))
                    .Spacing(10)
                    .Margin(top: 14))
            .Padding(22)
            .Background("#111827")
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(14);
    }
}

internal sealed class ProfilePage : Component
{
    private readonly Navigator _navigator;

    public ProfilePage(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (name, setName) = useState("Nuri");

        return Div(
                Text("Profile")
                    .FontSize(24)
                    .FontColor("#F8FAFC"),
                Text("Each route owns independent hook state.")
                    .FontColor("#94A3B8")
                    .Margin(top: 4, bottom: 18),
                Text("Display name").FontColor("#CBD5E1"),
                TextBox(name, value => setName(_ => value))
                    .Width(360),
                Text($"Hello, {name}.")
                    .FontSize(18)
                    .FontColor("#F8FAFC"),
                Div(
                        DivTypes.Row,
                        Button("Navigate activity", () => _navigator.Navigate("activity"))
                            .Size(150, 36),
                        Button("Replace with home", () => _navigator.Replace("home"))
                            .Size(150, 36))
                    .Spacing(10))
            .Spacing(12)
            .Padding(22)
            .Background("#111827")
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(14);
    }
}

internal sealed class ActivityPage : Component
{
    private readonly Navigator _navigator;

    public ActivityPage(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (subscribed, setSubscribed) = useState(true);

        return Div(
                Text("Activity")
                    .FontSize(24)
                    .FontColor("#F8FAFC"),
                Text("GoBack restores the previous route from history.")
                    .FontColor("#94A3B8")
                    .Margin(top: 4, bottom: 18),
                CheckBox("Subscribe to activity updates", value => setSubscribed(_ => value))
                    .Checked(subscribed),
                Text($"Activity updates are {(subscribed ? "enabled" : "disabled")}.")
                    .FontColor("#F8FAFC"),
                Button("Go back", _navigator.GoBack)
                    .Size(110, 36))
            .Spacing(12)
            .Padding(22)
            .Background("#111827")
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(14);
    }
}
