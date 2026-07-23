using Nuri.SimplyShare.Features.Chat;
using Nuri.SimplyShare.Features.Discovery;
using Nuri.SimplyShare.Features.Settings;
using Nuri.SimplyShare.Features.Transfers;

namespace Nuri.SimplyShare.App;

internal static class AppState
{
    private static readonly object SyncRoot = new();

    public static readonly Store<AppSettings> Settings = new(AppSettings.CreateDefault());
    public static readonly Store<IReadOnlyList<DeviceInfo>> Devices = new(Array.Empty<DeviceInfo>());
    public static readonly Store<IReadOnlyList<TransferItem>> Transfers = new(Array.Empty<TransferItem>());
    public static readonly Store<IReadOnlyDictionary<string, IReadOnlyList<ChatMessage>>> Chats =
        new(new Dictionary<string, IReadOnlyList<ChatMessage>>());
    public static readonly Store<string> Status = new("Starting local services...");

    public static void SetDevices(IEnumerable<DeviceInfo> devices)
    {
        Devices.Set(devices.OrderBy(device => device.Nickname, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static void UpsertTransfer(TransferItem transfer)
    {
        lock (SyncRoot)
        {
            var items = Transfers.Value.ToList();
            var index = items.FindIndex(item => item.Id == transfer.Id);
            if (index >= 0)
                items[index] = transfer;
            else
                items.Insert(0, transfer);

            Transfers.Set(items.Take(40).ToArray());
        }
    }

    public static void AddMessage(string deviceId, ChatMessage message)
    {
        lock (SyncRoot)
        {
            var chats = Chats.Value.ToDictionary(pair => pair.Key, pair => pair.Value);
            var messages = chats.TryGetValue(deviceId, out var existing)
                ? existing.ToList()
                : new List<ChatMessage>();
            messages.Add(message);
            chats[deviceId] = messages.TakeLast(200).ToArray();
            Chats.Set(chats);
        }
    }
}
