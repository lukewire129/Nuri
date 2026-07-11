using System;
using System.Collections.Generic;

namespace Nuri.UI.Navigation
{
    public sealed class Navigator
    {
        private readonly NavigationState _state;
        private readonly Action<Func<NavigationState, NavigationState>> _setState;

        public Navigator(NavigationState state, Action<NavigationState> setState)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            if (setState == null)
                throw new ArgumentNullException(nameof(setState));

            _setState = update => setState(update(_state));
        }

        public Navigator(NavigationState state, Action<Func<NavigationState, NavigationState>> setState)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _setState = setState ?? throw new ArgumentNullException(nameof(setState));
        }

        public string CurrentRoute => _state.CurrentRoute;

        public bool CanGoBack => _state.CanGoBack;

        public void Navigate(string route)
        {
            _setState(current =>
            {
                if (string.Equals(route, current.CurrentRoute, StringComparison.OrdinalIgnoreCase))
                    return current;

                var backStack = new List<string>(current.BackStack) { current.CurrentRoute };
                return new NavigationState(route, backStack);
            });
        }

        public void Replace(string route)
        {
            _setState(current =>
            {
                if (string.Equals(route, current.CurrentRoute, StringComparison.OrdinalIgnoreCase))
                    return current;

                return new NavigationState(route, current.BackStack);
            });
        }

        public void GoBack()
        {
            _setState(current =>
            {
                if (current.BackStack.Count == 0)
                    return current;

                var backStack = new List<string>(current.BackStack);
                var previousRoute = backStack[backStack.Count - 1];
                backStack.RemoveAt(backStack.Count - 1);
                return new NavigationState(previousRoute, backStack);
            });
        }
    }
}
