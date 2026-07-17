using System;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;

namespace Nuri.UI.Navigation
{
    public sealed class Router : Component
    {
        private readonly string _currentRoute;
        private readonly RouteDefinition[] _routes;
        private readonly Func<IElement>? _notFound;

        public Router(string currentRoute, params RouteDefinition[] routes)
            : this(currentRoute, null, routes)
        {
        }

        public Router(NavigationState navigationState, params RouteDefinition[] routes)
            : this(navigationState?.CurrentRoute ?? string.Empty, null, routes)
        {
        }

        public Router(NavigationState navigationState, Func<IElement>? notFound, params RouteDefinition[] routes)
            : this(navigationState?.CurrentRoute ?? string.Empty, notFound, routes)
        {
        }

        public Router(string currentRoute, Func<IElement>? notFound, params RouteDefinition[] routes)
        {
            _currentRoute = currentRoute ?? string.Empty;
            _notFound = notFound;
            _routes = routes ?? Array.Empty<RouteDefinition>();
        }

        public override IElement Render()
        {
            foreach (var route in _routes)
            {
                if (string.Equals(route.Key, _currentRoute, StringComparison.OrdinalIgnoreCase))
                    return RenderRoute(route);
            }

            if (_notFound != null)
                return _notFound();

            return Div();
        }

        private static IElement RenderRoute(RouteDefinition route)
        {
            return Component.Div(
                    DivTypes.Block,
                    new RouteHost(route).Key(route.Key));
        }
    }
}
