using System;
using Nuri.Constants;
using Nuri.UI.Controls;

namespace Nuri.UI.Dsl;

/// <summary>
/// A renderer-owned native control hosted as an opaque Nuri visual leaf.
/// </summary>
public sealed class NativeElement : Visual
{
    internal NativeElement(NativeControlDescriptor descriptor)
        : base(VirtualControlTypes.Native)
    {
        SetProperty(PropertyKeys.NativeControl, descriptor ?? throw new ArgumentNullException(nameof(descriptor)));
    }
}
