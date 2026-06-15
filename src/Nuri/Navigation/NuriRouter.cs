using System;
using System.Collections.Generic;
using Nuri.UI.Dsl;

namespace Nuri.Navigation
{
    public sealed class NuriRouter : IRouter
    {
        private readonly Stack<RouteState> _backStack = new Stack<RouteState>();
        private RouteState _current;

        public NuriRouter(RouteState initialRoute)
        {
            _current = initialRoute ?? throw new ArgumentNullException(nameof(initialRoute));
        }

        public event EventHandler? Changed;

        public RouteState Current => _current;

        public bool CanGoBack => _backStack.Count > 0;

        public static NuriRouter Create<TComponent>() where TComponent : Component, new()
        {
            return new NuriRouter(CreateRoute<TComponent>(null));
        }

        public void Navigate<TComponent>() where TComponent : Component, new()
        {
            Navigate<TComponent>(null);
        }

        public void Navigate<TComponent>(object? parameter) where TComponent : Component, new()
        {
            Navigate(CreateRoute<TComponent>(parameter), pushCurrent: true);
        }

        public void Navigate(string key, Func<IElement> factory, object? parameter = null)
        {
            Navigate(new RouteState(key, factory, parameter), pushCurrent: true);
        }

        public void Replace<TComponent>() where TComponent : Component, new()
        {
            Replace<TComponent>(null);
        }

        public void Replace<TComponent>(object? parameter) where TComponent : Component, new()
        {
            Navigate(CreateRoute<TComponent>(parameter), pushCurrent: false);
        }

        public void Back()
        {
            if (!CanGoBack)
                return;

            _current = _backStack.Pop();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void Navigate(RouteState route, bool pushCurrent)
        {
            if (pushCurrent)
                _backStack.Push(_current);

            _current = route;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static RouteState CreateRoute<TComponent>(object? parameter) where TComponent : Component, new()
        {
            return new RouteState(typeof(TComponent).FullName ?? typeof(TComponent).Name, () => new TComponent(), parameter);
        }
    }
}
