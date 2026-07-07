using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.TodoValidationSample.Components;

internal sealed class TodoItemComponent : Component
{
    private readonly TodoItem _item;
    private readonly bool _isEditing;
    private readonly Action<string> _startEdit;
    private readonly Action<string, string> _changeText;
    private readonly Action<string> _saveEdit;
    private readonly Action<string> _toggleDone;
    private readonly Action<string> _removeItem;

    public TodoItemComponent(
        TodoItem item,
        bool isEditing,
        Action<string> startEdit,
        Action<string, string> changeText,
        Action<string> saveEdit,
        Action<string> toggleDone,
        Action<string> removeItem)
    {
        _item = item;
        _isEditing = isEditing;
        _startEdit = startEdit;
        _changeText = changeText;
        _saveEdit = saveEdit;
        _toggleDone = toggleDone;
        _removeItem = removeItem;
    }

    public override IElement Render()
    {
        return Grid(
                _isEditing
                    ? TextBox(_item.Text, text => _changeText(_item.Id, text))
                        .Key($"edit-{_item.Id}")
                        .Height(34)
                        .Padding(10, 0, 10, 0)
                        .TextStart()
                        .TextVCenter()
                        .Column(0)
                    : Div(
                            Text(_item.IsDone ? "완료" : "진행")
                                .FontSize(11)
                                .FontColor(_item.IsDone ? "#047857" : "#2563eb"),
                            Text(_item.Text)
                                .FontSize(15)
                                .FontColor(_item.IsDone ? "#6b7280" : "#111827")
                                .Margin(top: 4)
                        )
                        .Column(0),
                SecondaryButton(_item.IsDone ? "다시 열기" : "완료", () => _toggleDone(_item.Id)).Column(1),
                SecondaryButton(_isEditing ? "저장" : "수정", OnEditButtonClick).Column(2),
                DangerButton("삭제", () => _removeItem(_item.Id)).Column(3)
            )
            .Columns(Star, Pixels(92), Pixels(82), Pixels(82))
            .Padding(14)
            .Margin(bottom: 10, right: 8)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(14);
    }

    private void OnEditButtonClick()
    {
        if (_isEditing)
            _saveEdit(_item.Id);
        else
            _startEdit(_item.Id);
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

    private static IElement DangerButton(string label, Action onClick)
    {
        return Button(label, onClick)
            .Height(34)
            .Margin(left: 8)
            .Background("#fff1f2")
            .FontColor("#be123c")
            .Brush("#fecdd3")
            .Thickness(1);
    }
}
