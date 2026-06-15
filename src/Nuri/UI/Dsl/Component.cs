using System;
using Nuri.Hosting;
using Nuri.Navigation;
using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    public abstract partial class Component : ComponentBase<IElement, AnimationValue>, IElement
    {
        private bool _entered;
        private bool _disposed;

        public static event EventHandler<Component>? AnyStateChanged;

        public event EventHandler? StateChanged;

        public abstract IElement Render();

        public void Enter()
        {
            if (_entered || _disposed)
                return;

            _entered = true;
            OnEnter();
        }

        public void Leave()
        {
            if (!_entered)
                return;

            OnLeave();
            _entered = false;
        }

        protected IRouter useRouter()
        {
            return useService<IRouter>();
        }

        protected IWindowService useWindows()
        {
            return useService<IWindowService>();
        }

        protected IDialogContext useDialog()
        {
            return useService<IDialogContext>();
        }

        protected T? useRouteParameter<T>()
        {
            var parameter = useRouter().Current.Parameter;
            if (parameter is null)
                return default;

            return parameter is T typedParameter ? typedParameter : default;
        }

        protected virtual void OnEnter()
        {
        }

        protected virtual void OnLeave()
        {
        }

        protected virtual void OnDispose()
        {
        }

        protected override void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
            AnyStateChanged?.Invoke(this, this);
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            Leave();
            OnDispose();
            _disposed = true;
        }
    }
}
