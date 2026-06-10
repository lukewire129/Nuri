using System;
using System.Collections.Generic;
using DeltaUI.Core.VirtualDom;
using DeltaUI.Core.VirtualDom.Rendering;

namespace DeltaUI.Avalonia
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
}
