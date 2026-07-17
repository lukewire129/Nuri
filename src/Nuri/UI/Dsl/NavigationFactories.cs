using System;
using Nuri.UI.Navigation;
using Nuri.UI.Values;

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

        public static AnimatedRouter AnimatedRouter(
            NavigationState navigationState,
            TimeSpan duration,
            params RouteDefinition[] routes)
        {
            return new AnimatedRouter(navigationState, duration, null, routes);
        }

        public static AnimatedRouter AnimatedRouter(
            NavigationState navigationState,
            TimeSpan duration,
            EasingValue? easing,
            params RouteDefinition[] routes)
        {
            return new AnimatedRouter(navigationState, duration, easing, routes);
        }
    }
}
