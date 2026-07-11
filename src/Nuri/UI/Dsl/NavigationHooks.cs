using Nuri.UI.Navigation;

namespace Nuri.UI.Dsl
{
    public abstract partial class Component
    {
        protected (NavigationState state, Navigator navigator) useNavigation(string initialRoute)
        {
            var (state, setState) = useState(new NavigationState(initialRoute));
            return (state, new Navigator(state, setState));
        }
    }
}
