namespace Nuri.SimplyShare.Features.Chat;

internal enum ChatMessageKind
{
    Text,
    File
}

internal sealed record ChatMessage(
    string Id,
    ChatMessageKind Kind,
    bool IsSent,
    string Content,
    DateTime Timestamp)
{
    public static ChatMessage SentText(string text) => new(Guid.NewGuid().ToString("N"), ChatMessageKind.Text, true, text, DateTime.Now);
    public static ChatMessage ReceivedText(string text) => new(Guid.NewGuid().ToString("N"), ChatMessageKind.Text, false, text, DateTime.Now);
    public static ChatMessage SentFile(string path) => new(Guid.NewGuid().ToString("N"), ChatMessageKind.File, true, path, DateTime.Now);
    public static ChatMessage ReceivedFile(string path) => new(Guid.NewGuid().ToString("N"), ChatMessageKind.File, false, path, DateTime.Now);
}
