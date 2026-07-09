using System;
using System.Collections.Generic;
using System.Threading;
using Nuri.Runtime.Diagnostics;

namespace Nuri.Runtime
{
    public sealed class Store<T>
    {
        private static int _nextStoreIndex;
        private readonly object _syncRoot = new object();
        private readonly Dictionary<StoreSubscriptionKey, IStoreSubscription<T>> _subscriptions = new Dictionary<StoreSubscriptionKey, IStoreSubscription<T>>();
        private readonly string _storeId;
        private T _value;

        public Store(T initialValue)
        {
            _value = initialValue;
            _storeId = typeof(T).FullName + "#" + Interlocked.Increment(ref _nextStoreIndex).ToString();
        }

        public T Value
        {
            get
            {
                lock (_syncRoot)
                {
                    return _value;
                }
            }
        }

        public void Set(T next)
        {
            IStoreSubscription<T>[] subscriptions;

            lock (_syncRoot)
            {
                if (EqualityComparer<T>.Default.Equals(_value, next))
                    return;

                _value = next;
                subscriptions = new IStoreSubscription<T>[_subscriptions.Count];
                _subscriptions.Values.CopyTo(subscriptions, 0);
            }

            if (NuriDiagnostics.IsEnabled)
                NuriDiagnostics.RecordStoreSet(EnsureDiagnosticsStoreRegistered(next), DiagnosticsValueFormatter.Summary(next));

            var invalidatedComponentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var subscription in subscriptions)
            {
                if (!subscription.ShouldInvalidate(next))
                    continue;

                if (invalidatedComponentIds.Add(subscription.ComponentId))
                    subscription.Invalidate();
            }
        }

        internal IStoreSubscription SubscribeComponent<TResult>(
            string componentId,
            int hookIndex,
            Func<T, TResult> selector,
            Action invalidate)
        {
            if (string.IsNullOrWhiteSpace(componentId))
                throw new ArgumentException("Component id is required.", nameof(componentId));

            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            if (invalidate == null)
                throw new ArgumentNullException(nameof(invalidate));

            lock (_syncRoot)
            {
                var key = new StoreSubscriptionKey(componentId, hookIndex);
                var selectedValue = selector(_value);
                var storeId = EnsureDiagnosticsStoreRegistered(_value);

                if (_subscriptions.TryGetValue(key, out var existing)
                    && existing is StoreSubscription<TResult> typedExisting)
                {
                    typedExisting.Update(selector, selectedValue, invalidate);
                    RecordDiagnosticsSubscription(storeId, componentId, hookIndex, selectedValue);
                    return typedExisting;
                }

                existing?.Dispose();
                var subscription = new StoreSubscription<TResult>(this, storeId, key, selector, selectedValue, invalidate);
                _subscriptions[key] = subscription;
                RecordDiagnosticsSubscription(storeId, componentId, hookIndex, selectedValue);
                return subscription;
            }
        }

        private void Remove(IStoreSubscription<T> subscription)
        {
            lock (_syncRoot)
            {
                if (_subscriptions.TryGetValue(subscription.Key, out var existing)
                    && ReferenceEquals(existing, subscription))
                    _subscriptions.Remove(subscription.Key);
            }
        }

        private string EnsureDiagnosticsStoreRegistered(T value)
        {
            if (!NuriDiagnostics.IsEnabled)
                return _storeId;

            NuriDiagnostics.RegisterStoreInstance(_storeId, typeof(T).Name, DiagnosticsValueFormatter.Summary(value));
            return _storeId;
        }

        private static void RecordDiagnosticsSubscription<TResult>(string storeId, string componentId, int hookIndex, TResult selectedValue)
        {
            if (!NuriDiagnostics.IsEnabled)
                return;

            NuriDiagnostics.RecordStoreSubscription(
                storeId,
                typeof(T).Name,
                componentId,
                hookIndex,
                DiagnosticsValueFormatter.TypeName(typeof(TResult)),
                DiagnosticsValueFormatter.Summary(selectedValue));
        }

        private sealed class StoreSubscription<TResult> : IStoreSubscription<T>
        {
            private readonly Store<T> _store;
            private readonly string _storeId;
            private Func<T, TResult> _selector;
            private TResult _lastSelectedValue;
            private Action? _invalidate;

            public StoreSubscription(
                Store<T> store,
                string storeId,
                StoreSubscriptionKey key,
                Func<T, TResult> selector,
                TResult lastSelectedValue,
                Action invalidate)
            {
                _store = store;
                _storeId = storeId;
                Key = key;
                _selector = selector;
                _lastSelectedValue = lastSelectedValue;
                _invalidate = invalidate;
            }

            public StoreSubscriptionKey Key { get; }

            public string ComponentId => Key.ComponentId;

            public object Store => _store;

            public void Update(Func<T, TResult> selector, TResult lastSelectedValue, Action invalidate)
            {
                _selector = selector;
                _lastSelectedValue = lastSelectedValue;
                _invalidate = invalidate;
            }

            public bool ShouldInvalidate(T nextValue)
            {
                var nextSelectedValue = _selector(nextValue);
                if (EqualityComparer<TResult>.Default.Equals(_lastSelectedValue, nextSelectedValue))
                    return false;

                _lastSelectedValue = nextSelectedValue;
                RecordDiagnosticsSubscription(_storeId, ComponentId, Key.HookIndex, nextSelectedValue);
                return true;
            }

            public void Invalidate()
            {
                _invalidate?.Invoke();
            }

            public void Dispose()
            {
                _invalidate = null;
                NuriDiagnostics.RemoveStoreSubscription(_storeId, ComponentId, Key.HookIndex);
                _store.Remove(this);
            }
        }
    }

    internal interface IStoreSubscription : IDisposable
    {
        string ComponentId { get; }

        object Store { get; }
    }

    internal interface IStoreSubscription<T> : IStoreSubscription
    {
        StoreSubscriptionKey Key { get; }

        bool ShouldInvalidate(T nextValue);

        void Invalidate();
    }

    internal sealed class StoreSubscriptionKey : IEquatable<StoreSubscriptionKey>
    {
        public StoreSubscriptionKey(string componentId, int hookIndex)
        {
            ComponentId = componentId;
            HookIndex = hookIndex;
        }

        public string ComponentId { get; }

        public int HookIndex { get; }

        public bool Equals(StoreSubscriptionKey? other)
        {
            return other != null
                && string.Equals(ComponentId, other.ComponentId, StringComparison.Ordinal)
                && HookIndex == other.HookIndex;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as StoreSubscriptionKey);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ComponentId != null ? StringComparer.Ordinal.GetHashCode(ComponentId) : 0) * 397) ^ HookIndex;
            }
        }
    }
}
