using System;
using System.Collections.Generic;
using DeltaUI.Core.UI.Events;

namespace DeltaUI.Core.UI
{
    public class Element<TElement, TAnimation> : IElement<TElement, TAnimation>
    {
        public string ParentId { get; set; } = "0";

        public string Id { get; set; } = "0";

        public string Type { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public List<TElement> Children { get; set; } = new List<TElement>();

        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, Delegate> Events { get; set; } = new Dictionary<string, Delegate>();

        public Dictionary<string, VirtualEvent> VirtualEvents { get; set; } = new Dictionary<string, VirtualEvent>();

        public Dictionary<string, TAnimation> Animations { get; set; } = new Dictionary<string, TAnimation>();

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

        public override bool Equals(object? obj)
        {
            return obj is Element<TElement, TAnimation> other && Type == other.Type && Kind == other.Kind;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type?.GetHashCode() ?? 0) * 397) ^ (Kind?.GetHashCode() ?? 0);
            }
        }
    }
}
