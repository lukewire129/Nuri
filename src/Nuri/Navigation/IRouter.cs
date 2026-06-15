using System;
using Nuri.UI.Dsl;

namespace Nuri.Navigation
{
    public interface IRouter
    {
        event EventHandler? Changed;

        RouteState Current { get; }

        bool CanGoBack { get; }

        void Navigate<TComponent>() where TComponent : Component, new();

        void Navigate<TComponent>(object? parameter) where TComponent : Component, new();

        void Navigate(string key, Func<IElement> factory, object? parameter = null);

        void Replace<TComponent>() where TComponent : Component, new();

        void Replace<TComponent>(object? parameter) where TComponent : Component, new();

        void Back();
    }
}
