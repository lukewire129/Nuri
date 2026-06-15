using System;
using System.Threading;

namespace Nuri.Runtime
{
    public static class NuriRuntimeContext
    {
        private static readonly AsyncLocal<NuriServiceProvider?> CurrentServices = new AsyncLocal<NuriServiceProvider?>();

        public static NuriServiceProvider Services => CurrentServices.Value ?? NuriServiceProvider.Empty;

        public static IDisposable PushServices(NuriServiceProvider services)
        {
            return new ServiceScope(services ?? NuriServiceProvider.Empty);
        }

        private sealed class ServiceScope : IDisposable
        {
            private readonly NuriServiceProvider? _previous;

            public ServiceScope(NuriServiceProvider services)
            {
                _previous = CurrentServices.Value;
                CurrentServices.Value = services;
            }

            public void Dispose()
            {
                CurrentServices.Value = _previous;
            }
        }
    }
}
