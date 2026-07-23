using Nuri.SimplyShare.Features.Devices;
using Nuri.SimplyShare.Features.Transfers;
using Nuri.SimplyShare.Shared;

namespace Nuri.SimplyShare.App;

public sealed class AppShell : Component
{
    private static readonly TimeSpan RouteTransitionDuration = TimeSpan.FromMilliseconds(220);
    public override IElement Render()
    {
        var (navigation, navigator) = useNavigation("devices");
        var (displayedRoute, setDisplayedRoute) = useState(navigation.CurrentRoute);
        var (routeVisible, setRouteVisible) = useState(true);
        var requestedRoute = navigation.CurrentRoute;

        useEffect(() =>
        {
            if (string.Equals(displayedRoute, requestedRoute, StringComparison.OrdinalIgnoreCase))
            {
                setRouteVisible(_ => true);
                return null;
            }

            var cancellation = new CancellationTokenSource();
            setRouteVisible(_ => false);
            _ = ReplaceRouteAfterExitAsync(requestedRoute, setDisplayedRoute, cancellation.Token);
            return () =>
            {
                cancellation.Cancel();
                cancellation.Dispose();
            };
        }, [requestedRoute, displayedRoute]);

        return Grid(
                new AppSidebar(navigation, navigator).Column(0),
                Grid(
                        new AppHeader().Row(0),
                        Div(
                                Router(
                                    displayedRoute,
                                    () => new EmptyPage("Page not found"),
                                    Route("devices", () => new DevicesPage()),
                                    Route("activity", () => new TransfersPage()),
                                    Route("about", () => new AboutPage())))
                            .Opacity(routeVisible ? 1 : 0)
                            .Translate(routeVisible ? 0 : 12, 0)
                            .Transition(RouteTransitionDuration, EasingValue.CubicOut)
                            .Row(1))
                    .Rows(Auto, Star)
                    .Column(1))
            .Columns(Pixels(220), Star)
            .Background(Palette.Canvas);
    }

    private static async Task ReplaceRouteAfterExitAsync(
        string route,
        Action<Func<string, string>> setDisplayedRoute,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(RouteTransitionDuration, cancellationToken).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
                setDisplayedRoute(_ => route);
        }
        catch (OperationCanceledException)
        {
        }
    }
}

internal sealed class AppSidebar : Component
{
    private readonly NavigationState _navigation;
    private readonly Navigator _navigator;

    public AppSidebar(NavigationState navigation, Navigator navigator)
    {
        _navigation = navigation;
        _navigator = navigator;
    }

    public override IElement Render()
    {
        return Div(
                Div(
                        Text("S").FontSize(24).FontWeight(FontWeightValue.Bold).FontColor(Palette.White).Center())
                    .Size(44, 44)
                    .Background(Palette.Accent)
                    .CornerRadius(14)
                    .Margin(bottom: 12),
                Text("SimplyShare").FontSize(20).FontWeight(FontWeightValue.Bold).FontColor(Palette.White),
                Text("Private LAN workspace").FontSize(11).FontColor(Palette.SidebarMuted).Margin(top: 4, bottom: 30),
                new NavigationButton("devices", "Devices", _navigation.CurrentRoute, _navigator),
                new NavigationButton("activity", "Activity", _navigation.CurrentRoute, _navigator),
                new NavigationButton("about", "About", _navigation.CurrentRoute, _navigator),
                Div().Grow(),
                Text("UDP 52525 / TCP 52526").FontSize(10).FontColor(Palette.SidebarMuted),
                Text("ECDH + AES-256-GCM").FontSize(10).FontColor(Palette.SidebarMuted).Margin(top: 5))
            .Padding(24)
            .Background(Palette.Sidebar);
    }
}

internal sealed class NavigationButton : Component
{
    private readonly string _route;
    private readonly string _label;
    private readonly string _selected;
    private readonly Navigator _navigator;

    public NavigationButton(string route, string label, string selected, Navigator navigator)
    {
        _route = route;
        _label = label;
        _selected = selected;
        _navigator = navigator;
    }

    public override IElement Render()
    {
        var active = string.Equals(_route, _selected, StringComparison.OrdinalIgnoreCase);
        var (hovered, setHovered) = useState(false);
        return Button(_label, () => _navigator.Navigate(_route))
            .Key(_route)
            .Height(42)
            .Padding(14, 0, 14, 0)
            .Margin(left: hovered && !active ? 5 : 0, bottom: 8)
            .TextStart()
            .Background(active ? Palette.Accent : hovered ? "#213B48" : Palette.Sidebar)
            .FontColor(active ? Palette.White : Palette.SidebarText)
            .Brush(active ? Palette.Accent : hovered ? "#2B4855" : Palette.Sidebar)
            .Thickness(1)
            .OnHover(value => setHovered(_ => value))
            .Transition(TimeSpan.FromMilliseconds(150), EasingValue.CubicOut);
    }
}

internal sealed class AppHeader : Component
{
    public override IElement Render()
    {
        return Grid(
                Div(
                        Text("Nearby sharing").FontSize(13).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink),
                        new ConnectionStatus())
                    .Column(0),
                Button("Settings", AppServices.Host.OpenSettings)
                    .Width(104)
                    .Height(36)
                    .Background(Palette.White)
                    .Brush(Palette.Border)
                    .Thickness(1)
                    .Column(1))
            .Columns(Star, Auto)
            .Padding(24, 14, 24, 14)
            .Background(Palette.White);
    }
}

internal sealed class ConnectionStatus : Component
{
    public override IElement Render()
    {
        var status = useStore(AppState.Status);
        return Text(status).FontSize(11).FontColor(Palette.Muted).Margin(top: 3);
    }
}

internal sealed class AboutPage : Component
{
    public override IElement Render()
    {
        return new PageLayout(
            "About",
            "A Nuri.WPF vertical-slice port of SimplyShare.",
            new SurfaceCard(
                Text("Serverless by design").FontSize(19).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink),
                Text("Discovery uses UDP broadcast. Text and files travel directly over TCP after an ephemeral ECDH key exchange.")
                    .FontColor(Palette.Muted)
                    .Margin(top: 10),
                Text("The shell owns only Router state. Device, activity, chat, and settings state rerender in their own component subtrees.")
                    .FontColor(Palette.Muted)
                    .Margin(top: 10)));
    }
}

internal sealed class EmptyPage : Component
{
    private readonly string _message;

    public EmptyPage(string message) => _message = message;

    public override IElement Render() => Text(_message).Center().FontColor(Palette.Muted);
}
