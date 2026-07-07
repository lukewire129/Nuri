using System;
using System.Collections.Generic;

namespace Nuri.UI.Navigation
{
    public sealed class NavigationState
    {
        private readonly IReadOnlyList<string> _backStack;

        public NavigationState(string currentRoute)
            : this(currentRoute, Array.Empty<string>())
        {
        }

        public NavigationState(string currentRoute, IReadOnlyList<string> backStack)
        {
            CurrentRoute = currentRoute ?? string.Empty;
            _backStack = backStack ?? Array.Empty<string>();
        }

        public string CurrentRoute { get; }

        public IReadOnlyList<string> BackStack => _backStack;

        public bool CanGoBack => _backStack.Count > 0;
    }
}
