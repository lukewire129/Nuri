using System;

namespace Nuri.Runtime
{
    public static class NuriServices
    {
        private static readonly object SyncRoot = new object();
        private static IServiceProvider? _serviceProvider;

        public static void UseServiceProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            lock (SyncRoot)
            {
                if (_serviceProvider != null)
                    throw new InvalidOperationException("Nuri service provider is already configured.");

                _serviceProvider = serviceProvider;
            }
        }

        public static TService GetRequiredService<TService>()
            where TService : class
        {
            IServiceProvider? serviceProvider;
            lock (SyncRoot)
                serviceProvider = _serviceProvider;

            if (serviceProvider == null)
                throw new InvalidOperationException("Nuri service provider is not configured. Call NuriServices.UseServiceProvider before rendering.");

            return serviceProvider.GetService(typeof(TService)) as TService
                ?? throw new InvalidOperationException($"The configured service provider does not contain {typeof(TService)}.");
        }
    }
}
