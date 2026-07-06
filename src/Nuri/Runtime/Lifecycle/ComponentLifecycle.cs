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
                Component.DisposeHookState(removeChild.Child.Id);

            foreach (var replaceEntry in operations.OfType<ReplaceEntryPatch>())
                Component.DisposeHookState(replaceEntry.OldEntry.Id);
        }

        public static void FlushPendingEffects()
        {
            Component.FlushPendingEffects();
        }

        public static void DisposeSubtree(string rootComponentId)
        {
            Component.DisposeHookState(rootComponentId);
        }
    }
}
