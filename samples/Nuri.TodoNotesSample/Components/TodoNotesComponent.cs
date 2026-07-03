using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.TodoNotesSample.Components;

public sealed class TodoNotesComponent : Component
{
    private static readonly TodoItem[] SeedItems =
    {
        new("note-ship-core", "Stabilize keyed list behavior before adding new hooks.", false, false),
        new("note-command-palette", "Extract keyboard and focus friction from command palette into concrete Core questions.", false, true),
        new("note-animation", "Keep animation semantics neutral and renderer-owned.", true, false),
        new("note-samples", "Build visible samples first, then promote repeated pain points into Core.", false, true)
    };

    public override IElement Render()
    {
        var (state, setState) = useState(new TodoState(string.Empty, FilterMode.All, SeedItems, null, string.Empty));
        var stateRef = useLatest(state);

        void UpdateState(Func<TodoState, TodoState> update)
        {
            var nextState = update(stateRef.Current);
            stateRef.Current = nextState;
            setState(nextState);
        }

        void SetDraft(string draft)
        {
            UpdateState(current => current with { Draft = draft });
        }

        void AddItem()
        {
            var text = stateRef.Current.Draft.Trim();
            if (text.Length == 0)
            {
                UpdateState(current => current with { Status = "Type a note before adding it." });
                return;
            }

            var item = new TodoItem($"note-{Guid.NewGuid():N}", text, false, false);
            UpdateState(current => current with
            {
                Draft = string.Empty,
                Items = current.Items.Prepend(item).ToArray(),
                EditingId = null,
                Status = $"Added: {Shorten(text)}"
            });
        }

        void SetFilter(FilterMode filter)
        {
            UpdateState(current => current with { Filter = filter, Status = $"Viewing {FilterLabel(filter).ToLowerInvariant()} notes." });
        }

        void ToggleDone(string id)
        {
            UpdateItem(id, item => item with { IsDone = !item.IsDone }, item => item.IsDone ? "Marked active." : "Completed.");
        }

        void TogglePinned(string id)
        {
            UpdateItem(id, item => item with { IsPinned = !item.IsPinned }, item => item.IsPinned ? "Unpinned note." : "Pinned note.");
        }

        void StartEditing(string id)
        {
            var item = stateRef.Current.Items.FirstOrDefault(candidate => candidate.Id == id);
            if (item is null)
                return;

            UpdateState(current => current with
            {
                EditingId = id,
                Status = $"Editing: {Shorten(item.Text)}"
            });
        }

        void ChangeItemText(string id, string text)
        {
            UpdateState(current => current with
            {
                Items = current.Items
                    .Select(item => item.Id == id ? item with { Text = text } : item)
                    .ToArray()
            });
        }

        void SaveEditing(string id)
        {
            var item = stateRef.Current.Items.FirstOrDefault(candidate => candidate.Id == id);
            if (item is null)
                return;

            var text = item.Text.Trim();
            if (text.Length == 0)
            {
                DeleteItem(id);
                return;
            }

            UpdateState(current => current with
            {
                EditingId = null,
                Items = current.Items
                    .Select(candidate => candidate.Id == id ? candidate with { Text = text } : candidate)
                    .ToArray(),
                Status = $"Saved: {Shorten(text)}"
            });
        }

        void CancelEditing()
        {
            UpdateState(current => current with { EditingId = null, Status = "Edit cancelled." });
        }

        void DeleteItem(string id)
        {
            var item = stateRef.Current.Items.FirstOrDefault(candidate => candidate.Id == id);
            if (item is null)
                return;

            UpdateState(current => current with
            {
                Items = current.Items.Where(candidate => candidate.Id != id).ToArray(),
                EditingId = current.EditingId == id ? null : current.EditingId,
                Status = $"Deleted: {Shorten(item.Text)}"
            });
        }

        void ClearCompleted()
        {
            var completedCount = stateRef.Current.Items.Count(item => item.IsDone);
            if (completedCount == 0)
            {
                UpdateState(current => current with { Status = "No completed notes to clear." });
                return;
            }

            UpdateState(current => current with
            {
                Items = current.Items.Where(item => !item.IsDone).ToArray(),
                EditingId = current.EditingId is { } editingId && current.Items.Any(item => item.Id == editingId && item.IsDone) ? null : current.EditingId,
                Status = $"Cleared {completedCount} completed {(completedCount == 1 ? "note" : "notes")}."
            });
        }

        void UpdateItem(string id, Func<TodoItem, TodoItem> update, Func<TodoItem, string> status)
        {
            UpdateState(current =>
            {
                TodoItem? changed = null;
                var items = current.Items
                    .Select(item =>
                    {
                        if (item.Id != id)
                            return item;

                        changed = update(item);
                        return changed;
                    })
                    .ToArray();

                return changed is null
                    ? current
                    : current with { Items = items, Status = status(changed) };
            });
        }

        var visibleItems = useMemo(() => FilterAndOrder(state.Items, state.Filter), state.Items, state.Filter);
        var activeCount = state.Items.Count(item => !item.IsDone);
        var completedCount = state.Items.Length - activeCount;

        return Div(
                Header(activeCount, completedCount),
                Composer(state.Draft, AddItem, SetDraft),
                Grid(
                        OverviewPanel(state, activeCount, completedCount).Column(0),
                        NotesPanel(visibleItems, state.EditingId, state.Filter, completedCount, SetFilter, StartEditing, ChangeItemText, SaveEditing, CancelEditing, ToggleDone, TogglePinned, DeleteItem, ClearCompleted)
                            .Column(1)
                    )
                    .Columns(Pixels(260), Star)
                    .Margin(top:24)
            )
            .Padding(32)
            .Background("#0b1120");
    }

    private static IElement Header(int activeCount, int completedCount)
    {
        return Grid(
                Div(
                        Text("Todo Notes")
                            .FontSize(30)
                            .FontWeight(FontWeightValue.Bold)
                            .FontColor("#f8fafc"),
                        Text("A user-facing sample that pressures local state, keyed lists, inline editing, and focus behavior.")
                            .FontSize(14)
                            .FontColor("#94a3b8")
                            .Margin(top:8)
                    )
                    .Column(0),
                Text($"{activeCount} active / {completedCount} completed")
                    .FontSize(13)
                    .FontColor("#cbd5e1")
                    .End()
                    .VCenter()
                    .Column(1)
            )
            .Columns(Star, Pixels(240));
    }

    private static IElement Composer(string draft, Action addItem, Action<string> setDraft)
    {
        return Grid(
                TextBox(draft, setDraft)
                    .Key("new-note")
                    .AutoFocus()
                    .Height(46)
                    .Padding(14, 0, 14, 0)
                    .Background("#111827")
                    .FontColor("#f8fafc")
                    .Brush("#334155")
                    .Thickness(1)
                    .TextStart()
                    .TextVCenter()
                    .Column(0),
                Button("Add Note", addItem)
                    .Height(46)
                    .Background("#2563eb")
                    .FontColor("#ffffff")
                    .Brush("#1d4ed8")
                    .Thickness(1)
                    .Column(1)
            )
            .Columns(Star, Pixels(136))
            .Margin(0, 24, 0, 0);
    }

    private static IElement OverviewPanel(TodoState state, int activeCount, int completedCount)
    {
        var pinnedCount = state.Items.Count(item => item.IsPinned);
        return Div(
                SummaryCard("Working Set", activeCount.ToString(), "Active notes still on deck."),
                SummaryCard("Pinned", pinnedCount.ToString(), "Pinned notes float to the top to stress keyed reorder."),
                SummaryCard("Completed", completedCount.ToString(), "Completed notes stay visible until cleared."),
                Div(
                        Text("Latest status")
                            .FontColor("#94a3b8")
                            .FontSize(12),
                        Text(state.Status)
                            .FontColor("#e2e8f0")
                            .FontSize(13)
                            .Margin(0, 8, 0, 0)
                    )
                    .Padding(16)
                    .Background("#111827")
                    .Brush("#1f2937")
                    .Thickness(1)
                    .CornerRadius(16)
            )
            .Margin(0, 0, 24, 0);
    }

    private static IElement SummaryCard(string label, string value, string description)
    {
        return Div(
                Text(label)
                    .FontColor("#94a3b8")
                    .FontSize(12),
                Text(value)
                    .FontColor("#f8fafc")
                    .FontSize(28)
                    .FontWeight(FontWeightValue.Bold)
                    .Margin(0, 6, 0, 6),
                Text(description)
                    .FontColor("#cbd5e1")
                    .FontSize(12)
            )
            .Padding(16)
            .Margin(0, 0, 0, 16)
            .Background("#111827")
            .Brush("#1f2937")
            .Thickness(1)
            .CornerRadius(16);
    }

    private static IElement NotesPanel(
        TodoItem[] visibleItems,
        string? editingId,
        FilterMode filter,
        int completedCount,
        Action<FilterMode> setFilter,
        Action<string> startEditing,
        Action<string, string> changeItemText,
        Action<string> saveEditing,
        Action cancelEditing,
        Action<string> toggleDone,
        Action<string> togglePinned,
        Action<string> deleteItem,
        Action clearCompleted)
    {
        return Div(
                Grid(
                        Text("Notes")
                            .FontColor("#f8fafc")
                            .FontSize(20)
                            .FontWeight(FontWeightValue.Bold)
                            .Column(0),
                        FilterBar(filter, setFilter)
                            .End()
                            .Column(1)
                    )
                    .Columns(Star, Pixels(296)),
                visibleItems.Length == 0
                    ? EmptyState(filter)
                    : Div(visibleItems.Select(item => NoteRow(item, editingId == item.Id, startEditing, changeItemText, saveEditing, cancelEditing, toggleDone, togglePinned, deleteItem)).ToArray())
                        .Margin(0, 18, 0, 0),
                Grid(
                        Text("Pinning and completion reorder the same keyed items instead of rebuilding the whole list.")
                            .FontColor("#94a3b8")
                            .FontSize(12)
                            .Column(0),
                        Button($"Clear Completed ({completedCount})", clearCompleted)
                            .Background("#0f172a")
                            .FontColor("#cbd5e1")
                            .Brush("#334155")
                            .Thickness(1)
                            .Column(1)
                    )
                    .Columns(Star, Pixels(180))
                    .Margin(0, 18, 0, 0)
            )
            .Padding(20)
            .Background("#111827")
            .Brush("#1f2937")
            .Thickness(1)
            .CornerRadius(18);
    }

    private static IElement FilterBar(FilterMode filter, Action<FilterMode> setFilter)
    {
        return Grid(
                FilterButton("All", filter == FilterMode.All, () => setFilter(FilterMode.All)).Column(0),
                FilterButton("Active", filter == FilterMode.Active, () => setFilter(FilterMode.Active)).Column(1),
                FilterButton("Completed", filter == FilterMode.Completed, () => setFilter(FilterMode.Completed)).Column(2)
            )
            .Columns(Stars(1), Stars(1), Stars(1));
    }

    private static IElement FilterButton(string label, bool selected, Action onClick)
    {
        return Button(label, onClick)
            .Margin(0, 0, 8, 0)
            .Height(38)
            .Background(selected ? "#1d4ed8" : "#0f172a")
            .FontColor(selected ? "#ffffff" : "#cbd5e1")
            .Brush(selected ? "#2563eb" : "#334155")
            .Thickness(1)
            .Transition(120, EasingValue.CubicOut);
    }

    private static IElement EmptyState(FilterMode filter)
    {
        return Div(
                Text("No notes in this view yet.")
                    .FontColor("#f8fafc")
                    .FontSize(18)
                    .FontWeight(FontWeightValue.Bold),
                Text($"Switch filters or add a note to populate the {FilterLabel(filter).ToLowerInvariant()} lane.")
                    .FontColor("#94a3b8")
                    .FontSize(13)
                    .Margin(0, 8, 0, 0)
            )
            .Padding(20)
            .Margin(0, 18, 0, 0)
            .Background("#0f172a")
            .Brush("#1e293b")
            .Thickness(1)
            .CornerRadius(14);
    }

    private static IElement NoteRow(
        TodoItem item,
        bool isEditing,
        Action<string> startEditing,
        Action<string, string> changeItemText,
        Action<string> saveEditing,
        Action cancelEditing,
        Action<string> toggleDone,
        Action<string> togglePinned,
        Action<string> deleteItem)
    {
        return Grid(
                Div(
                        Text(item.IsPinned ? "PINNED" : item.IsDone ? "DONE" : "ACTIVE")
                            .FontColor(item.IsPinned ? "#fde68a" : item.IsDone ? "#86efac" : "#93c5fd")
                            .FontSize(11)
                            .FontWeight(FontWeightValue.Bold),
                        isEditing
                            ? TextBox(item.Text, text => changeItemText(item.Id, text))
                                .Key($"edit-{item.Id}")
                                .AutoFocus()
                                .Height(40)
                                .Padding(12, 0, 12, 0)
                                .Background("#0f172a")
                                .FontColor("#f8fafc")
                                .Brush("#334155")
                                .Thickness(1)
                                .TextStart()
                                .TextVCenter()
                                .Margin(0, 10, 0, 0)
                            : Text(item.Text)
                                .FontColor(item.IsDone ? "#94a3b8" : "#f8fafc")
                                .FontSize(15)
                                .Margin(0, 10, 0, 0),
                        Text(item.IsDone ? "Completed notes remain visible until you clear them." : "Double-step flow: edit inline, then save the keyed row in place.")
                            .FontColor("#64748b")
                            .FontSize(12)
                            .Margin(0, 8, 0, 0)
                    )
                    .Column(0),
                Grid(
                        ActionButton(item.IsDone ? "Reopen" : "Done", item.IsDone ? "#14532d" : "#0f172a", toggleDone, item.Id).Column(0),
                        ActionButton(item.IsPinned ? "Unpin" : "Pin", item.IsPinned ? "#713f12" : "#0f172a", togglePinned, item.Id).Column(1),
                        isEditing
                            ? ActionButton("Save", "#1d4ed8", saveEditing, item.Id).Column(2)
                            : ActionButton("Edit", "#0f172a", startEditing, item.Id).Column(2),
                        isEditing
                            ? Button("Cancel", cancelEditing)
                                .Background("#0f172a")
                                .FontColor("#cbd5e1")
                                .Brush("#334155")
                                .Thickness(1)
                                .Column(3)
                            : ActionButton("Delete", "#3f1d1d", deleteItem, item.Id).Column(3)
                    )
                    .Columns(Pixels(88), Pixels(88), Pixels(88), Pixels(88))
                    .End()
                    .VCenter()
                    .Column(1)
            )
            .Key(item.Id)
            .Columns(Star, Pixels(376))
            .Padding(16)
            .Margin(0, 0, 0, 12)
            .Background(item.IsPinned ? "#1f2937" : item.IsDone ? "#0f172a" : "#172033")
            .Brush(item.IsPinned ? "#f59e0b" : item.IsDone ? "#1e293b" : "#334155")
            .Thickness(1)
            .CornerRadius(16)
            .Transition(120, EasingValue.CubicOut);
    }

    private static IElement ActionButton(string label, string background, Action<string> handler, string id)
    {
        return Button(label, () => handler(id))
            .Height(38)
            .Margin(0, 0, 8, 0)
            .Background(background)
            .FontColor("#e2e8f0")
            .Brush("#334155")
            .Thickness(1);
    }

    private static TodoItem[] FilterAndOrder(TodoItem[] items, FilterMode filter)
    {
        return items
            .Where(item => filter switch
            {
                FilterMode.Active => !item.IsDone,
                FilterMode.Completed => item.IsDone,
                _ => true
            })
            .OrderByDescending(item => item.IsPinned)
            .ThenBy(item => item.IsDone)
            .ToArray();
    }

    private static string FilterLabel(FilterMode filter)
    {
        return filter switch
        {
            FilterMode.Active => "Active",
            FilterMode.Completed => "Completed",
            _ => "All"
        };
    }

    private static string Shorten(string text)
    {
        return text.Length <= 36 ? text : $"{text[..33]}...";
    }
}

internal enum FilterMode
{
    All,
    Active,
    Completed
}

internal sealed record TodoItem(string Id, string Text, bool IsDone, bool IsPinned);

internal sealed record TodoState(string Draft, FilterMode Filter, TodoItem[] Items, string? EditingId, string Status);
