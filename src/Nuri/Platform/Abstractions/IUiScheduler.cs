using System;

namespace Nuri.Platform.Abstractions
{
    public interface IUiScheduler
    {
        void Schedule(Action action);
    }
}
