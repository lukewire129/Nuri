using System;
using System.Collections.Generic;
using Nuri.Runtime;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Events;

namespace Nuri.UI
{
    public abstract class ComponentBase<TElement, TAnimation> : IElement<TElement, TAnimation>, IDisposable
    {
        private static readonly StateStore StateStore = new StateStore();
        private static readonly object HookSyncRoot = new object();
        private static readonly Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, MemoHookState>> MemoStore = new Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, MemoHookState>>();
        private static readonly Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, EffectHookState>> EffectStore = new Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, EffectHookState>>();
        private static readonly Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, IStoreSubscription>> StoreHookStore = new Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, IStoreSubscription>>();
        private static readonly Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, HashSet<int>> PendingEffects = new Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, HashSet<int>>();
        private int _stateIndex;
        private bool _hasUsedHooks;
        private string? _registeredRuntimeId;
        private RuntimeTreeIdentity.RuntimeTreeNode? _runtimeNode;

        public string ParentId { get; set; } = "0";

        public string Id { get; set; } = "0";

        public string Type { get; set; } = "Component";

        public string Kind { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public List<TElement> Children { get; set; } = new List<TElement>();

        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, Delegate> Events { get; set; } = new Dictionary<string, Delegate>();

        public Dictionary<string, VirtualEvent> VirtualEvents { get; set; } = new Dictionary<string, VirtualEvent>();

        public Dictionary<string, TAnimation> Animations { get; set; } = new Dictionary<string, TAnimation>();

        public string LastPropertyName { get; set; } = string.Empty;

        public bool TryGetValue(string propertyName, out object value)
        {
            if (Properties.TryGetValue(propertyName, out var temp))
            {
                value = temp;
                return true;
            }

            value = default!;
            return false;
        }

        public void LoadNodeNumber(string parentId, int myId)
        {
            ParentId = parentId;
            Id = string.IsNullOrWhiteSpace(Key)
                ? $"{parentId}_{myId}"
                : $"{parentId}#key:{Key}";
            RegisterRuntimeIdentity(parentId);
        }

        internal void LoadNodeNumberByPosition(string parentId, int myId)
        {
            ParentId = parentId;
            Id = $"{parentId}_{myId}";
            RegisterRuntimeIdentity(parentId);
        }

        private void RegisterRuntimeIdentity(string parentId)
        {
            if (_registeredRuntimeId != null && !string.Equals(_registeredRuntimeId, Id, StringComparison.Ordinal))
                RuntimeTreeIdentity.Unregister(_registeredRuntimeId);
            _runtimeNode = RuntimeTreeIdentity.Register(Id, parentId);
            _registeredRuntimeId = Id;
        }

        internal RuntimeTreeIdentity.RuntimeTreeNode RuntimeNode => GetRuntimeNode();

        private RuntimeTreeIdentity.RuntimeTreeNode GetRuntimeNode()
        {
            if (_runtimeNode == null
                || !string.Equals(_runtimeNode.Id, Id, StringComparison.Ordinal))
                _runtimeNode = RuntimeTreeIdentity.GetNode(Id);

            return _runtimeNode;
        }

        public TElement SetProperty(string name, object value)
        {
            Properties[name] = value;
            LastPropertyName = name;
            return (TElement)(object)this;
        }

        public TElement AddEvent(string eventName, Delegate handler)
        {
            Events[eventName] = handler;
            return (TElement)(object)this;
        }

        public TElement AddVirtualEvent(string eventName, VirtualEvent handler)
        {
            VirtualEvents[eventName] = handler;
            return (TElement)(object)this;
        }

        public TElement AddAnimation(string animationName, TAnimation animation)
        {
            Animations[animationName] = animation;
            return (TElement)(object)this;
        }

        protected (T state, Action<Func<T, T>> setState) useState<T>(T initialValue)
        {
            _hasUsedHooks = true;
            var componentId = Id;
            var componentNode = GetRuntimeNode();
            var index = _stateIndex;
            var stateEntries = StateStore.GetOrCreateComponentStateEntries(componentNode);
            StateHookState<T> hook;
            if (!stateEntries.TryGetValue(index, out var entry))
            {
                hook = new StateHookState<T>(initialValue, this, componentId);
                stateEntries[index] = hook;
            }
            else if (entry is StateHookState<T> typedHook)
            {
                hook = typedHook;
                hook.UpdateOwner(this, componentId);
            }
            else if (entry is T typedState)
            {
                hook = new StateHookState<T>(typedState, this, componentId);
                stateEntries[index] = hook;
            }
            else
                throw new InvalidOperationException($"Cannot change state type at index {index} from {entry?.GetType()} to {typeof(T)}.");

            RecordHookValue(componentId, index, HookKind.State, typeof(T).Name, hook.Value);

            _stateIndex++;
            return (hook.Value, hook.Setter);
        }

        protected (TState state, Action<TAction> dispatch) useReducer<TState, TAction>(Func<TState, TAction, TState> reducer, TState initialState)
        {
            if (reducer == null)
                throw new ArgumentNullException(nameof(reducer));

            _hasUsedHooks = true;
            var componentId = Id;
            var componentNode = GetRuntimeNode();
            var index = _stateIndex;
            var state = StateStore.GetOrCreateState(componentNode, index, initialState);
            RecordHookValue(componentId, index, HookKind.Reducer, typeof(TState).Name, state);

            void Dispatch(TAction action)
            {
                var currentState = StateStore.GetOrCreateState(componentNode, index, initialState);
                var newState = reducer(currentState, action);
                if (EqualityComparer<TState>.Default.Equals(currentState, newState))
                    return;

                StateStore.UpdateState(componentNode, index, newState);
                Id = componentId;
                OnStateChanged();
            }

            _stateIndex++;
            return (state, Dispatch);
        }

        protected Ref<T> useRef<T>(T initialValue)
        {
            _hasUsedHooks = true;
            var componentId = Id;
            var componentNode = GetRuntimeNode();
            var index = _stateIndex;
            var reference = StateStore.GetOrCreateState(componentNode, index, new Ref<T>(initialValue));
            RecordHookValue(componentId, index, HookKind.Ref, typeof(T).Name, reference.Current);
            _stateIndex++;
            return reference;
        }

        protected Ref<T> useLatest<T>(T value)
        {
            var reference = useRef(value);
            reference.Current = value;
            return reference;
        }

        protected T useStore<T>(Store<T> store)
        {
            return useStore(store, value => value);
        }

        protected TResult useStore<T, TResult>(Store<T> store, Func<T, TResult> selector)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            _hasUsedHooks = true;
            var componentId = Id;
            var componentNode = GetRuntimeNode();
            var index = _stateIndex;
            var selectedValue = selector(store.Value);

            lock (HookSyncRoot)
            {
                var hooks = GetOrCreateHooks(StoreHookStore, componentNode);
                if (!hooks.TryGetValue(index, out var subscription)
                    || !ReferenceEquals(subscription.Store, store))
                {
                    subscription?.Dispose();
                    hooks[index] = store.SubscribeComponent(componentId, index, selector, OnStateChanged);
                }
                else
                {
                    hooks[index] = store.SubscribeComponent(componentId, index, selector, OnStateChanged);
                }
            }

            RecordHookValue(componentId, index, HookKind.Store, typeof(TResult).Name, selectedValue);
            _stateIndex++;
            return selectedValue;
        }

        protected T useMemo<T>(Func<T> factory, params object?[] dependencies)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _hasUsedHooks = true;
            var componentNode = GetRuntimeNode();
            var index = _stateIndex;

            lock (HookSyncRoot)
            {
                var hooks = GetOrCreateHooks(MemoStore, componentNode);
                if (!hooks.TryGetValue(index, out var hook)
                    || hook is not MemoHookState<T> typedHook
                    || DependenciesChanged(typedHook.Dependencies, dependencies))
                {
                    typedHook = new MemoHookState<T>(factory(), CloneDependencies(dependencies) ?? Array.Empty<object?>());
                    hooks[index] = typedHook;
                }

                RecordHookValue(Id, index, HookKind.Memo, typeof(T).Name, typedHook.Value);
                _stateIndex++;
                return typedHook.Value;
            }
        }

        protected void useEffect(Action effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            useEffect(() =>
            {
                effect();
                return null;
            }, null);
        }

        protected void useEffect(Action effect, params object?[] dependencies)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            useEffect(() =>
            {
                effect();
                return null;
            }, dependencies);
        }

        protected void useEffect(Func<Action?> effect)
        {
            useEffect(effect, null);
        }

        protected void useEffect(Func<Action?> effect, params object?[]? dependencies)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            _hasUsedHooks = true;
            var componentNode = GetRuntimeNode();
            var index = _stateIndex;

            lock (HookSyncRoot)
            {
                var hooks = GetOrCreateHooks(EffectStore, componentNode);
                if (!hooks.TryGetValue(index, out var hook)
                    || DependenciesChanged(hook.Dependencies, dependencies))
                {
                    hooks[index] = new EffectHookState(effect, CloneDependencies(dependencies))
                    {
                        Cleanup = hook?.Cleanup
                    };
                    GetOrCreatePendingEffects(componentNode).Add(index);
                }

                RecordEffectHook(Id, index, dependencies);
            }

            _stateIndex++;
        }

        protected void ResetStateIndex()
        {
            _stateIndex = 0;
        }

        public void ResetStateIndexForRender()
        {
            _runtimeNode = RuntimeTreeIdentity.GetNode(Id);
            ResetStateIndex();
        }

        protected void CompleteHookRender()
        {
            if (NuriDiagnostics.IsEnabled)
                NuriDiagnostics.RecordComponentRendered(Id, GetType().Name);

            if (_hasUsedHooks || _stateIndex > 0)
                TrimHookStateForComponent(GetRuntimeNode(), Id, _stateIndex);
        }

        protected static void FlushPendingEffectsForRender()
        {
            List<(RuntimeTreeIdentity.RuntimeTreeNode component, int index)> pending;

            lock (HookSyncRoot)
            {
                pending = new List<(RuntimeTreeIdentity.RuntimeTreeNode component, int index)>();
                foreach (var component in PendingEffects)
                {
                    foreach (var index in component.Value)
                        pending.Add((component.Key, index));
                }

                PendingEffects.Clear();
            }

            foreach (var (component, index) in pending)
            {
                Action? cleanup = null;
                Func<Action?>? effectFactory = null;

                lock (HookSyncRoot)
                {
                    if (!EffectStore.TryGetValue(component, out var hooks)
                        || !hooks.TryGetValue(index, out var hook))
                        continue;

                    cleanup = hook.Cleanup;
                    effectFactory = hook.Effect;
                    hook.Cleanup = null;
                }

                cleanup?.Invoke();
                var nextCleanup = effectFactory();

                lock (HookSyncRoot)
                {
                    if (EffectStore.TryGetValue(component, out var hooks)
                        && hooks.TryGetValue(index, out var hook))
                        hook.Cleanup = nextCleanup;
                }
            }
        }

        protected static void DisposeHookStateForSubtree(string rootComponentId)
        {
            if (string.IsNullOrWhiteSpace(rootComponentId))
                return;

            List<Action?> cleanups;

            lock (HookSyncRoot)
            {
                cleanups = new List<Action?>();

                var componentNodes = new HashSet<RuntimeTreeIdentity.RuntimeTreeNode>(EffectStore.Keys);
                componentNodes.UnionWith(MemoStore.Keys);
                componentNodes.UnionWith(StoreHookStore.Keys);
                componentNodes.Add(RuntimeTreeIdentity.GetNode(rootComponentId));

                foreach (var componentNode in componentNodes)
                {
                    if (!RuntimeTreeIdentity.IsDescendantOrSelf(componentNode.Id, rootComponentId))
                        continue;

                    if (EffectStore.TryGetValue(componentNode, out var effectHooks))
                    {
                        foreach (var hook in effectHooks.Values)
                            cleanups.Add(hook.Cleanup);

                        EffectStore.Remove(componentNode);
                    }

                    PendingEffects.Remove(componentNode);
                    MemoStore.Remove(componentNode);
                    DisposeStoreHooks(componentNode);
                    StateStore.RemoveComponentState(componentNode);
                }

                NuriDiagnostics.DisposeComponentSubtree(rootComponentId);
                RuntimeTreeIdentity.RemoveSubtree(rootComponentId);
            }

            foreach (var cleanup in cleanups)
                cleanup?.Invoke();
        }

        protected static void TrimHookStateForComponent(string componentId, int usedHookCount)
        {
            TrimHookStateForComponent(RuntimeTreeIdentity.GetNode(componentId), componentId, usedHookCount);
        }

        private static void TrimHookStateForComponent(RuntimeTreeIdentity.RuntimeTreeNode componentNode, string componentId, int usedHookCount)
        {
            if (string.IsNullOrWhiteSpace(componentId))
                return;

            List<Action?> cleanups;

            lock (HookSyncRoot)
            {
                cleanups = new List<Action?>();
                StateStore.TrimComponentState(componentNode, usedHookCount);
                TrimStateIndexes(MemoStore, componentNode, usedHookCount);
                TrimStoreHooks(componentNode, usedHookCount);
                NuriDiagnostics.TrimHooks(componentId, usedHookCount);

                if (EffectStore.TryGetValue(componentNode, out var effectHooks))
                {
                    foreach (var index in new List<int>(effectHooks.Keys))
                    {
                        if (index < usedHookCount)
                            continue;

                        cleanups.Add(effectHooks[index].Cleanup);
                        effectHooks.Remove(index);
                    }

                    if (effectHooks.Count == 0)
                        EffectStore.Remove(componentNode);
                }

                if (PendingEffects.TryGetValue(componentNode, out var pendingIndexes))
                {
                    pendingIndexes.RemoveWhere(index => index >= usedHookCount);
                    if (pendingIndexes.Count == 0)
                        PendingEffects.Remove(componentNode);
                }
            }

            foreach (var cleanup in cleanups)
                cleanup?.Invoke();
        }

        protected virtual void OnStateChanged()
        {
        }

        private static void RecordHookValue<T>(string componentId, int index, HookKind kind, string displayType, T value)
        {
            if (NuriDiagnostics.IsEnabled)
                NuriDiagnostics.RecordHook(componentId, index, kind, displayType, DiagnosticsValueFormatter.Summary(value));
        }

        private static void RecordEffectHook(string componentId, int index, object?[]? dependencies)
        {
            if (NuriDiagnostics.IsEnabled)
                NuriDiagnostics.RecordHook(componentId, index, HookKind.Effect, "Effect", DiagnosticsValueFormatter.DependenciesSummary(dependencies));
        }

        private static Dictionary<int, THook> GetOrCreateHooks<THook>(Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, THook>> store, RuntimeTreeIdentity.RuntimeTreeNode component)
        {
            if (!store.TryGetValue(component, out var hooks))
            {
                hooks = new Dictionary<int, THook>();
                store[component] = hooks;
            }

            return hooks;
        }

        private static HashSet<int> GetOrCreatePendingEffects(RuntimeTreeIdentity.RuntimeTreeNode component)
        {
            if (!PendingEffects.TryGetValue(component, out var indexes))
            {
                indexes = new HashSet<int>();
                PendingEffects[component] = indexes;
            }

            return indexes;
        }

        private static object?[]? CloneDependencies(object?[]? dependencies)
        {
            if (dependencies == null)
                return null;

            var clone = new object?[dependencies.Length];
            Array.Copy(dependencies, clone, dependencies.Length);
            return clone;
        }

        private static bool DependenciesChanged(object?[]? previous, object?[]? current)
        {
            if (current == null)
                return true;

            if (previous == null || previous.Length != current.Length)
                return true;

            for (var i = 0; i < current.Length; i++)
            {
                if (!Equals(previous[i], current[i]))
                    return true;
            }

            return false;
        }

        private static void TrimStateIndexes<T>(Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, T>> store, RuntimeTreeIdentity.RuntimeTreeNode component, int usedHookCount)
        {
            if (!store.TryGetValue(component, out var hooks))
                return;

            foreach (var index in new List<int>(hooks.Keys))
            {
                if (index >= usedHookCount)
                    hooks.Remove(index);
            }

            if (hooks.Count == 0)
                store.Remove(component);
        }

        private static void TrimStoreHooks(RuntimeTreeIdentity.RuntimeTreeNode component, int usedHookCount)
        {
            if (!StoreHookStore.TryGetValue(component, out var hooks))
                return;

            foreach (var index in new List<int>(hooks.Keys))
            {
                if (index < usedHookCount)
                    continue;

                hooks[index].Dispose();
                hooks.Remove(index);
            }

            if (hooks.Count == 0)
                StoreHookStore.Remove(component);
        }

        private static void DisposeStoreHooks(RuntimeTreeIdentity.RuntimeTreeNode component)
        {
            if (!StoreHookStore.TryGetValue(component, out var hooks))
                return;

            foreach (var hook in hooks.Values)
                hook.Dispose();

            StoreHookStore.Remove(component);
        }

        public abstract void Dispose();

        private sealed class StateHookState<T> : StateStore.StateSlot<T>
        {
            private ComponentBase<TElement, TAnimation> _owner;
            private string _componentId;

            public StateHookState(
                T value,
                ComponentBase<TElement, TAnimation> owner,
                string componentId) : base(value)
            {
                _owner = owner;
                _componentId = componentId;
                Setter = SetState;
            }

            public Action<Func<T, T>> Setter { get; }

            public void UpdateOwner(ComponentBase<TElement, TAnimation> owner, string componentId)
            {
                if (!ReferenceEquals(_owner, owner))
                    _owner = owner;
                if (!ReferenceEquals(_componentId, componentId))
                    _componentId = componentId;
            }

            private void SetState(Func<T, T> update)
            {
                var currentState = Value;
                var newValue = update(currentState);
                if (EqualityComparer<T>.Default.Equals(currentState, newValue))
                    return;

                Value = newValue;
                _owner.Id = _componentId;
                _owner.OnStateChanged();
            }
        }

        private abstract class MemoHookState
        {
        }

        private sealed class MemoHookState<T> : MemoHookState
        {
            public MemoHookState(T value, object?[] dependencies)
            {
                Value = value;
                Dependencies = dependencies;
            }

            public T Value { get; }

            public object?[] Dependencies { get; }
        }

        private sealed class EffectHookState
        {
            public EffectHookState(Func<Action?> effect, object?[]? dependencies)
            {
                Effect = effect;
                Dependencies = dependencies;
            }

            public Func<Action?> Effect { get; }

            public object?[]? Dependencies { get; }

            public Action? Cleanup { get; set; }
        }
    }

    public sealed class Ref<T>
    {
        public Ref(T current)
        {
            Current = current;
        }

        public T Current { get; set; }
    }
}
