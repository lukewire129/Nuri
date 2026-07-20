using System;

namespace Nuri.Runtime.Diagnostics
{
    public enum DebugKey
    {
        F1 = 1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12
    }

    public interface INuriDebugHost
    {
        bool HasStarted { get; }

        bool IsClosed { get; }

        void SetDebugShortcut(DebugKey key, Action openInspector);

        RuntimeSnapshot CaptureSnapshot();

        void HighlightComponent(string? componentId);
    }
}
