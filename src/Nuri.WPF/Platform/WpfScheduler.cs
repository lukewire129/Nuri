using System;
using System.Windows;
using System.Windows.Threading;
using Nuri.Platform.Abstractions;

namespace Nuri.WPF
{
    internal sealed class WpfScheduler : IUiScheduler
    {
        private readonly Func<Dispatcher?> _getDispatcher;

        public WpfScheduler(Func<Dispatcher?> getDispatcher)
        {
            _getDispatcher = getDispatcher ?? throw new ArgumentNullException(nameof(getDispatcher));
        }

        public void Schedule(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var dispatcher = _getDispatcher() ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action, DispatcherPriority.Render);
        }
    }
}
