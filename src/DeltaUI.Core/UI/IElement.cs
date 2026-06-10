using System;
using System.Collections.Generic;
using DeltaUI.Core.UI.Events;

namespace DeltaUI.Core.UI
{
    public interface IElement<TElement, TAnimation>
    {
        string ParentId { get; set; }

        string Id { get; set; }

        string Type { get; set; }

        string Kind { get; set; }

        string Name { get; set; }

        string Key { get; set; }

        List<TElement> Children { get; set; }

        Dictionary<string, object> Properties { get; set; }

        Dictionary<string, Delegate> Events { get; set; }

        Dictionary<string, VirtualEvent> VirtualEvents { get; set; }

        Dictionary<string, TAnimation> Animations { get; set; }

        bool TryGetValue(string propertyName, out object value);

        void LoadNodeNumber(string parentId, int myId);

        TElement SetProperty(string name, object value);

        TElement AddEvent(string eventName, Delegate handler);

        TElement AddVirtualEvent(string eventName, VirtualEvent handler);

        TElement AddAnimation(string animationName, TAnimation animation);
    }
}
