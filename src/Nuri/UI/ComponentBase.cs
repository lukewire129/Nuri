using System;
using System.Collections.Generic;
using Nuri.Runtime;
using Nuri.UI.Events;

namespace Nuri.UI
{
    public abstract class ComponentBase<TElement, TAnimation> : IElement<TElement, TAnimation>, IDisposable
    {
        private static readonly StateStore StateStore = new StateStore();
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

        protected void ResetStateIndex()
        {
            _stateIndex = 0;
        }

        public void ResetStateIndexForRender()
        {
            ResetStateIndex();
        }

        protected virtual void OnStateChanged()
        {
        }

        public abstract void Dispose();
    }
}
