using DeltaUI.Core.UI;
using DeltaUI.Core.UI.Values;
using System;
using System.Diagnostics;
using System.Windows;

namespace DeltaUI.WPF
{
    public abstract partial class Component : ComponentBase<IElement, AnimationValue>, IElement
    {
        public Component()
        {
            WeakEventManager<ApplicationRoot, EventArgs>
                .AddHandler (ApplicationRoot.Instance, nameof (ApplicationRoot.StateIndexInitialize), OnEvent);
        }

        public abstract IElement Render();

        protected override void OnStateChanged()
        {
            ApplicationRoot.Instance.RequestRebuild(this);
        }

        private void OnEvent(object? sender, EventArgs e)
        {
            ResetStateIndex();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Debug.WriteLine ("Component disposing...");
                // 등록된 모든 클린업 호출
                foreach (var cleanup in _cleanupEffects)
                {
                    cleanup?.Invoke ();
                }
                _cleanupEffects.Clear ();
            }
        }

        public override void Dispose()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        public override bool Equals(object? obj)
        {
            return obj is Component other && GetType () == other.GetType ();
        }

        public override int GetHashCode()
        {
            return GetType ().GetHashCode ();
        }
    }
}
