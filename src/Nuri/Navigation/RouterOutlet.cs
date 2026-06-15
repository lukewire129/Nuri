using Nuri.UI.Dsl;

namespace Nuri.Navigation
{
    public sealed class RouterOutlet : Component
    {
        private readonly IRouter _router;

        public RouterOutlet(IRouter router)
        {
            _router = router;
            _router.Changed += OnRouterChanged;
        }

        public override IElement Render()
        {
            return _router.Current.CreateElement();
        }

        public override void Dispose()
        {
            _router.Changed -= OnRouterChanged;
        }

        private void OnRouterChanged(object? sender, System.EventArgs e)
        {
            OnStateChanged();
        }
    }
}
