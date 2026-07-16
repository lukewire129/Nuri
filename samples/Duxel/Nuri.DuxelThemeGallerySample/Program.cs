using Duxel.Core;
using Nuri.Duxel;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;

NuriApplication.Run(
    themeController => new ThemeGalleryComponent(
        themeController),
    title: "Nuri Duxel Theme Gallery",
    width: 1180,
    height: 760,
    theme: UiTheme.SolarizedDark);

internal sealed class ThemeGalleryComponent(
    DuxelThemeController themeController) : Component
{
    private static readonly ThemePreset[] ThemePresets =
    [
        new("Windows 11", UiCompiledDesign.Windows11.Theme),
        new("Windows 11 Dark", UiCompiledDesign.Windows11Dark.Theme),
        new("ImGui - Dark", UiTheme.ImGuiDark),
        new("ImGui - Light", UiTheme.ImGuiLight),
        new("ImGui - Classic", UiTheme.ImGuiClassic),
        new("Nord", UiTheme.Nord),
        new("Solarized Dark", UiTheme.SolarizedDark),
        new("Solarized Light", UiTheme.SolarizedLight),
        new("Dracula", UiTheme.Dracula),
        new("Monokai", UiTheme.Monokai),
        new("Catppuccin Mocha", UiTheme.CatppuccinMocha),
        new("GitHub Dark", UiTheme.GitHubDark)
    ];

    public override IElement Render()
    {
        var (activeTheme, setActiveTheme) = useState(new ActiveThemeState(
            "Solarized Dark",
            (themeController.CurrentTheme ?? UiTheme.SolarizedDark).WindowBg));
        var (name, setName) = useState("Nuri user");
        var (password, setPassword) = useState("theme-gallery");
        var (notifications, setNotifications) = useState(true);
        var (compactMode, setCompactMode) = useState(false);
        var (choice, setChoice) = useState("Balanced");
        var (clickCount, setClickCount) = useState(0);

        var themeButtons = ThemePresets
            .Select((preset, index) =>
                Button(preset.Name, () =>
                    {
                        if (themeController.RequestTheme(preset.Theme))
                        {
                            setActiveTheme (current => current with
                            {
                                WindowBg = preset.Theme.WindowBg,
                                Name = preset.Name,
                            });
                        }
                    })
                    .Key(preset.Name)
                    .Row(index / 4)
                    .Column(index % 4)
                    .Height(34))
            .Cast<IElement>()
            .ToArray();

        return Div(
                DivTypes.Scroll,
                Div(
                    Text("Nuri.Duxel Theme Gallery")
                        .FontSize(28),
                    Text($"Active palette: {activeTheme.Name}")
                        .FontSize(18),
                    Text("Choose a Duxel preset. Nuri hook state and the entered values stay intact while the palette changes."),
                    Text("Theme presets")
                        .FontSize(20),
                    Grid(themeButtons)
                        .Columns(Star, Star, Star, Star)
                        .Rows(Auto, Auto, Auto)
                        .ColumnSpacing(10)
                        .RowSpacing(10),
                    Text("Default controls")
                        .FontSize(20),
                    Grid(
                            Div(
                                    Text("Buttons")
                                        .FontSize(17),
                                    Div(
                                            DivTypes.Row,
                                            Button("Default", () => setClickCount(current => current + 1))
                                                .Size(110, 34),
                                            Button("Reset", () => setClickCount(_ => 0))
                                                .Size(90, 34),
                                            Button("Wide action", () => setClickCount(current => current + 5))
                                                .Size(150, 34))
                                        .Spacing(10),
                                    Text($"Button result: {clickCount}"))
                                .Spacing(9)
                                .Row(0)
                                .Column(0),
                            Div(
                                    Text("Text input")
                                        .FontSize(17),
                                    Text("Name"),
                                    TextBox(name, value => setName(_ => value))
                                        .Width(360),
                                    Text("Password-style input"),
                                    PasswordBox()
                                        .TextValue(password)
                                        .OnTextChanged(value => setPassword(_ => value))
                                        .Width(360),
                                    Text($"Hello, {name}. Stored characters: {password.Length}"))
                                .Spacing(8)
                                .Row(0)
                                .Column(1),
                            Div(
                                    Text("Selection controls")
                                        .FontSize(17),
                                    CheckBox("Enable notifications", value => setNotifications(_ => value))
                                        .Checked(notifications),
                                    ToggleButton("Compact mode", value => setCompactMode(_ => value))
                                        .Checked(compactMode),
                                    RadioButton("Balanced", value =>
                                        {
                                            if (value)
                                            {
                                                setChoice(_ => "Balanced");
                                            }
                                        })
                                        .Checked(choice == "Balanced"),
                                    RadioButton("Performance", value =>
                                        {
                                            if (value)
                                            {
                                                setChoice(_ => "Performance");
                                            }
                                        })
                                        .Checked(choice == "Performance"),
                                    Text($"Notifications: {notifications} | Compact: {compactMode} | Mode: {choice}"))
                                .Spacing(9)
                                .Row(1)
                                .Column(0),
                            Div(
                                    Text("Typography and alignment")
                                        .FontSize(17),
                                    Text("Default body text"),
                                    Text("Large heading")
                                        .FontSize(24),
                                    Text("Centered text")
                                        .HCenter(),
                                    Button("Centered button text", () => setClickCount(current => current + 1))
                                        .TextCenter()
                                        .Size(220, 38))
                                .Spacing(10)
                                .Row(1)
                                .Column(1))
                        .Columns(Star, Star)
                        .Rows(Auto, Auto)
                        .ColumnSpacing(28)
                        .RowSpacing(22),
                    Text("The gallery uses the controls currently materialized by Nuri.Duxel. Runtime theme changes update Duxel's color palette on the next frame.")
                ).Spacing(15)
            .Background(activeTheme.WindowBg)
            .Padding(22)
        );
    }

    private readonly record struct ActiveThemeState(string Name, UiColor WindowBg);
    private readonly record struct ThemePreset(string Name, UiTheme Theme);
}
