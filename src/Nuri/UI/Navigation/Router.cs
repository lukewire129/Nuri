using System;
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
                    return route.Render();
            }

            if (_notFound != null)
                return _notFound();

            return Div();
        }
    }
}
