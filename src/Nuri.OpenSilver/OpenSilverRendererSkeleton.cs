using System;
using System.Collections.Generic;
using Nuri.Hosting;
using Nuri.Runtime;
using Nuri.UI.Dsl;
using Nuri.VirtualDom;
using Nuri.VirtualDom.Rendering;

namespace Nuri.OpenSilver
{
    public sealed class OpenSilverRendererSkeleton : IVirtualEntryRenderer<object>
    {
        public object Build(VirtualEntry entry)
        {
            throw new NotImplementedException("Replace object with the OpenSilver native element type and map VirtualEntry types to OpenSilver controls.");
        }

        public void ApplyDiff(object root, IReadOnlyList<PatchOperation> operations)
        {
            throw new NotImplementedException("Apply Core patch operations to the OpenSilver native control tree.");
        }
    }

    public sealed class OpenSilverControlRegistrySkeleton : IControlRegistry<object>
    {
        public object Create(string type)
        {
            throw new NotImplementedException($"Register an OpenSilver factory for virtual control type '{type}'.");
        }

        public void AddChild(object parent, object child, int? index = null)
        {
            throw new NotImplementedException("Map child insertion to OpenSilver content/panel APIs.");
        }

        public void RemoveChild(object parent, object child)
        {
            throw new NotImplementedException("Map child removal to OpenSilver content/panel APIs.");
        }

        public void MoveChild(object parent, object child, int newIndex)
        {
            throw new NotImplementedException("Map keyed child reorder to OpenSilver panel/item APIs.");
        }

        public void ReplaceChild(object parent, object oldChild, object newChild)
        {
            throw new NotImplementedException("Map child replacement to OpenSilver content/panel APIs.");
        }
    }

    public sealed class OpenSilverHostAdapterSkeleton : INuriHostAdapter<object, object>
    {
        public NuriMountedRoot<object> Attach(object host, IElement rootElement, NuriServiceProvider? services = null)
        {
            throw new NotImplementedException("Replace object with the OpenSilver native host type, build the native root, and assign it to the host without taking over OpenSilver startup.");
        }
    }
}
