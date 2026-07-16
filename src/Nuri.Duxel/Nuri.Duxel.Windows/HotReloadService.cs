using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Nuri.Duxel.HotReloadService))]

namespace Nuri.Duxel;

public static class HotReloadService
{
    internal static event Action<Type[]?>? UpdateApplicationEvent;

    internal static void ClearCache(Type[]? types)
    {
    }

    internal static void UpdateApplication(Type[]? types)
    {
        UpdateApplicationEvent?.Invoke(types);
    }
}
