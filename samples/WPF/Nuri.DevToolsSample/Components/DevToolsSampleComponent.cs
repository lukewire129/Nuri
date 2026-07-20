using Nuri.Runtime;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.DevToolsSample.Components;

public sealed class DevToolsSampleComponent : Component
{
    public override IElement Render()
    {
        var (expanded, setExpanded) = useState(true);

        return Grid(Rows(Auto, Star, Auto),
                Div(
                        Text("WPF Runtime DevTools Sample").FontSize(24).FontWeight(FontWeightValue.Bold),
                        Text("This WPF application is the inspected target. Press F12 to open Nuri DevTools.")
                            .FontColor("#475569")
                            .Margin(top: 6))
                    .Row(0),
                Grid(
                        Div(
                                Button(expanded ? "Collapse detail" : "Expand detail", () => setExpanded(current => !current)).Height(36).Margin(bottom: 10),
                                Button("Change name", SampleActions.ChangeName).Height(36).Margin(bottom: 10),
                                Button("Change role", SampleActions.ChangeRole).Height(36).Margin(bottom: 10),
                                Button("Increment login", SampleActions.IncrementLoginCount).Height(36))
                            .Padding(16)
                            .Background("#ffffff")
                            .Brush("#d8dee8")
                            .Thickness(1)
                            .CornerRadius(8)
                            .Column(0),
                        Div(
                                new NameBadgeComponent(),
                                new RolePanelComponent(),
                                new LoginCounterComponent(),
                                expanded ? new DetailComponent().Key("detail") : Text("Detail is unmounted").FontColor("#64748b"))
                            .Padding(16)
                            .Background("#ffffff")
                            .Brush("#d8dee8")
                            .Thickness(1)
                            .CornerRadius(8)
                            .Column(1))
                    .Columns(Pixels(220), Star)
                    .Margin(top: 16)
                    .Row(1),
                Text("Select a component in DevTools to inspect hooks, stores, render counts, and highlight its WPF element.")
                    .FontColor("#64748b")
                    .Margin(top: 14)
                    .Row(2))
            .Padding(24)
            .Background("#f4f6f8");
    }
}

internal static class SampleStore
{
    public static readonly Store<UserState> User = new Store<UserState>(new UserState("Guest", "User", 0));
}

internal static class SampleActions
{
    public static void ChangeName()
    {
        var current = SampleStore.User.Value;
        SampleStore.User.Set(current with { Name = current.Name == "Guest" ? "Dana" : "Guest" });
        Console.WriteLine("Name changed.");
    }

    public static void ChangeRole()
    {
        var current = SampleStore.User.Value;
        SampleStore.User.Set(current with { Role = current.Role == "User" ? "Admin" : "User" });
        Console.WriteLine("Role changed.");
    }

    public static void IncrementLoginCount()
    {
        var current = SampleStore.User.Value;
        SampleStore.User.Set(current with { LoginCount = current.LoginCount + 1 });
        Console.WriteLine("Login count incremented.");
    }
}

internal sealed record UserState(string Name, string Role, int LoginCount);

internal sealed class NameBadgeComponent : Component
{
    public override IElement Render()
    {
        var name = useStore(SampleStore.User, state => state.Name);
        return Div(Text("Name: " + name).Center())
            .Height(42)
            .Background("#e7f5ff")
            .Brush("#74c0fc")
            .Thickness(1)
            .CornerRadius(8)
            .Margin(bottom: 10);
    }
}

internal sealed class RolePanelComponent : Component
{
    public override IElement Render()
    {
        var role = useStore(SampleStore.User, state => state.Role);
        return Div(Text("Role: " + role).Center())
            .Height(42)
            .Background("#f3f0ff")
            .Brush("#b197fc")
            .Thickness(1)
            .CornerRadius(8)
            .Margin(bottom: 10);
    }
}

internal sealed class LoginCounterComponent : Component
{
    public override IElement Render()
    {
        var loginCount = useStore(SampleStore.User, state => state.LoginCount);
        return Div(Text("Login count: " + loginCount).Center())
            .Height(42)
            .Background("#ebfbee")
            .Brush("#8ce99a")
            .Thickness(1)
            .CornerRadius(8)
            .Margin(bottom: 10);
    }
}

internal sealed class DetailComponent : Component
{
    public override IElement Render()
    {
        useEffect(() =>
        {
            return () => { };
        }, []);

        var memo = useMemo(() => DateTime.Now.ToString("HH:mm:ss"), []);
        return Div(
                Text("Detail component").FontWeight(FontWeightValue.Bold),
                Text("Memo created at: " + memo).FontColor("#64748b").Margin(top: 6))
            .Padding(14)
            .Background("#f8fafc")
            .Brush("#e2e8f0")
            .Thickness(1)
            .CornerRadius(8);
    }
}
