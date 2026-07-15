using System.Collections.Generic;
using System.Linq;
using Nuri.UI.Dsl;
using Nuri.VirtualDom;

namespace Nuri.Runtime.Lifecycle
{
    public static class ComponentLifecycle
    {
        public static void CleanupRemovedComponentState(IEnumerable<PatchOperation> operations)
        {
            foreach (var removeChild in operations.OfType<RemoveChildPatch>())
                DisposeEntryHookState(removeChild.Child);

            foreach (var replaceEntry in operations.OfType<ReplaceEntryPatch>())
                DisposeEntryHookState(replaceEntry.OldEntry);
        }

        public static void FlushPendingEffects()
        {
            Component.FlushPendingEffects();
        }

        public static void DisposeSubtree(string rootComponentId)
        {
            Component.DisposeHookState(rootComponentId);
        }

        public static bool IsInSubtree(string componentId, string rootComponentId)
        {
            return RuntimeTreeIdentity.IsDescendantOrSelf(componentId, rootComponentId);
        }

        private static void DisposeEntryHookState(VirtualEntry entry)
        {
            foreach (var componentId in EnumerateComponentIds(entry).Distinct())
                Component.DisposeHookState(componentId);

            Component.DisposeHookState(entry.Id);
        }

        private static IEnumerable<string> EnumerateComponentIds(VirtualEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.ComponentId))
                yield return entry.ComponentId!;

            foreach (var child in entry.Children)
            {
                foreach (var componentId in EnumerateComponentIds(child))
                    yield return componentId;
            }
        }
    }
}
