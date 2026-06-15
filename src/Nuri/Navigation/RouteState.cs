using System;
using Nuri.UI.Dsl;

namespace Nuri.Navigation
{
    public sealed class RouteState
    {
        public RouteState(string key, Func<IElement> factory, object? parameter)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Parameter = parameter;
        }

        public string Key { get; }

        public Func<IElement> Factory { get; }

        public object? Parameter { get; }

        public IElement CreateElement()
        {
            return Factory();
        }
    }
}
