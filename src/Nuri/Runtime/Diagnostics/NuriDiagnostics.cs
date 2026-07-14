using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Nuri.VirtualDom;

namespace Nuri.Runtime.Diagnostics
{
    public static class NuriDiagnostics
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, RootRecord> Roots = new Dictionary<string, RootRecord>();
        private static readonly Dictionary<string, ComponentRecord> Components = new Dictionary<string, ComponentRecord>();
        private static readonly Dictionary<string, StoreRecord> Stores = new Dictionary<string, StoreRecord>();
        private static readonly Dictionary<string, VirtualizedItemsSnapshot> VirtualizedItems = new Dictionary<string, VirtualizedItemsSnapshot>();
        private static readonly RuntimeLogBuffer Logs = new RuntimeLogBuffer(5000);
        private static readonly HashSet<string> LoggedOnceKeys = new HashSet<string>(StringComparer.Ordinal);
        private static long _sequence;

        public static bool IsEnabled { get; private set; }

        public static event EventHandler? Changed;

        public static void Enable()
        {
            IsEnabled = true;
            Log(RuntimeLogKind.Diagnostics, null, null, "Diagnostics enabled.");
        }

        public static void Disable()
        {
            IsEnabled = false;
        }

        public static RuntimeSnapshot GetSnapshot()
        {
            lock (SyncRoot)
            {
                var roots = Roots.Values
                    .Select(root => new ApplicationRootSnapshot(
                        root.RootId,
                        root.Renderer,
                        CreateComponentTree(root.GetCurrentVirtualEntry()),
                        root.PatchBatchCount,
                        root.PatchCount,
                        root.LastPatchCount,
                        new Dictionary<PatchOperationType, int>(root.LastPatchCounts)))
                    .ToArray();

                var stores = Stores.Values
                    .Select(store => new StoreSnapshot(
                        store.StoreId,
                        store.StoreType,
                        store.ValueSummary,
                        store.Subscriptions.Values
                            .Select(subscription => new StoreSubscriptionSnapshot(
                                store.StoreId,
                                store.StoreType,
                                subscription.ComponentId,
                                subscription.HookIndex,
                                subscription.SelectedType,
                                subscription.SelectedValueSummary))
                            .ToArray()))
                    .ToArray();

                return new RuntimeSnapshot(
                    DateTimeOffset.UtcNow,
                    roots,
                    stores,
                    Logs.Snapshot(),
                    VirtualizedItems.Values.ToArray());
            }
        }

        public static void RegisterRoot(string rootId, string renderer, Func<VirtualEntry?> getCurrentVirtualEntry)
        {
            if (!IsEnabled)
                return;

            if (string.IsNullOrWhiteSpace(rootId))
                return;

            if (getCurrentVirtualEntry == null)
                return;

            lock (SyncRoot)
            {
                Roots[rootId] = new RootRecord(rootId, renderer, getCurrentVirtualEntry);
            }

            Log(RuntimeLogKind.RootRegistered, rootId, null, "Root registered.");
        }

        public static void UnregisterRoot(string rootId)
        {
            if (!IsEnabled)
                return;

            lock (SyncRoot)
            {
                Roots.Remove(rootId);
            }

            Log(RuntimeLogKind.RootUnregistered, rootId, null, "Root unregistered.");
        }

        public static void RecordComponentRendered(string componentId, string typeName)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(componentId))
                return;

            lock (SyncRoot)
            {
                var record = GetOrCreateComponent(componentId);
                record.TypeName = typeName;
                record.RenderCount++;
                record.LastRenderedSequence = _sequence + 1;
            }

            Log(RuntimeLogKind.ComponentRendered, null, componentId, "Component rendered.");
        }

        public static void RecordPatchBatch(string rootId, IReadOnlyList<PatchOperation> operations)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(rootId) || operations == null)
                return;

            lock (SyncRoot)
            {
                if (!Roots.TryGetValue(rootId, out var root))
                    return;

                root.PatchBatchCount++;
                root.PatchCount += operations.Count;
                root.LastPatchCount = operations.Count;
                root.LastPatchCounts.Clear();
                foreach (var operation in operations)
                {
                    root.LastPatchCounts.TryGetValue(operation.Type, out var count);
                    root.LastPatchCounts[operation.Type] = count + 1;
                }
            }

            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static void RecordVirtualizedItems(string hostId, int itemCount, int realizedCount)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(hostId))
                return;

            lock (SyncRoot)
            {
                VirtualizedItems[hostId] = new VirtualizedItemsSnapshot(hostId, itemCount, realizedCount);
            }

            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static void RemoveVirtualizedItems(string hostId)
        {
            if (string.IsNullOrWhiteSpace(hostId))
                return;

            bool removed;
            lock (SyncRoot)
            {
                removed = VirtualizedItems.Remove(hostId);
            }

            if (removed && IsEnabled)
                Changed?.Invoke(null, EventArgs.Empty);
        }

        public static void RecordHook(string componentId, int index, HookKind kind, string displayType, string summary)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(componentId))
                return;

            lock (SyncRoot)
            {
                var record = GetOrCreateComponent(componentId);
                record.Hooks[index] = new HookSnapshot(index, kind, displayType, summary);
            }
        }

        public static void TrimHooks(string componentId, int usedHookCount)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(componentId))
                return;

            lock (SyncRoot)
            {
                if (!Components.TryGetValue(componentId, out var record))
                    return;

                foreach (var index in record.Hooks.Keys.ToArray())
                {
                    if (index >= usedHookCount)
                        record.Hooks.Remove(index);
                }
            }
        }

        public static void DisposeComponentSubtree(string rootComponentId)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(rootComponentId))
                return;

            lock (SyncRoot)
            {
                foreach (var componentId in Components.Keys.ToArray())
                {
                    if (RuntimeTreeIdentity.IsDescendantOrSelf(componentId, rootComponentId))
                        Components.Remove(componentId);
                }

                foreach (var store in Stores.Values)
                {
                    foreach (var key in store.Subscriptions.Keys.ToArray())
                    {
                        if (RuntimeTreeIdentity.IsDescendantOrSelf(key.ComponentId, rootComponentId))
                            store.Subscriptions.Remove(key);
                    }
                }
            }

            Log(RuntimeLogKind.ComponentUnmounted, null, rootComponentId, "Component subtree disposed.");
        }

        public static void RecordComponentInvalidated(string componentId, string reason)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(componentId))
                return;

            lock (SyncRoot)
            {
                var record = GetOrCreateComponent(componentId);
                record.LastInvalidatedSequence = _sequence + 1;
            }

            Log(RuntimeLogKind.ComponentInvalidated, null, componentId, reason);
        }

        public static string RegisterStore(Type storeType, string valueSummary)
        {
            if (!IsEnabled)
                return string.Empty;

            var storeId = storeType.FullName ?? storeType.Name;
            RegisterStoreInstance(storeId, storeType.Name, valueSummary);
            return storeId;
        }

        public static void RegisterStoreInstance(string storeId, string storeType, string valueSummary)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(storeId))
                return;

            lock (SyncRoot)
            {
                if (!Stores.TryGetValue(storeId, out var store))
                {
                    store = new StoreRecord(storeId, storeType);
                    Stores[storeId] = store;
                }

                store.ValueSummary = valueSummary;
            }
        }

        public static void RecordStoreSet(string storeId, string valueSummary)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(storeId))
                return;

            lock (SyncRoot)
            {
                if (Stores.TryGetValue(storeId, out var store))
                    store.ValueSummary = valueSummary;
            }

            Log(RuntimeLogKind.StoreChanged, null, null, "Store changed: " + storeId);
        }

        public static void RecordStoreSubscription(
            string storeId,
            string storeType,
            string componentId,
            int hookIndex,
            string selectedType,
            string selectedValueSummary)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(storeId) || string.IsNullOrWhiteSpace(componentId))
                return;

            lock (SyncRoot)
            {
                if (!Stores.TryGetValue(storeId, out var store))
                {
                    store = new StoreRecord(storeId, storeType);
                    Stores[storeId] = store;
                }

                store.Subscriptions[new StoreSubscriptionSnapshotKey(componentId, hookIndex)] =
                    new StoreSubscriptionRecord(componentId, hookIndex, selectedType, selectedValueSummary);
            }
        }

        public static void RemoveStoreSubscription(string storeId, string componentId, int hookIndex)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(storeId))
                return;

            lock (SyncRoot)
            {
                if (Stores.TryGetValue(storeId, out var store))
                    store.Subscriptions.Remove(new StoreSubscriptionSnapshotKey(componentId, hookIndex));
            }
        }

        public static void ClearLogs()
        {
            lock (SyncRoot)
            {
                Logs.Clear();
                LoggedOnceKeys.Clear();
            }

            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static void LogApp(
            string message,
            [CallerFilePath] string? sourceFile = null,
            [CallerMemberName] string? sourceMember = null,
            [CallerLineNumber] int sourceLine = 0)
        {
            Log(
                RuntimeLogKind.AppLog,
                null,
                null,
                message,
                null,
                sourceFile,
                sourceMember,
                sourceLine);
        }

        public static void LogConsole(
            string message,
            string? sourceType,
            string? sourceFile,
            string? sourceMember,
            int? sourceLine)
        {
            Log(RuntimeLogKind.Console, null, null, message, sourceType, sourceFile, sourceMember, sourceLine);
        }

        public static void Log(RuntimeLogKind kind, string? rootId, string? componentId, string message)
        {
            Log(kind, rootId, componentId, message, null, null, null, null);
        }

        public static void LogOnce(RuntimeLogKind kind, string dedupeKey, string? rootId, string? componentId, string message)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(dedupeKey))
                return;

            RuntimeLogEntry entry;
            lock (SyncRoot)
            {
                if (!LoggedOnceKeys.Add(dedupeKey))
                    return;

                entry = new RuntimeLogEntry(
                    ++_sequence,
                    DateTimeOffset.UtcNow,
                    kind,
                    rootId,
                    componentId,
                    message);
                Logs.Add(entry);
            }

            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static void Log(
            RuntimeLogKind kind,
            string? rootId,
            string? componentId,
            string message,
            string? sourceType,
            string? sourceFile,
            string? sourceMember,
            int? sourceLine)
        {
            if (!IsEnabled)
                return;

            RuntimeLogEntry entry;
            lock (SyncRoot)
            {
                entry = new RuntimeLogEntry(
                    ++_sequence,
                    DateTimeOffset.UtcNow,
                    kind,
                    rootId,
                    componentId,
                    message,
                    sourceType,
                    sourceFile,
                    sourceMember,
                    sourceLine);
                Logs.Add(entry);
            }

            Changed?.Invoke(null, EventArgs.Empty);
        }

        private static ComponentSnapshot? CreateComponentTree(VirtualEntry? entry)
        {
            if (entry == null)
                return null;

            var componentId = !string.IsNullOrWhiteSpace(entry.ComponentId) ? entry.ComponentId! : entry.Id;
            Components.TryGetValue(componentId, out var record);

            return new ComponentSnapshot(
                componentId,
                record?.TypeName ?? entry.Type,
                entry.Type,
                entry.Key,
                record?.RenderCount ?? 0,
                record?.LastInvalidatedSequence,
                record?.LastRenderedSequence,
                record?.Hooks.Values.OrderBy(hook => hook.Index).ToArray() ?? Array.Empty<HookSnapshot>(),
                entry.Children.Select(CreateComponentTree).Where(child => child != null).Cast<ComponentSnapshot>().ToArray());
        }

        private static ComponentRecord GetOrCreateComponent(string componentId)
        {
            if (!Components.TryGetValue(componentId, out var record))
            {
                record = new ComponentRecord(componentId);
                Components[componentId] = record;
            }

            return record;
        }

        private sealed class RootRecord
        {
            public RootRecord(string rootId, string renderer, Func<VirtualEntry?> getCurrentVirtualEntry)
            {
                RootId = rootId;
                Renderer = renderer;
                GetCurrentVirtualEntry = getCurrentVirtualEntry;
            }

            public string RootId { get; }

            public string Renderer { get; }

            public Func<VirtualEntry?> GetCurrentVirtualEntry { get; }

            public long PatchBatchCount { get; set; }

            public long PatchCount { get; set; }

            public int LastPatchCount { get; set; }

            public Dictionary<PatchOperationType, int> LastPatchCounts { get; } = new Dictionary<PatchOperationType, int>();
        }

        private sealed class ComponentRecord
        {
            public ComponentRecord(string componentId)
            {
                ComponentId = componentId;
            }

            public string ComponentId { get; }

            public string TypeName { get; set; } = string.Empty;

            public int RenderCount { get; set; }

            public long? LastInvalidatedSequence { get; set; }

            public long? LastRenderedSequence { get; set; }

            public Dictionary<int, HookSnapshot> Hooks { get; } = new Dictionary<int, HookSnapshot>();
        }

        private sealed class StoreRecord
        {
            public StoreRecord(string storeId, string storeType)
            {
                StoreId = storeId;
                StoreType = storeType;
            }

            public string StoreId { get; }

            public string StoreType { get; }

            public string ValueSummary { get; set; } = string.Empty;

            public Dictionary<StoreSubscriptionSnapshotKey, StoreSubscriptionRecord> Subscriptions { get; } =
                new Dictionary<StoreSubscriptionSnapshotKey, StoreSubscriptionRecord>();
        }

        private sealed class StoreSubscriptionRecord
        {
            public StoreSubscriptionRecord(string componentId, int hookIndex, string selectedType, string selectedValueSummary)
            {
                ComponentId = componentId;
                HookIndex = hookIndex;
                SelectedType = selectedType;
                SelectedValueSummary = selectedValueSummary;
            }

            public string ComponentId { get; }

            public int HookIndex { get; }

            public string SelectedType { get; }

            public string SelectedValueSummary { get; }
        }

        private sealed class StoreSubscriptionSnapshotKey : IEquatable<StoreSubscriptionSnapshotKey>
        {
            public StoreSubscriptionSnapshotKey(string componentId, int hookIndex)
            {
                ComponentId = componentId;
                HookIndex = hookIndex;
            }

            public string ComponentId { get; }

            public int HookIndex { get; }

            public bool Equals(StoreSubscriptionSnapshotKey? other)
            {
                return other != null
                    && string.Equals(ComponentId, other.ComponentId, StringComparison.Ordinal)
                    && HookIndex == other.HookIndex;
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as StoreSubscriptionSnapshotKey);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(ComponentId) * 397) ^ HookIndex;
                }
            }
        }
    }
}
