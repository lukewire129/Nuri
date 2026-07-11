using System;
using Nuri.UI.Dsl;

namespace Nuri.UI.Navigation
{
    public sealed class RouteDefinition
    {
        public RouteDefinition(string key, Func<IElement> render)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Route key must not be empty.", nameof(key));

            Key = key;
            Render = render ?? throw new ArgumentNullException(nameof(render));
        }

        public string Key { get; }

        public Func<IElement> Render { get; }
    }
}
