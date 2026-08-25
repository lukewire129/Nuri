using System;
using System.Runtime.CompilerServices;

namespace Nuri.UI;

/// <summary>
/// Owns native-island mount cleanup independently of any specific renderer type.
/// </summary>
public static class NativeControlLifecycle
{
    private static readonly ConditionalWeakTable<object, NativeControlState> States = new();

    public static void MountAndRender(object nativeControl, NativeControlDescriptor descriptor)
    {
        if (nativeControl == null)
            throw new ArgumentNullException(nameof(nativeControl));
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        var state = States.GetValue(nativeControl, _ => new NativeControlState());
        if (!state.IsMounted)
        {
            state.Cleanup = descriptor.Mount?.Invoke(nativeControl);
            state.IsMounted = true;
        }

        descriptor.Render(nativeControl);
    }

    public static void Render(object nativeControl, NativeControlDescriptor descriptor)
    {
        if (nativeControl == null)
            throw new ArgumentNullException(nameof(nativeControl));
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        descriptor.Render(nativeControl);
    }

    public static void Unmount(object nativeControl)
    {
        if (nativeControl == null || !States.TryGetValue(nativeControl, out var state))
            return;

        States.Remove(nativeControl);
        state.Cleanup?.Invoke();
    }

    private sealed class NativeControlState
    {
        public Action? Cleanup { get; set; }

        public bool IsMounted { get; set; }
    }
}
