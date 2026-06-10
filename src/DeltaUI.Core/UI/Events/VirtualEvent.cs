using System;

namespace DeltaUI.Core.UI.Events
{
    public enum VirtualEventKind
    {
        Click,
        TextChanged,
        ContentChanged,
        CheckChanged
    }

    public sealed class VirtualEvent
    {
        public VirtualEvent(VirtualEventKind kind, Delegate handler)
        {
            Kind = kind;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public VirtualEventKind Kind { get; }

        public Delegate Handler { get; }
    }
}
