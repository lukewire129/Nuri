using System;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    public abstract partial class Component : ComponentBase<IElement, AnimationValue>, IElement
    {
        public static event EventHandler<Component>? AnyStateChanged;

        public event EventHandler? StateChanged;

        public abstract IElement Render();

        protected override void OnStateChanged()
        {
            NuriDiagnostics.RecordComponentInvalidated(Id, "Component state changed.");
            StateChanged?.Invoke(this, EventArgs.Empty);
            AnyStateChanged?.Invoke(this, this);
        }

        public override void Dispose()
        {
        }

        public static void FlushPendingEffects()
        {
            FlushPendingEffectsForRender();
        }

        public static void DisposeHookState(string rootComponentId)
        {
            DisposeHookStateForSubtree(rootComponentId);
        }

        public void CompleteRenderHooks()
        {
            CompleteHookRender();
        }
    }
}
