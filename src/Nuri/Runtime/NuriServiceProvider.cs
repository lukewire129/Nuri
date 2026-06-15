using System;
using System.Collections.Generic;

namespace Nuri.Runtime
{
    public sealed class NuriServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, Func<NuriServiceProvider, object>> _factories;
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private readonly IServiceProvider? _fallback;

        public NuriServiceProvider()
            : this(new Dictionary<Type, Func<NuriServiceProvider, object>>(), null)
        {
        }

        internal NuriServiceProvider(Dictionary<Type, Func<NuriServiceProvider, object>> factories, IServiceProvider? fallback)
        {
            _factories = new Dictionary<Type, Func<NuriServiceProvider, object>>(factories);
            _fallback = fallback;
        }

        public static NuriServiceProvider Empty { get; } = new NuriServiceProvider();

        public static NuriServiceProvider FromServiceProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            return new NuriServiceProvider(new Dictionary<Type, Func<NuriServiceProvider, object>>(), serviceProvider);
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            if (_instances.TryGetValue(serviceType, out var instance))
                return instance;

            if (!_factories.TryGetValue(serviceType, out var factory))
                return _fallback?.GetService(serviceType);

            instance = factory(this);
            _instances[serviceType] = instance;
            return instance;
        }

        public TService? GetService<TService>() where TService : class
        {
            return GetService(typeof(TService)) as TService;
        }

        public TService GetRequiredService<TService>() where TService : class
        {
            return GetService<TService>()
                ?? throw new InvalidOperationException($"Service '{typeof(TService).FullName}' is not registered.");
        }
    }
}
