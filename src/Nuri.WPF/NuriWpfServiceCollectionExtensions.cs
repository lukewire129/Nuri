using Nuri.Hosting;
using Nuri.Runtime;

namespace Nuri.WPF
{
    public static class NuriWpfServiceCollectionExtensions
    {
        public static NuriServiceCollection AddNuriWpfServices(this NuriServiceCollection services)
        {
            services.AddSingleton<IWindowService>(provider => new WpfWindowService(provider));
            return services;
        }
    }
}
