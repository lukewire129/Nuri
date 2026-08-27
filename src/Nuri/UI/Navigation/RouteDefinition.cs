using System;
using Nuri.UI.Dsl;

namespace Nuri.UI.Navigation
{
    /// <summary>
    /// Defines a single route by its key and a render function that produces the route content.
    /// </summary>
    public sealed class RouteDefinition
    {
        /// <summary>
        /// Creates a route definition.
        /// </summary>
        /// <param name="key">The unique route key (must not be empty).</param>
        /// <param name="render">Produces the route content element (must not be null).</param>
        public RouteDefinition(string key, Func<IElement> render)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Route key must not be empty.", nameof(key));

            Key = key;
            Render = render ?? throw new ArgumentNullException(nameof(render));
        }

        /// <summary>
        /// Gets the unique route key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the render function that produces the route content.
        /// </summary>
        public Func<IElement> Render { get; }
    }
}
