namespace Nuri.PreviewHost;

public static class PreviewProtocol
{
    public const string Name = "nuri-preview-v1";
}

public sealed record PreviewCaptureStatus(string Message, bool IsBuilding, bool HasError);
