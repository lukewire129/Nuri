using Nuri.UI.Dsl;
using Nuri.UI.Events;
using Nuri.UI.Values;

namespace Nuri.CommandPaletteSample.Components;

public sealed class CommandPaletteComponent : Component
{
    private static readonly CommandItem[] Commands =
    {
        new("open-file", "Open File", "Ctrl+O"),
        new("save-file", "Save File", "Ctrl+S"),
        new("open-settings", "Open Settings", "Ctrl+,"),
        new("toggle-terminal", "Toggle Terminal", "Ctrl+`"),
        new("format-document", "Format Document", "Shift+Alt+F"),
        new("run-build", "Run Build", "Ctrl+Shift+B"),
        new("run-tests", "Run Tests", "Ctrl+T")
    };

    public override IElement Render()
    {
        var (state, setState) = useState(new PaletteState(string.Empty, 0, "Type to filter commands."));
        var stateRef = useRef(state);
        stateRef.Current = state;

        void UpdateState(Func<PaletteState, PaletteState> update)
        {
            var nextState = update(stateRef.Current);
            stateRef.Current = nextState;
            setState(nextState);
        }

        CommandItem[] Filter(string query)
        {
            return Commands
                .Where(command => string.IsNullOrWhiteSpace(query)
                    || command.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        void ChangeQuery(string query)
        {
            UpdateState(current => current with
            {
                Query = query,
                SelectedIndex = 0,
                Status = "Type to filter commands."
            });
        }

        void MoveSelection(int delta)
        {
            UpdateState(current =>
            {
                var count = Filter(current.Query).Length;
                return current with
                {
                    SelectedIndex = Math.Clamp(current.SelectedIndex + delta, 0, Math.Max(count - 1, 0))
                };
            });
        }

        void HandleKeyDown(KeyboardKey key)
        {
            if (key == KeyboardKey.Down)
            {
                MoveSelection(1);
                return;
            }

            if (key == KeyboardKey.Up)
            {
                MoveSelection(-1);
                return;
            }

            if (key == KeyboardKey.Enter)
            {
                var current = stateRef.Current;
                var commands = Filter(current.Query);
                if (commands.Length > 0)
                {
                    var selected = commands[Math.Clamp(current.SelectedIndex, 0, commands.Length - 1)];
                    UpdateState(value => value with { Status = $"Executed: {selected.Title}" });
                }

                return;
            }

            if (key == KeyboardKey.Escape)
            {
                UpdateState(current => current with
                {
                    Query = string.Empty,
                    SelectedIndex = 0,
                    Status = "Query cleared."
                });
            }
        }

        var filteredCommands = Filter(state.Query);
        var selectedIndex = Math.Clamp(state.SelectedIndex, 0, Math.Max(filteredCommands.Length - 1, 0));

        return Div(
                Text("Command Palette")
                    .FontSize(22)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor("#f8fafc"),
                TextBox(state.Query, ChangeQuery)
                    .Key("search")
                    .OnKeyDown(HandleKeyDown)
                    .AutoFocus()
                    .Height(48)
                    .Padding(14, 0, 14, 0)
                    .Margin(0, 16, 0, 12)
                    .Background("#111827")
                    .FontColor("#f8fafc")
                    .FontSize(18)
                    .TextStart()
                    .TextVCenter(),
                Div(CommandRows(filteredCommands, selectedIndex)),
                Text(state.Status)
                    .Key("status")
                    .FontColor("#93c5fd")
                    .FontSize(13)
                    .Margin(0, 14, 0, 0)
            )
            .Width(540)
            .Padding(24)
            .CornerRadius(18)
            .Background("#0f172a")
            .Brush("#334155")
            .Thickness(1)
            .Center();
    }

    private static IElement[] CommandRows(CommandItem[] commands, int selectedIndex)
    {
        if (commands.Length == 0)
            return new[] { Text("No commands found").Key("empty").FontColor("#94a3b8").Padding(12) };

        return commands
            .Select((command, index) => CommandRow(command, index == selectedIndex))
            .ToArray();
    }

    private static IElement CommandRow(CommandItem command, bool selected)
    {
        return Grid(
                Text(command.Title)
                    .FontWeight(FontWeightValue.Bold)
                    .FontColor(selected ? "#ffffff" : "#e2e8f0")
                    .Column(0),
                Text(command.Shortcut)
                    .FontColor(selected ? "#dbeafe" : "#94a3b8")
                    .End()
                    .Column(1)
            )
            .Key(command.Id)
            .Columns(Star, Pixels(120))
            .Padding(12)
            .Margin(0, 0, 0, 8)
            .CornerRadius(10)
            .Background(selected ? "#1d4ed8" : "#1e293b")
            .Transition(120, EasingValue.CubicOut);
    }
}

internal sealed record CommandItem(string Id, string Title, string Shortcut);

internal sealed record PaletteState(string Query, int SelectedIndex, string Status);
