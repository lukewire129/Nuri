using Nuri.UI.Dsl;
using System.Threading.Tasks;

namespace Nuri.Hosting
{
    public interface IWindowService
    {
        INuriWindowHandle Show<TComponent>(NuriWindowOptions? options = null)
            where TComponent : Component, new();

        INuriWindowHandle Show(IElement rootElement, NuriWindowOptions? options = null);

        bool? ShowDialog<TComponent>(NuriWindowOptions? options = null)
            where TComponent : Component, new();

        bool? ShowDialog(IElement rootElement, NuriWindowOptions? options = null);

        Task<bool?> ShowDialogAsync<TComponent>(NuriWindowOptions? options = null)
            where TComponent : Component, new();

        Task<bool?> ShowDialogAsync(IElement rootElement, NuriWindowOptions? options = null);
    }
}
