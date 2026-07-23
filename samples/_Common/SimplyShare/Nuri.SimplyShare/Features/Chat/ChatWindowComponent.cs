using Nuri.SimplyShare.App;
using Nuri.SimplyShare.Features.Discovery;
using Nuri.SimplyShare.Shared;

namespace Nuri.SimplyShare.Features.Chat;

public sealed class ChatWindowComponent : Component
{
    private readonly DeviceInfo _device;

    public ChatWindowComponent(DeviceInfo device) => _device = device;

    public override IElement Render()
    {
        var messages = useStore(
            AppState.Chats,
            chats => chats.TryGetValue(_device.DeviceId, out var values) ? values : Array.Empty<ChatMessage>());
        var (input, setInput) = useState("");
        var inputRef = useLatest(input);
        var (busy, setBusy) = useState(false);
        var (error, setError) = useState("");

        async void SendText()
        {
            var text = inputRef.Current.Trim();
            if (text.Length == 0 || busy)
                return;

            setBusy(_ => true);
            setError(_ => "");
            try
            {
                await AppServices.Transfers.SendTextAsync(_device, text);
                AppState.AddMessage(_device.DeviceId, ChatMessage.SentText(text));
                inputRef.Current = "";
                setInput(_ => "");
            }
            catch (Exception exception)
            {
                setError(_ => exception.Message);
            }
            finally
            {
                setBusy(_ => false);
            }
        }

        async void SendFile()
        {
            if (busy)
                return;

            var paths = AppServices.Host.SelectFiles($"Send files to {_device.Nickname}");
            if (paths.Length == 0)
                return;

            setBusy(_ => true);
            setError(_ => "");
            try
            {
                foreach (var path in paths)
                {
                    await AppServices.Transfers.SendFilesAsync(_device, [path]);
                    AppState.AddMessage(_device.DeviceId, ChatMessage.SentFile(path));
                }
            }
            catch (Exception exception)
            {
                setError(_ => exception.Message);
            }
            finally
            {
                setBusy(_ => false);
            }
        }

        var sendButton = Button(busy ? "Working..." : "Send", SendText)
            .Width(92)
            .Height(42)
            .Background(Palette.Accent)
            .FontColor(Palette.White)
            .Brush(Palette.AccentDark)
            .Thickness(1)
            .Column(2);
        sendButton.SetProperty("IsEnabled", !busy);

        return Grid(
                new ChatHeader(_device).Row(0),
                Div(DivTypes.Scroll,
                        messages.Count == 0
                            ? new ChatEmptyState(_device.Nickname)
                            : Div(messages.Select(message => (IElement)new MessageBubble(message).Key(message.Id)).ToArray())
                                .Padding(20))
                    .Row(1)
                    .Background(Palette.Canvas),
                Div(
                        string.IsNullOrEmpty(error)
                            ? Div().Height(0)
                            : Text(error).FontSize(11).FontColor(Palette.Danger).Margin(bottom: 8),
                        Grid(
                                Button("Attach", SendFile)
                                    .Width(82)
                                    .Height(42)
                                    .Background(Palette.White)
                                    .Brush(Palette.Border)
                                    .Thickness(1)
                                    .Column(0),
                                TextBox(input, value =>
                                    {
                                        inputRef.Current = value;
                                        setInput(_ => value);
                                    })
                                    .Height(42)
                                    .Padding(12, 0, 12, 0)
                                    .Margin(left: 8, right: 8)
                                    .Column(1),
                                sendButton)
                            .Columns(Auto, Star, Auto))
                    .Padding(14)
                    .Background(Palette.White)
                    .Row(2))
            .Rows(Auto, Star, Auto);
    }
}

internal sealed class ChatHeader : Component
{
    private readonly DeviceInfo _device;

    public ChatHeader(DeviceInfo device) => _device = device;

    public override IElement Render()
    {
        return Div(DivTypes.Row,
                Div(Text("ON").FontSize(9).FontWeight(FontWeightValue.Bold).FontColor(Palette.Success).Center())
                    .Size(36, 36)
                    .Background(Palette.SoftGreen)
                    .CornerRadius(18)
                    .Margin(right: 12),
                Div(
                    Text(_device.Nickname).FontSize(16).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink),
                    Text($"Encrypted direct connection to {_device.IpAddress}").FontSize(10).FontColor(Palette.Muted).Margin(top: 3)))
            .Padding(18, 14, 18, 14)
            .Background(Palette.White);
    }
}

internal sealed class ChatEmptyState : Component
{
    private readonly string _name;

    public ChatEmptyState(string name) => _name = name;

    public override IElement Render()
    {
        return Div(
                Text("Start a private conversation").FontSize(20).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink).Center(),
                Text($"Messages and files sent to {_name} are encrypted in transit.")
                    .FontSize(12)
                    .FontColor(Palette.Muted)
                    .HCenter()
                    .Margin(top: 8))
            .Padding(30)
            .Center();
    }
}

internal sealed class MessageBubble : Component
{
    private readonly ChatMessage _message;

    public MessageBubble(ChatMessage message) => _message = message;

    public override IElement Render()
    {
        var (visible, setVisible) = useState(false);

        useEffect(() =>
        {
            setVisible(_ => true);
        }, []);

        var content = _message.Kind == ChatMessageKind.File
            ? $"File: {Path.GetFileName(_message.Content)}"
            : _message.Content;
        var bubble = Div(
                Text(content).FontSize(13).FontColor(Palette.Ink),
                Text(_message.Timestamp.ToString("HH:mm")).FontSize(9).FontColor(Palette.Muted).Margin(top: 5))
            .Padding(12)
            .Width(340)
            .Background(_message.IsSent ? Palette.SoftAccent : Palette.White)
            .Brush(_message.IsSent ? Palette.SoftAccent : Palette.Border)
            .Thickness(1)
            .CornerRadius(12)
            .Margin(bottom: 10)
            .Opacity(visible ? 1 : 0)
            .TranslateX(visible ? 0 : _message.IsSent ? 14 : -14)
            .Transition(TimeSpan.FromMilliseconds(190), EasingValue.CubicOut);

        return _message.IsSent ? bubble.End() : bubble.Start();
    }
}
