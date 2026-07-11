using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.TodoValidationSample.Components;

public sealed class TodoValidationComponent : Component
{
    private static readonly TodoItem[] InitialItems =
    {
        new("todo-controlled-input", "입력값 상태 동기화 확인", false),
        new("todo-list-diff", "키 기반 리스트 이동 확인", false),
        new("todo-filter", "필터 전환 확인", true),
        new("todo-item-edit", "행 안에서 바로 수정", false),
        new("todo-remove", "중간 항목 삭제", false)
    };

    public override IElement Render()
    {
        var (state, setState) = useState(new TodoState(string.Empty, TodoFilter.All, InitialItems, null, 1));
        var stateRef = useLatest(state);

        void Update(Func<TodoState, TodoState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        void AddItem()
        {
            var text = stateRef.Current.Draft.Trim();
            if (text.Length == 0)
                return;

            Update(current => current with
            {
                Draft = string.Empty,
                Items = current.Items.Append(new TodoItem($"todo-new-{current.NextId}", text, false)).ToArray(),
                NextId = current.NextId + 1
            });
        }

        void ToggleDone(string id)
        {
            UpdateItem(id, item => item with { IsDone = !item.IsDone });
        }

        void MoveFirstToEnd()
        {
            Update(current => current.Items.Length < 2
                ? current
                : current with { Items = current.Items.Skip(1).Append(current.Items[0]).ToArray() });
        }

        void StartEdit(string id)
        {
            Update(current => current with { EditingId = id });
        }

        void ChangeItemText(string id, string text)
        {
            UpdateItem(id, item => item with { Text = text });
        }

        void SaveEdit(string id)
        {
            Update(current => current with
            {
                EditingId = current.EditingId == id ? null : current.EditingId,
                Items = current.Items
                    .Select(item => item.Id == id ? item with { Text = item.Text.Trim() } : item)
                    .Where(item => item.Text.Length > 0)
                    .ToArray()
            });
        }

        void RemoveItem(string id)
        {
            Update(current => current with
            {
                Items = current.Items.Where(item => item.Id != id).ToArray(),
                EditingId = current.EditingId == id ? null : current.EditingId
            });
        }

        void UpdateItem(string id, Func<TodoItem, TodoItem> change)
        {
            Update(current => current with { Items = current.Items.Select(item => item.Id == id ? change(item) : item).ToArray() });
        }

        var visibleItems = useMemo(() => ApplyFilter(state.Items, state.Filter), state.Items, state.Filter);
        var listChildren = visibleItems.Length == 0
            ? new[]
            {
                (IElement)Text("표시할 항목이 없습니다.")
                    .FontColor("#6b7280")
            }
            : visibleItems.Select(item => (IElement)new TodoItemComponent(
                    item,
                    state.EditingId == item.Id,
                    StartEdit,
                    ChangeItemText,
                    SaveEdit,
                    ToggleDone,
                    RemoveItem).Key(item.Id))
                .ToArray();

        return Grid(Rows(Auto, Auto, Star),
                Div(
                        Text("할 일 검증 샘플")
                            .FontSize(26)
                            .FontWeight(FontWeightValue.Bold)
                            .FontColor("#111827"),
                        Text("controlled input, list diff, filter, item edit, remove 흐름을 한 화면에서 확인합니다.")
                            .FontSize(13)
                            .FontColor("#6b7280")
                            .Margin(top: 6)
                    )
                    .Margin(bottom: 18)
                    .Row(0),
                Div(
                        Grid(
                                TextBox(state.Draft, draft => Update(current => current with { Draft = draft }))
                                    .Key("controlled-input")
                                    .Height(36)
                                    .Padding(10, 0, 10, 0)
                                    .TextStart()
                                    .TextVCenter()
                                    .Column(0),
                                PrimaryButton("추가", AddItem).Column(1)
                            )
                            .Columns(Star, Pixels(88)),
                        Grid(
                                FilterButton("전체", state.Filter == TodoFilter.All, () => Update(current => current with { Filter = TodoFilter.All })).Column(0),
                                FilterButton("진행", state.Filter == TodoFilter.Active, () => Update(current => current with { Filter = TodoFilter.Active })).Column(1),
                                FilterButton("완료", state.Filter == TodoFilter.Done, () => Update(current => current with { Filter = TodoFilter.Done })).Column(2),
                                SecondaryButton("첫 항목 뒤로", MoveFirstToEnd).Column(3),
                                SecondaryButton("초기화", () => Update(_ => new TodoState(string.Empty, TodoFilter.All, InitialItems, null, 1))).Column(4)
                            )
                            .Columns(Pixels(74), Pixels(74), Pixels(74), Pixels(124), Pixels(90))
                            .Margin(top: 14),
                        Text($"필터: {FilterLabel(state.Filter)} · 입력 길이: {state.Draft.Length} · 표시 항목: {visibleItems.Length}")
                            .FontSize(12)
                            .FontColor("#6b7280")
                            .Margin(top: 14)
                    )
                    .Padding(18)
                    .Background("#ffffff")
                    .Brush("#e5e7eb")
                    .Thickness(1)
                    .CornerRadius(16)
                    .Row(1),
                Div(DivTypes.Scroll, listChildren)
                    .Margin(top: 18)
                    .Row(2)
            )
            .Padding(24)
            .Background("#f3f4f6");
    }

    private static IElement PrimaryButton(string label, Action onClick)
    {
        return Button(label, onClick)
            .Height(36)
            .Background("#111827")
            .FontColor("#ffffff")
            .Brush("#111827")
            .Thickness(1);
    }

    private static IElement SecondaryButton(string label, Action onClick)
    {
        return Button(label, onClick)
            .Height(34)
            .Margin(left: 8)
            .Background("#ffffff")
            .FontColor("#374151")
            .Brush("#d1d5db")
            .Thickness(1);
    }

    private static IElement FilterButton(string label, bool selected, Action onClick)
    {
        return Button(label, onClick)
            .Height(34)
            .Margin(right: 8)
            .Background(selected ? "#dbeafe" : "#ffffff")
            .FontColor(selected ? "#1d4ed8" : "#374151")
            .Brush(selected ? "#93c5fd" : "#d1d5db")
            .Thickness(1);
    }

    private static string FilterLabel(TodoFilter filter)
    {
        return filter switch
        {
            TodoFilter.Active => "진행",
            TodoFilter.Done => "완료",
            _ => "전체"
        };
    }

    private static TodoItem[] ApplyFilter(TodoItem[] items, TodoFilter filter)
    {
        return items.Where(item => filter switch
        {
            TodoFilter.Active => !item.IsDone,
            TodoFilter.Done => item.IsDone,
            _ => true
        }).ToArray();
    }
}

internal enum TodoFilter
{
    All,
    Active,
    Done
}

internal sealed record TodoItem(string Id, string Text, bool IsDone);

internal sealed record TodoState(string Draft, TodoFilter Filter, TodoItem[] Items, string? EditingId, int NextId);
