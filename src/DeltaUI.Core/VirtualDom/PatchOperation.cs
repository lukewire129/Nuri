using System;
using System.Collections.Generic;

namespace DeltaUI.Core.VirtualDom
{
    public enum PatchOperationType
    {
        AddChild,
        RemoveChild,
        MoveChild,
        ReplaceEntry,
        RemoveProperty,
        UpdateProperty,
        RemoveEvent,
        AddEvent,
        RemoveAnimation,
        AddAnimation
    }

    public abstract class PatchOperation
    {
        protected PatchOperation(PatchOperationType type)
        {
            Type = type;
        }

        public PatchOperationType Type { get; }
    }

    public sealed class AddChildPatch : PatchOperation
    {
        public AddChildPatch(VirtualEntry parent, VirtualEntry child, int index) : base(PatchOperationType.AddChild)
        {
            Parent = parent;
            Child = child;
            Index = index;
        }

        public VirtualEntry Parent { get; }

        public VirtualEntry Child { get; }

        public int Index { get; }
    }

    public sealed class RemoveChildPatch : PatchOperation
    {
        public RemoveChildPatch(VirtualEntry parent, VirtualEntry child, int index) : base(PatchOperationType.RemoveChild)
        {
            Parent = parent;
            Child = child;
            Index = index;
        }

        public VirtualEntry Parent { get; }

        public VirtualEntry Child { get; }

        public int Index { get; }
    }

    public sealed class MoveChildPatch : PatchOperation
    {
        public MoveChildPatch(VirtualEntry parent, VirtualEntry child, int oldIndex, int newIndex) : base(PatchOperationType.MoveChild)
        {
            Parent = parent;
            Child = child;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        public VirtualEntry Parent { get; }

        public VirtualEntry Child { get; }

        public int OldIndex { get; }

        public int NewIndex { get; }
    }

    public sealed class ReplaceEntryPatch : PatchOperation
    {
        public ReplaceEntryPatch(VirtualEntry oldEntry, VirtualEntry newEntry) : base(PatchOperationType.ReplaceEntry)
        {
            OldEntry = oldEntry;
            NewEntry = newEntry;
        }

        public VirtualEntry OldEntry { get; }

        public VirtualEntry NewEntry { get; }
    }

    public sealed class UpdatePropertyPatch : PatchOperation
    {
        public UpdatePropertyPatch(VirtualEntry target, string propertyName, object? value) : base(PatchOperationType.UpdateProperty)
        {
            Target = target;
            PropertyName = propertyName;
            Value = value;
        }

        public VirtualEntry Target { get; }

        public string PropertyName { get; }

        public object? Value { get; }
    }

    public sealed class RemovePropertyPatch : PatchOperation
    {
        public RemovePropertyPatch(VirtualEntry target, string propertyName) : base(PatchOperationType.RemoveProperty)
        {
            Target = target;
            PropertyName = propertyName;
        }

        public VirtualEntry Target { get; }

        public string PropertyName { get; }
    }

    public sealed class AddEventPatch : PatchOperation
    {
        public AddEventPatch(VirtualEntry target, KeyValuePair<string, Delegate> @event) : base(PatchOperationType.AddEvent)
        {
            Target = target;
            Event = @event;
        }

        public VirtualEntry Target { get; }

        public KeyValuePair<string, Delegate> Event { get; }
    }

    public sealed class RemoveEventPatch : PatchOperation
    {
        public RemoveEventPatch(VirtualEntry target, KeyValuePair<string, Delegate> @event) : base(PatchOperationType.RemoveEvent)
        {
            Target = target;
            Event = @event;
        }

        public VirtualEntry Target { get; }

        public KeyValuePair<string, Delegate> Event { get; }
    }

    public sealed class AddAnimationPatch : PatchOperation
    {
        public AddAnimationPatch(VirtualEntry target, KeyValuePair<string, object?> animation) : base(PatchOperationType.AddAnimation)
        {
            Target = target;
            Animation = animation;
        }

        public VirtualEntry Target { get; }

        public KeyValuePair<string, object?> Animation { get; }
    }

    public sealed class RemoveAnimationPatch : PatchOperation
    {
        public RemoveAnimationPatch(VirtualEntry target, KeyValuePair<string, object?> animation) : base(PatchOperationType.RemoveAnimation)
        {
            Target = target;
            Animation = animation;
        }

        public VirtualEntry Target { get; }

        public KeyValuePair<string, object?> Animation { get; }
    }
}
