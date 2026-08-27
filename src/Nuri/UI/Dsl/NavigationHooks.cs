using Nuri.UI.Navigation;

namespace Nuri.UI.Dsl
{
    public abstract partial class Component
    {
        /// <summary>
        /// Owns local route state and returns the current <see cref="NavigationState"/> with a <see cref="Navigator"/> used to change routes. Call in the same order on every render and never conditionally.
        /// </summary>
        /// <param name="initialRoute">The initial route key.</param>
        /// <returns>A tuple of the navigation state and a navigator.</returns>
        protected (NavigationState state, Navigator navigator) useNavigation(string initialRoute)
        {
            var (state, setState) = useState(new NavigationState(initialRoute));
            return (state, new Navigator(state, setState));
        }
    }
}
