using System;
using System.Collections.Generic;

namespace Nuri.Runtime
{
    public sealed class NuriServiceCollection
    {
        private readonly Dictionary<Type, Func<NuriServiceProvider, object>> _factories =
            new Dictionary<Type, Func<NuriServiceProvider, object>>();
        private IServiceProvider? _fallback;

        public NuriServiceCollection AddSingleton<TService>(TService instance) where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _factories[typeof(TService)] = _ => instance;
            return this;
        }

        public NuriServiceCollection AddSingleton<TService>(Func<NuriServiceProvider, TService> factory) where TService : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[typeof(TService)] = provider => factory(provider);
            return this;
        }

        public NuriServiceCollection AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService, new()
        {
            _factories[typeof(TService)] = _ => new TImplementation();
            return this;
        }

        public NuriServiceCollection UseFallback(IServiceProvider serviceProvider)
        {
            _fallback = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            return this;
        }

        public NuriServiceProvider BuildServiceProvider()
        {
            return new NuriServiceProvider(_factories, _fallback);
        }
    }
}
