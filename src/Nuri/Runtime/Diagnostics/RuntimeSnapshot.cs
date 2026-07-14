using System;
using System.Collections.Generic;
using Nuri.VirtualDom;

namespace Nuri.Runtime.Diagnostics
{
    public sealed class RuntimeSnapshot
    {
        public RuntimeSnapshot(
            DateTimeOffset capturedAt,
            IReadOnlyList<ApplicationRootSnapshot> roots,
            IReadOnlyList<StoreSnapshot> stores,
            IReadOnlyList<RuntimeLogEntry> recentLogs)
            : this(capturedAt, roots, stores, recentLogs, Array.Empty<VirtualizedItemsSnapshot>())
        {
        }

        public RuntimeSnapshot(
            DateTimeOffset capturedAt,
            IReadOnlyList<ApplicationRootSnapshot> roots,
            IReadOnlyList<StoreSnapshot> stores,
            IReadOnlyList<RuntimeLogEntry> recentLogs,
            IReadOnlyList<VirtualizedItemsSnapshot> virtualizedItems)
        {
            CapturedAt = capturedAt;
            Roots = roots;
            Stores = stores;
            RecentLogs = recentLogs;
            VirtualizedItems = virtualizedItems;
        }

        public DateTimeOffset CapturedAt { get; }

        public IReadOnlyList<ApplicationRootSnapshot> Roots { get; }

        public IReadOnlyList<StoreSnapshot> Stores { get; }

        public IReadOnlyList<RuntimeLogEntry> RecentLogs { get; }

        public IReadOnlyList<VirtualizedItemsSnapshot> VirtualizedItems { get; }
    }

    public sealed class ApplicationRootSnapshot
    {
        public ApplicationRootSnapshot(string rootId, string renderer, ComponentSnapshot? rootComponent)
            : this(rootId, renderer, rootComponent, 0, 0, 0, new Dictionary<PatchOperationType, int>())
        {
        }

        public ApplicationRootSnapshot(
            string rootId,
            string renderer,
            ComponentSnapshot? rootComponent,
            long patchBatchCount,
            long patchCount,
            int lastPatchCount,
            IReadOnlyDictionary<PatchOperationType, int> lastPatchCounts)
        {
            RootId = rootId;
            Renderer = renderer;
            RootComponent = rootComponent;
            PatchBatchCount = patchBatchCount;
            PatchCount = patchCount;
            LastPatchCount = lastPatchCount;
            LastPatchCounts = lastPatchCounts;
        }

        public string RootId { get; }

        public string Renderer { get; }

        public ComponentSnapshot? RootComponent { get; }

        public long PatchBatchCount { get; }

        public long PatchCount { get; }

        public int LastPatchCount { get; }

        public IReadOnlyDictionary<PatchOperationType, int> LastPatchCounts { get; }
    }

    public sealed class VirtualizedItemsSnapshot
    {
        public VirtualizedItemsSnapshot(string hostId, int itemCount, int realizedCount)
        {
            HostId = hostId;
            ItemCount = itemCount;
            RealizedCount = realizedCount;
        }

        public string HostId { get; }

        public int ItemCount { get; }

        public int RealizedCount { get; }
    }

    public sealed class ComponentSnapshot
    {
        public ComponentSnapshot(
            string componentId,
            string typeName,
            string entryType,
            string? key,
            int renderCount,
            long? lastInvalidatedSequence,
            long? lastRenderedSequence,
            IReadOnlyList<HookSnapshot> hooks,
            IReadOnlyList<ComponentSnapshot> children)
        {
            ComponentId = componentId;
            TypeName = typeName;
            EntryType = entryType;
            Key = key;
            RenderCount = renderCount;
            LastInvalidatedSequence = lastInvalidatedSequence;
            LastRenderedSequence = lastRenderedSequence;
            Hooks = hooks;
            Children = children;
        }

        public string ComponentId { get; }

        public string TypeName { get; }

        public string EntryType { get; }

        public string? Key { get; }

        public int RenderCount { get; }

        public long? LastInvalidatedSequence { get; }

        public long? LastRenderedSequence { get; }

        public IReadOnlyList<HookSnapshot> Hooks { get; }

        public IReadOnlyList<ComponentSnapshot> Children { get; }
    }

    public sealed class HookSnapshot
    {
        public HookSnapshot(int index, HookKind kind, string displayType, string summary)
        {
            Index = index;
            Kind = kind;
            DisplayType = displayType;
            Summary = summary;
        }

        public int Index { get; }

        public HookKind Kind { get; }

        public string DisplayType { get; }

        public string Summary { get; }
    }

    public enum HookKind
    {
        State,
        Reducer,
        Ref,
        Memo,
        Effect,
        Store
    }
}
