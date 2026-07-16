using Nuri.Runtime;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.StoreSample.Components;

public sealed class StoreSampleComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] App");

        return Grid(Rows(Auto, Star, Auto),
                new HeaderComponent().Row(0),
                Grid(
                        new SidebarComponent().Column(0),
                        new LargeListComponent().Column(1))
                    .Columns(Pixels(220), Star)
                    .Row(1),
                new FooterComponent().Row(2))
            .Padding(22)
            .Background("#f4f6f8");
    }

    internal static void ChangeUser()
    {
        var current = UserStore.State.Value;
        UserStore.State.Set(current with { Name = current.Name == "Guest" ? "Dana" : "Guest" });
    }

    internal static void ChangeRole()
    {
        var current = UserStore.State.Value;
        UserStore.State.Set(current with { Role = current.Role == "User" ? "Admin" : "User" });
    }

    internal static void IncrementLoginCount()
    {
        var current = UserStore.State.Value;
        UserStore.State.Set(current with { LoginCount = current.LoginCount + 1 });
    }
}

internal static class UserStore
{
    public static readonly Store<UserState> State = new Store<UserState>(new UserState("Guest", "User", 0));
}

internal sealed record UserState(string Name, string Role, int LoginCount);

internal sealed class HeaderComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] Header");

        return Grid(
                Text("Store partial rerender").FontSize(24).FontWeight(FontWeightValue.Bold).VCenter().Column(0),
                new UserBadgeComponent().Column(1),
                new AdminPanelComponent().Column(2),
                new LoginCounterComponent().Column(3),
                Button("Name", StoreSampleComponent.ChangeUser).Height(36).Column(4),
                Button("Role", StoreSampleComponent.ChangeRole).Height(36).Column(5),
                Button("Login", StoreSampleComponent.IncrementLoginCount).Height(36).Column(6))
            .Columns(Star, Pixels(130), Pixels(130), Pixels(130), Pixels(70), Pixels(70), Pixels(78))
            .Padding(16)
            .Background("#ffffff")
            .Brush("#d8dee8")
            .Thickness(1)
            .CornerRadius(8)
            .Margin(bottom: 14);
    }
}

internal sealed class UserBadgeComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] UserBadge");
        var userName = useStore(UserStore.State, state => state.Name);

        return Div(Text("Badge: " + userName).Center())
            .Height(36)
            .Background("#e7f5ff")
            .Brush("#74c0fc")
            .Thickness(1)
            .CornerRadius(8);
    }
}

internal sealed class AdminPanelComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] AdminPanel");
        var role = useStore(UserStore.State, state => state.Role);

        return Div(Text("Role: " + role).Center())
            .Height(36)
            .Background("#f3f0ff")
            .Brush("#b197fc")
            .Thickness(1)
            .CornerRadius(8);
    }
}

internal sealed class LoginCounterComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] LoginCounter");
        var loginCount = useStore(UserStore.State, state => state.LoginCount);

        return Div(Text("Logins: " + loginCount).Center())
            .Height(36)
            .Background("#ebfbee")
            .Brush("#8ce99a")
            .Thickness(1)
            .CornerRadius(8);
    }
}

internal sealed class SidebarComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] Sidebar");

        return Div(
                Text("Sidebar").FontSize(18).FontWeight(FontWeightValue.Bold).Margin(bottom: 12),
                Button("Static action").Height(34).Margin(bottom: 8),
                Button("Another action").Height(34))
            .Padding(16)
            .Background("#ffffff")
            .Brush("#d8dee8")
            .Thickness(1)
            .CornerRadius(8)
            .Margin(right: 14);
    }
}

internal sealed class LargeListComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] LargeList");

        var rows = Enumerable.Range(1, 40)
            .Select(index => (IElement)Div(Text("Row " + index.ToString("00")))
                .Padding(10)
                .Margin(bottom: 6)
                .Background(index % 2 == 0 ? "#ffffff" : "#f8fafc")
                .Brush("#e5e9f0")
                .Thickness(1)
                .CornerRadius(6))
            .ToArray();

        return Div(DivTypes.Scroll, Div(rows))
            .Padding(16)
            .Background("#ffffff")
            .Brush("#d8dee8")
            .Thickness(1)
            .CornerRadius(8);
    }
}

internal sealed class FooterComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] Footer");

        return Grid(
                Text("Footer").VCenter().Column(0),
                new UserStatusComponent().Column(1))
            .Columns(Star, Pixels(220))
            .Padding(16)
            .Background("#ffffff")
            .Brush("#d8dee8")
            .Thickness(1)
            .CornerRadius(8)
            .Margin(top: 14);
    }
}

internal sealed class UserStatusComponent : Component
{
    public override IElement Render()
    {
        Console.WriteLine("[Render] UserStatus");

        return Div(Text("Status: static").Center())
            .Height(34)
            .Background("#fff4e6")
            .Brush("#ffc078")
            .Thickness(1)
            .CornerRadius(8);
    }
}
