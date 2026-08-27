using System;
using Nuri.UI.Navigation;

namespace Nuri.UI.Dsl
{
    public abstract partial class Component
    {
        /// <summary>
        /// Defines a route with a key and a render function that produces the route content.
        /// </summary>
        /// <param name="key">The unique route key.</param>
        /// <param name="render">Produces the route content element.</param>
        /// <returns>A new route definition.</returns>
        public static RouteDefinition Route(string key, Func<IElement> render)
        {
            return new RouteDefinition(key, render);
        }

        /// <summary>
        /// Creates a router driven by an explicit current route key and the available routes.
        /// </summary>
        /// <param name="currentRoute">The active route key.</param>
        /// <param name="routes">The route definitions.</param>
        /// <returns>A new router.</returns>
        public static Router Router(string currentRoute, params RouteDefinition[] routes)
        {
            return new Router(currentRoute, routes);
        }

        /// <summary>
        /// Creates a router driven by a <see cref="NavigationState"/> and the available routes.
        /// </summary>
        /// <param name="navigationState">The navigation state owned by <c>useNavigation</c>.</param>
        /// <param name="routes">The route definitions.</param>
        /// <returns>A new router.</returns>
        public static Router Router(NavigationState navigationState, params RouteDefinition[] routes)
        {
            return new Router(navigationState, routes);
        }

        /// <summary>
        /// Creates a router with an explicit current route key, a not-found fallback, and the available routes.
        /// </summary>
        /// <param name="currentRoute">The active route key.</param>
        /// <param name="notFound">Produces the content shown when no route matches.</param>
        /// <param name="routes">The route definitions.</param>
        /// <returns>A new router.</returns>
        public static Router Router(string currentRoute, Func<IElement> notFound, params RouteDefinition[] routes)
        {
            return new Router(currentRoute, notFound, routes);
        }

        /// <summary>
        /// Creates a router driven by a <see cref="NavigationState"/>, with a not-found fallback and the available routes.
        /// </summary>
        /// <param name="navigationState">The navigation state owned by <c>useNavigation</c>.</param>
        /// <param name="notFound">Produces the content shown when no route matches.</param>
        /// <param name="routes">The route definitions.</param>
        /// <returns>A new router.</returns>
        public static Router Router(NavigationState navigationState, Func<IElement> notFound, params RouteDefinition[] routes)
        {
            return new Router(navigationState, notFound, routes);
        }

    }
}
