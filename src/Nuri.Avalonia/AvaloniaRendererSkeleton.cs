using System;
using System.Collections.Generic;
using Nuri.Hosting;
using Nuri.Runtime;
using Nuri.UI.Dsl;
using Nuri.VirtualDom;
using Nuri.VirtualDom.Rendering;

namespace Nuri.Avalonia
{
    public sealed class AvaloniaRendererSkeleton : IVirtualEntryRenderer<object>
    {
        public object Build(VirtualEntry entry)
        {
            throw new NotImplementedException("Replace object with Avalonia.Controls.Control and map VirtualEntry types to Avalonia controls.");
        }

        public void ApplyDiff(object root, IReadOnlyList<PatchOperation> operations)
        {
            throw new NotImplementedException("Apply Core patch operations to the Avalonia native control tree.");
        }
    }

    public sealed class AvaloniaControlRegistrySkeleton : IControlRegistry<object>
    {
        public object Create(string type)
        {
            throw new NotImplementedException($"Register an Avalonia factory for virtual control type '{type}'.");
        }

        public void AddChild(object parent, object child, int? index = null)
        {
            throw new NotImplementedException("Map child insertion to Avalonia content/panel APIs.");
        }

        public void RemoveChild(object parent, object child)
        {
            throw new NotImplementedException("Map child removal to Avalonia content/panel APIs.");
        }

        public void MoveChild(object parent, object child, int newIndex)
        {
            throw new NotImplementedException("Map keyed child reorder to Avalonia panel/item APIs.");
        }

        public void ReplaceChild(object parent, object oldChild, object newChild)
        {
            throw new NotImplementedException("Map child replacement to Avalonia content/panel APIs.");
        }
    }

    public sealed class AvaloniaHostAdapterSkeleton : INuriHostAdapter<object, object>
    {
        public NuriMountedRoot<object> Attach(object host, IElement rootElement, NuriServiceProvider? services = null)
        {
            throw new NotImplementedException("Replace object with Avalonia.Controls.ContentControl, build the native root with AvaloniaRendererSkeleton, and assign it to host.Content. Keep Avalonia startup owned by the Avalonia app.");
        }
    }
}
