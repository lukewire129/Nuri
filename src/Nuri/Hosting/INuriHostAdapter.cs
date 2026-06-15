using Nuri.Runtime;
using Nuri.UI.Dsl;

namespace Nuri.Hosting
{
    public interface INuriHostAdapter<TNativeHost, TNativeRoot>
    {
        NuriMountedRoot<TNativeRoot> Attach(TNativeHost host, IElement rootElement, NuriServiceProvider? services = null);
    }
}
