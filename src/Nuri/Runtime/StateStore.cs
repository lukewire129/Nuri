using System;
using System.Collections.Generic;

namespace Nuri.Runtime
{
    public sealed class StateStore
    {
        private readonly Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, object?>> _store =
            new Dictionary<RuntimeTreeIdentity.RuntimeTreeNode, Dictionary<int, object?>>();

        public T GetOrCreateState<T>(string componentId, int index, T initialValue)
            => GetOrCreateState(RuntimeTreeIdentity.GetNode(componentId), index, initialValue);

        internal T GetOrCreateState<T>(RuntimeTreeIdentity.RuntimeTreeNode component, int index, T initialValue)
        {
            if (!_store.TryGetValue(component, out var states))
            {
                states = new Dictionary<int, object?>();
                _store[component] = states;
            }

            if (!states.TryGetValue(index, out var value))
            {
                states[index] = initialValue;
                return initialValue;
            }

            if (value is StateSlot<T> slot)
                return slot.Value;

            return (T)value!;
        }

        internal Dictionary<int, object?> GetOrCreateComponentStateEntries(RuntimeTreeIdentity.RuntimeTreeNode component)
        {
            if (!_store.TryGetValue(component, out var states))
            {
                states = new Dictionary<int, object?>();
                _store[component] = states;
            }

            return states;
        }

        public void UpdateState<T>(string componentId, int index, T newValue)
            => UpdateState(RuntimeTreeIdentity.GetNode(componentId), index, newValue);

        internal void UpdateState<T>(RuntimeTreeIdentity.RuntimeTreeNode component, int index, T newValue)
        {
            if (!_store.TryGetValue(component, out var states))
                throw new InvalidOperationException($"Component {component.Id} not found.");

            if (states.TryGetValue(index, out var existingValue) && existingValue is StateSlot<T> slot)
            {
                slot.Value = newValue;
                return;
            }

            if (existingValue != null && !(existingValue is T))
                throw new InvalidOperationException($"Cannot change state type at index {index} from {existingValue.GetType()} to {typeof(T)}.");

            states[index] = newValue;
        }

        public void RemoveComponentState(string componentId)
            => RemoveComponentState(RuntimeTreeIdentity.GetNode(componentId));

        internal void RemoveComponentState(RuntimeTreeIdentity.RuntimeTreeNode component)
        {
            _store.Remove(component);
        }

        public void TrimComponentState(string componentId, int usedHookCount)
            => TrimComponentState(RuntimeTreeIdentity.GetNode(componentId), usedHookCount);

        internal void TrimComponentState(RuntimeTreeIdentity.RuntimeTreeNode component, int usedHookCount)
        {
            if (!_store.TryGetValue(component, out var states))
                return;

            foreach (var index in new List<int>(states.Keys))
            {
                if (index >= usedHookCount)
                    states.Remove(index);
            }

            if (states.Count == 0)
                _store.Remove(component);
        }

        public void Clear()
        {
            _store.Clear();
        }

        internal class StateSlot<T>
        {
            public StateSlot(T value)
            {
                Value = value;
            }

            public T Value { get; set; }

        }
    }
}
