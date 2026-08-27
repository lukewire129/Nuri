using System;
using System.Collections.Generic;

namespace Nuri.UI.Navigation
{
    /// <summary>
    /// Immutable snapshot of the current route and the back-stack history, produced and owned by <c>useNavigation</c>.
    /// </summary>
    public sealed class NavigationState
    {
        private readonly IReadOnlyList<string> _backStack;

        /// <summary>
        /// Creates a navigation state with no back-stack.
        /// </summary>
        /// <param name="currentRoute">The active route key.</param>
        public NavigationState(string currentRoute)
            : this(currentRoute, Array.Empty<string>())
        {
        }

        /// <summary>
        /// Creates a navigation state with a back-stack.
        /// </summary>
        /// <param name="currentRoute">The active route key.</param>
        /// <param name="backStack">The ordered history of previously visited routes.</param>
        public NavigationState(string currentRoute, IReadOnlyList<string> backStack)
        {
            CurrentRoute = currentRoute ?? string.Empty;
            _backStack = backStack ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets the active route key.
        /// </summary>
        public string CurrentRoute { get; }

        /// <summary>
        /// Gets the ordered history of previously visited routes.
        /// </summary>
        public IReadOnlyList<string> BackStack => _backStack;

        /// <summary>
        /// Gets a value indicating whether there is at least one route to navigate back to.
        /// </summary>
        public bool CanGoBack => _backStack.Count > 0;
    }
}
