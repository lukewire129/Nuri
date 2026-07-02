using System;
using System.Collections.Generic;
using Nuri.Runtime;
using Nuri.UI.Events;

namespace Nuri.UI
{
    public abstract class ComponentBase<TElement, TAnimation> : IElement<TElement, TAnimation>, IDisposable
    {
        private static readonly StateStore StateStore = new StateStore();
        private static readonly object HookSyncRoot = new object();
        private static readonly Dictionary<string, Dictionary<int, MemoHookState>> MemoStore = new Dictionary<string, Dictionary<int, MemoHookState>>();
        private static readonly Dictionary<string, Dictionary<int, EffectHookState>> EffectStore = new Dictionary<string, Dictionary<int, EffectHookState>>();
        private static readonly Dictionary<string, HashSet<int>> PendingEffects = new Dictionary<string, HashSet<int>>();
        private int _stateIndex;

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
            Id = $"{parentId}_{myId}";
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

        protected (T state, Action<T> setState) useState<T>(T initialValue)
        {
            var index = _stateIndex;
            var state = StateStore.GetOrCreateState(Id, index, initialValue);

            void SetState(T newValue)
            {
                var currentState = StateStore.GetOrCreateState(Id, index, initialValue);
                if (EqualityComparer<T>.Default.Equals(currentState, newValue))
                    return;

                StateStore.UpdateState(Id, index, newValue);
                OnStateChanged();
            }

            _stateIndex++;
            return (state, SetState);
        }

        protected (TState state, Action<TAction> dispatch) useReducer<TState, TAction>(Func<TState, TAction, TState> reducer, TState initialState)
        {
            if (reducer == null)
                throw new ArgumentNullException(nameof(reducer));

            var index = _stateIndex;
            var state = StateStore.GetOrCreateState(Id, index, initialState);

            void Dispatch(TAction action)
            {
                var currentState = StateStore.GetOrCreateState(Id, index, initialState);
                var newState = reducer(currentState, action);
                if (EqualityComparer<TState>.Default.Equals(currentState, newState))
                    return;

                StateStore.UpdateState(Id, index, newState);
                OnStateChanged();
            }

            _stateIndex++;
            return (state, Dispatch);
        }

        protected Ref<T> useRef<T>(T initialValue)
        {
            var index = _stateIndex;
            var reference = StateStore.GetOrCreateState(Id, index, new Ref<T>(initialValue));
            _stateIndex++;
            return reference;
        }

        protected Ref<T> useLatest<T>(T value)
        {
            var reference = useRef(value);
            reference.Current = value;
            return reference;
        }

        protected T useMemo<T>(Func<T> factory, params object?[] dependencies)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var index = _stateIndex;

            lock (HookSyncRoot)
            {
                var hooks = GetOrCreateHooks(MemoStore, Id);
                if (!hooks.TryGetValue(index, out var hook)
                    || hook is not MemoHookState<T> typedHook
                    || DependenciesChanged(typedHook.Dependencies, dependencies))
                {
                    typedHook = new MemoHookState<T>(factory(), CloneDependencies(dependencies) ?? Array.Empty<object?>());
                    hooks[index] = typedHook;
                }

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

            var index = _stateIndex;

            lock (HookSyncRoot)
            {
                var hooks = GetOrCreateHooks(EffectStore, Id);
                if (!hooks.TryGetValue(index, out var hook)
                    || DependenciesChanged(hook.Dependencies, dependencies))
                {
                    hooks[index] = new EffectHookState(effect, CloneDependencies(dependencies))
                    {
                        Cleanup = hook?.Cleanup
                    };
                    GetOrCreatePendingEffects(Id).Add(index);
                }
            }

            _stateIndex++;
        }

        protected void ResetStateIndex()
        {
            _stateIndex = 0;
        }

        public void ResetStateIndexForRender()
        {
            ResetStateIndex();
        }

        protected void CompleteHookRender()
        {
            TrimHookStateForComponent(Id, _stateIndex);
        }

        protected static void FlushPendingEffectsForRender()
        {
            List<(string componentId, int index)> pending;

            lock (HookSyncRoot)
            {
                pending = new List<(string componentId, int index)>();
                foreach (var component in PendingEffects)
                {
                    foreach (var index in component.Value)
                        pending.Add((component.Key, index));
                }

                PendingEffects.Clear();
            }

            foreach (var (componentId, index) in pending)
            {
                Action? cleanup = null;
                Func<Action?>? effectFactory = null;

                lock (HookSyncRoot)
                {
                    if (!EffectStore.TryGetValue(componentId, out var hooks)
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
                    if (EffectStore.TryGetValue(componentId, out var hooks)
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

                foreach (var componentId in new List<string>(EffectStore.Keys))
                {
                    if (!IsInSubtree(componentId, rootComponentId))
                        continue;

                    foreach (var hook in EffectStore[componentId].Values)
                        cleanups.Add(hook.Cleanup);

                    EffectStore.Remove(componentId);
                    PendingEffects.Remove(componentId);
                    MemoStore.Remove(componentId);
                    StateStore.RemoveComponentState(componentId);
                }
            }

            foreach (var cleanup in cleanups)
                cleanup?.Invoke();
        }

        protected static void TrimHookStateForComponent(string componentId, int usedHookCount)
        {
            if (string.IsNullOrWhiteSpace(componentId))
                return;

            List<Action?> cleanups;

            lock (HookSyncRoot)
            {
                cleanups = new List<Action?>();
                StateStore.TrimComponentState(componentId, usedHookCount);
                TrimStateIndexes(MemoStore, componentId, usedHookCount);

                if (EffectStore.TryGetValue(componentId, out var effectHooks))
                {
                    foreach (var index in new List<int>(effectHooks.Keys))
                    {
                        if (index < usedHookCount)
                            continue;

                        cleanups.Add(effectHooks[index].Cleanup);
                        effectHooks.Remove(index);
                    }

                    if (effectHooks.Count == 0)
                        EffectStore.Remove(componentId);
                }

                if (PendingEffects.TryGetValue(componentId, out var pendingIndexes))
                {
                    pendingIndexes.RemoveWhere(index => index >= usedHookCount);
                    if (pendingIndexes.Count == 0)
                        PendingEffects.Remove(componentId);
                }
            }

            foreach (var cleanup in cleanups)
                cleanup?.Invoke();
        }

        protected virtual void OnStateChanged()
        {
        }

        private static Dictionary<int, THook> GetOrCreateHooks<THook>(Dictionary<string, Dictionary<int, THook>> store, string componentId)
        {
            if (!store.TryGetValue(componentId, out var hooks))
            {
                hooks = new Dictionary<int, THook>();
                store[componentId] = hooks;
            }

            return hooks;
        }

        private static HashSet<int> GetOrCreatePendingEffects(string componentId)
        {
            if (!PendingEffects.TryGetValue(componentId, out var indexes))
            {
                indexes = new HashSet<int>();
                PendingEffects[componentId] = indexes;
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

        private static bool IsInSubtree(string componentId, string rootComponentId)
        {
            return string.Equals(componentId, rootComponentId, StringComparison.Ordinal)
                || componentId.StartsWith(rootComponentId + "_", StringComparison.Ordinal);
        }

        private static void TrimStateIndexes<T>(Dictionary<string, Dictionary<int, T>> store, string componentId, int usedHookCount)
        {
            if (!store.TryGetValue(componentId, out var hooks))
                return;

            foreach (var index in new List<int>(hooks.Keys))
            {
                if (index >= usedHookCount)
                    hooks.Remove(index);
            }

            if (hooks.Count == 0)
                store.Remove(componentId);
        }

        public abstract void Dispose();

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
