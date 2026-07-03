using System;

[assembly: System.Reflection.Metadata.MetadataUpdateHandlerAttribute(typeof(Nuri.Avalonia.HotReloadService))]

namespace Nuri.Avalonia
{
    public static class HotReloadService
    {
        public static event Action<Type[]?>? UpdateApplicationEvent;

        internal static void ClearCache(Type[]? types)
        {
        }

        internal static void UpdateApplication(Type[]? types)
        {
            UpdateApplicationEvent?.Invoke(types);
        }
    }
}
