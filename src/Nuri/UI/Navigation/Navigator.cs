using System;
using System.Collections.Generic;

namespace Nuri.UI.Navigation
{
    public sealed class Navigator
    {
        private readonly NavigationState _state;
        private readonly Action<NavigationState> _setState;

        public Navigator(NavigationState state, Action<NavigationState> setState)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _setState = setState ?? throw new ArgumentNullException(nameof(setState));
        }

        public string CurrentRoute => _state.CurrentRoute;

        public bool CanGoBack => _state.CanGoBack;

        public void Navigate(string route)
        {
            if (string.Equals(route, _state.CurrentRoute, StringComparison.OrdinalIgnoreCase))
                return;

            var backStack = new List<string>(_state.BackStack) { _state.CurrentRoute };
            _setState(new NavigationState(route, backStack));
        }

        public void Replace(string route)
        {
            if (string.Equals(route, _state.CurrentRoute, StringComparison.OrdinalIgnoreCase))
                return;

            _setState(new NavigationState(route, _state.BackStack));
        }

        public void GoBack()
        {
            if (_state.BackStack.Count == 0)
                return;

            var backStack = new List<string>(_state.BackStack);
            var previousRoute = backStack[backStack.Count - 1];
            backStack.RemoveAt(backStack.Count - 1);
            _setState(new NavigationState(previousRoute, backStack));
        }
    }
}
