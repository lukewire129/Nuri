using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Navigation;
using Nuri.UI.Values;

namespace Nuri.RouterTransitionSample.Components;

public sealed class RouterTransitionComponent : Component
{
    private static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(260);

    public override IElement Render()
    {
        var (navigation, navigator) = useNavigation("home");
        var (displayedRoute, setDisplayedRoute) = useState(navigation.CurrentRoute);
        var (opacity, setOpacity) = useState(1.0);
        var requestedRoute = navigation.CurrentRoute;

        useEffect(() =>
        {
            if (RouteEquals(displayedRoute, requestedRoute))
            {
                setOpacity(_ => 1.0);
                return null;
            }

            var cancellation = new CancellationTokenSource();
            setOpacity(_ => 0.0);
            _ = ReplaceAfterExitAsync(
                requestedRoute,
                setDisplayedRoute,
                cancellation.Token);

            return () =>
            {
                cancellation.Cancel();
                cancellation.Dispose();
            };
        }, [requestedRoute, displayedRoute]);

        return Div(
                DivTypes.Scroll,
                Div(
                    Text("Router transition with hooks")
                        .FontSize(30)
                        .FontWeight(FontWeightValue.Bold)
                        .FontColor("#F8FAFC"),
                    Text("The sample owns its animation policy with useState and useEffect.")
                        .FontColor("#94A3B8")
                        .Margin(top: 6, bottom: 20),
                    Div(
                            DivTypes.Row,
                            NavigationButton("home", "Home", navigation, navigator),
                            NavigationButton("profile", "Profile", navigation, navigator),
                            NavigationButton("settings", "Settings", navigation, navigator))
                        .Spacing(10),
                    Div(
                            DivTypes.Row,
                            Button("Go back", navigator.GoBack)
                                .Size(100, 36)
                                .Background(navigator.CanGoBack ? "#334155" : "#1E293B")
                                .FontColor(navigator.CanGoBack ? "#F8FAFC" : "#64748B"),
                            Text($"Requested: {requestedRoute}  |  Displayed: {displayedRoute}  |  History: {navigation.BackStack.Count}")
                                .FontColor("#CBD5E1"))
                        .Spacing(14)
                        .Margin(top: 14, bottom: 20),
                    Div(
                            Router(
                                displayedRoute,
                                Route("home", () => new HomeRoute(navigator)),
                                Route("profile", () => new ProfileRoute(navigator)),
                                Route("settings", () => new SettingsRoute(navigator))))
                        .Opacity(opacity)
                        .Transition(TransitionDuration, EasingValue.CubicOut),
                    Text("Select routes quickly: effect cleanup cancels the pending replacement and keeps the latest request.")
                        .FontColor("#94A3B8")
                        .Margin(top: 18))
                    .Spacing(12))
            .Padding(28)
            .Background("#0B1120");
    }

    private static async Task ReplaceAfterExitAsync(
        string route,
        Action<Func<string, string>> setDisplayedRoute,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TransitionDuration, cancellationToken).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
                setDisplayedRoute(_ => route);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static IElement NavigationButton(
        string route,
        string label,
        NavigationState navigation,
        Navigator navigator)
    {
        var isActive = RouteEquals(route, navigation.CurrentRoute);

        return Button(label, () => navigator.Navigate(route))
            .Key("nav-" + route)
            .Size(120, 40)
            .Background(isActive ? "#2563EB" : "#1E293B")
            .FontColor("#F8FAFC");
    }

    private static bool RouteEquals(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class HomeRoute : Component
{
    private readonly Navigator _navigator;

    public HomeRoute(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (count, setCount) = useState(0);

        return RouteLayout.Surface(
            "Home",
            "The current keyed route stays mounted until the exit animation finishes.",
            Text($"Route-local counter: {count}")
                .FontSize(20)
                .FontColor("#FDE68A"),
            Div(
                    DivTypes.Row,
                    Button("Increment", () => setCount(current => current + 1))
                        .Size(110, 36),
                    Button("Open profile", () => _navigator.Navigate("profile"))
                        .Size(130, 36))
                .Spacing(10),
            Text("Returning after replacement mounts a fresh keyed route subtree.")
                .FontColor("#94A3B8"));
    }
}

internal sealed class ProfileRoute : Component
{
    private readonly Navigator _navigator;

    public ProfileRoute(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (name, setName) = useState("Nuri");

        return RouteLayout.Surface(
            "Profile",
            "Route content can keep using ordinary hooks while the parent controls transitions.",
            Text("Display name").FontColor("#CBD5E1"),
            TextBox(name, value => setName(_ => value))
                .Width(360),
            Text($"Hello, {name}.")
                .FontSize(18)
                .FontColor("#F8FAFC"),
            Div(
                    DivTypes.Row,
                    Button("Open settings", () => _navigator.Navigate("settings"))
                        .Size(130, 36),
                    Button("Replace with home", () => _navigator.Replace("home"))
                        .Size(150, 36))
                .Spacing(10));
    }
}

internal sealed class SettingsRoute : Component
{
    private readonly Navigator _navigator;

    public SettingsRoute(Navigator navigator)
    {
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var (notifications, setNotifications) = useState(true);

        return RouteLayout.Surface(
            "Settings",
            "Navigation history remains independent from the animation state.",
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
                .Spacing(10));
    }
}

internal static class RouteLayout
{
    public static IElement Surface(
        string title,
        string description,
        params IElement[] children)
    {
        return Component.Div(
                Component.Text(title)
                    .FontSize(24)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#F8FAFC"),
                Component.Text(description)
                    .FontColor("#94A3B8")
                    .Margin(top: 4, bottom: 18),
                Component.Div(children).Spacing(14))
            .Padding(22)
            .Background("#111827")
            .Brush("#334155")
            .Thickness(1)
            .CornerRadius(14);
    }
}
