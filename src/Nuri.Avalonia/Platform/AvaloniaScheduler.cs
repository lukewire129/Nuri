using System;
using Avalonia.Threading;
using Nuri.Platform.Abstractions;

namespace Nuri.Avalonia
{
    internal sealed class AvaloniaScheduler : IUiScheduler
    {
        public void Schedule(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Dispatcher.UIThread.Post(action);
        }
    }
}
