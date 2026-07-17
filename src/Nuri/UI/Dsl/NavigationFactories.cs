using System;
using Nuri.UI.Navigation;

namespace Nuri.UI.Dsl
{
    public abstract partial class Component
    {
        public static RouteDefinition Route(string key, Func<IElement> render)
        {
            return new RouteDefinition(key, render);
        }

        public static Router Router(string currentRoute, params RouteDefinition[] routes)
        {
            return new Router(currentRoute, routes);
        }

        public static Router Router(NavigationState navigationState, params RouteDefinition[] routes)
        {
            return new Router(navigationState, routes);
        }

        public static Router Router(string currentRoute, Func<IElement> notFound, params RouteDefinition[] routes)
        {
            return new Router(currentRoute, notFound, routes);
        }

        public static Router Router(NavigationState navigationState, Func<IElement> notFound, params RouteDefinition[] routes)
        {
            return new Router(navigationState, notFound, routes);
        }

    }
}
