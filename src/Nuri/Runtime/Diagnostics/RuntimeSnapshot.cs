using System;
using System.Collections.Generic;

namespace Nuri.Runtime.Diagnostics
{
    public sealed class RuntimeSnapshot
    {
        public RuntimeSnapshot(
            DateTimeOffset capturedAt,
            IReadOnlyList<ApplicationRootSnapshot> roots,
            IReadOnlyList<StoreSnapshot> stores,
            IReadOnlyList<RuntimeLogEntry> recentLogs)
        {
            CapturedAt = capturedAt;
            Roots = roots;
            Stores = stores;
            RecentLogs = recentLogs;
        }

        public DateTimeOffset CapturedAt { get; }

        public IReadOnlyList<ApplicationRootSnapshot> Roots { get; }

        public IReadOnlyList<StoreSnapshot> Stores { get; }

        public IReadOnlyList<RuntimeLogEntry> RecentLogs { get; }
    }

    public sealed class ApplicationRootSnapshot
    {
        public ApplicationRootSnapshot(string rootId, string renderer, ComponentSnapshot? rootComponent)
        {
            RootId = rootId;
            Renderer = renderer;
            RootComponent = rootComponent;
        }

        public string RootId { get; }

        public string Renderer { get; }

        public ComponentSnapshot? RootComponent { get; }
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
