using System;
using System.Collections.Generic;

namespace Nuri.UI.Navigation
{
    /// <summary>
    /// Navigates between routes by pushing, replacing, or popping navigation state. Returned by <c>useNavigation</c>.
    /// </summary>
    public sealed class Navigator
    {
        private readonly NavigationState _state;
        private readonly Action<Func<NavigationState, NavigationState>> _setState;

        /// <summary>
        /// Creates a navigator backed by a state setter.
        /// </summary>
        /// <param name="state">The current navigation state.</param>
        /// <param name="setState">State setter invoked with the next navigation state.</param>
        public Navigator(NavigationState state, Action<NavigationState> setState)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            if (setState == null)
                throw new ArgumentNullException(nameof(setState));

            _setState = update => setState(update(_state));
        }

        /// <summary>
        /// Creates a navigator backed by a functional state updater.
        /// </summary>
        /// <param name="state">The current navigation state.</param>
        /// <param name="setState">Updater invoked with the current state, returning the next state.</param>
        public Navigator(NavigationState state, Action<Func<NavigationState, NavigationState>> setState)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _setState = setState ?? throw new ArgumentNullException(nameof(setState));
        }

        /// <summary>
        /// Gets the active route key.
        /// </summary>
        public string CurrentRoute => _state.CurrentRoute;

        /// <summary>
        /// Gets a value indicating whether there is at least one route to navigate back to.
        /// </summary>
        public bool CanGoBack => _state.CanGoBack;

        /// <summary>
        /// Navigates to <paramref name="route"/>, pushing the current route onto the back-stack. No-op if the route is already active.
        /// </summary>
        /// <param name="route">The destination route key.</param>
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

        /// <summary>
        /// Replaces the active route with <paramref name="route"/>, preserving the back-stack. No-op if the route is already active.
        /// </summary>
        /// <param name="route">The destination route key.</param>
        public void Replace(string route)
        {
            _setState(current =>
            {
                if (string.Equals(route, current.CurrentRoute, StringComparison.OrdinalIgnoreCase))
                    return current;

                return new NavigationState(route, current.BackStack);
            });
        }

        /// <summary>
        /// Returns to the previous route by popping the back-stack. No-op if there is no history.
        /// </summary>
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
