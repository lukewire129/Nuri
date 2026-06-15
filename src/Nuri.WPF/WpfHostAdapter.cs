using System.Windows;
using System.Windows.Controls;
using Nuri.Hosting;
using Nuri.Runtime;
using Nuri.UI.Dsl;

namespace Nuri.WPF
{
    public sealed class WpfHostAdapter : INuriHostAdapter<ContentControl, FrameworkElement>
    {
        public NuriMountedRoot<FrameworkElement> Attach(ContentControl host, IElement rootElement, NuriServiceProvider? services = null)
        {
            var root = NuriApplication.Attach(host, rootElement, services);
            return new NuriMountedRoot<FrameworkElement>(root.RootVisual, root.Services, root.Dispose);
        }
    }
}
