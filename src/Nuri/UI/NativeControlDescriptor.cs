using System;

namespace Nuri.UI;

/// <summary>
/// Describes a renderer-owned native control without introducing a native framework dependency into Core.
/// </summary>
public sealed class NativeControlDescriptor
{
    public NativeControlDescriptor(
        Type nativeType,
        Func<object> create,
        Func<object, Action?>? mount,
        Action<object> render)
    {
        NativeType = nativeType ?? throw new ArgumentNullException(nameof(nativeType));
        Create = create ?? throw new ArgumentNullException(nameof(create));
        Mount = mount;
        Render = render ?? throw new ArgumentNullException(nameof(render));
    }

    public Type NativeType { get; }

    public Func<object> Create { get; }

    public Func<object, Action?>? Mount { get; }

    public Action<object> Render { get; }
}
