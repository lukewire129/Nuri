using System;
using Nuri.Runtime;

namespace Nuri.Hosting
{
    public sealed class NuriMountedRoot<TNativeRoot> : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public NuriMountedRoot(TNativeRoot nativeRoot, NuriServiceProvider services, Action dispose)
        {
            NativeRoot = nativeRoot;
            Services = services ?? throw new ArgumentNullException(nameof(services));
            _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
        }

        public TNativeRoot NativeRoot { get; }

        public NuriServiceProvider Services { get; }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _dispose();
        }
    }
}
