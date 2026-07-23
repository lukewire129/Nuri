namespace Nuri.SimplyShare.Features.Transfers;

public enum TransferDirection
{
    Sending,
    Receiving
}

public enum TransferStatus
{
    Connecting,
    Transferring,
    Completed,
    Failed
}

public sealed record TransferItem(
    string Id,
    string Name,
    string Peer,
    TransferDirection Direction,
    long BytesTransferred,
    long TotalBytes,
    TransferStatus Status,
    string? Error = null)
{
    public double Progress => TotalBytes <= 0 ? 0 : Math.Clamp((double)BytesTransferred / TotalBytes, 0, 1);
}
