using Nuri.UI.Dsl;

namespace Nuri.UI.Navigation
{
    internal sealed class RouteHost : Component
    {
        private readonly RouteDefinition _route;

        public RouteHost(RouteDefinition route)
        {
            _route = route;
        }

        public override IElement Render()
        {
            return _route.Render();
        }
    }
}
