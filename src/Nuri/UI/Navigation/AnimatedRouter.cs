using System;
using System.Threading;
using System.Threading.Tasks;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.UI.Navigation
{
    public sealed class AnimatedRouter : Component
    {
        private readonly string _requestedRoute;
        private readonly TimeSpan _duration;
        private readonly EasingValue? _easing;
        private readonly RouteDefinition[] _routes;

        public AnimatedRouter(
            NavigationState navigationState,
            TimeSpan duration,
            EasingValue? easing,
            params RouteDefinition[] routes)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Transition duration must not be negative.");

            _requestedRoute = navigationState?.CurrentRoute ?? string.Empty;
            _duration = duration;
            _easing = easing;
            _routes = routes ?? Array.Empty<RouteDefinition>();
        }

        public override IElement Render()
        {
            var (transition, setTransition) = useState(TransitionState.Initial(_requestedRoute));

            useEffect(() =>
            {
                if (RouteEquals(transition.DisplayedRoute, _requestedRoute))
                {
                    if (transition.Phase != TransitionPhase.Idle || transition.Opacity < 1.0)
                    {
                        setTransition(current =>
                            RouteEquals(current.DisplayedRoute, _requestedRoute)
                                ? current.Enter()
                                : current);
                    }

                    return null;
                }

                var cancellation = new CancellationTokenSource();
                setTransition(current => current.ExitTo(_requestedRoute));
                _ = CompleteExitAsync(_requestedRoute, setTransition, cancellation.Token);

                return () =>
                {
                    cancellation.Cancel();
                    cancellation.Dispose();
                };
            }, [_requestedRoute]);

            useEffect(() =>
            {
                if (transition.Phase == TransitionPhase.Entering)
                {
                    setTransition(current =>
                        current.Phase == TransitionPhase.Entering
                            ? current.Enter()
                            : current);
                }
            }, [transition.Phase, transition.DisplayedRoute]);

            return Div(
                    DivTypes.Block,
                    RenderRoute(transition.DisplayedRoute))
                .Opacity(transition.Opacity)
                .Transition(_duration, _easing);
        }

        private async Task CompleteExitAsync(
            string targetRoute,
            Action<Func<TransitionState, TransitionState>> setTransition,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_duration, cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                    return;

                setTransition(current =>
                    RouteEquals(current.TargetRoute, targetRoute)
                        ? current.Show(targetRoute)
                        : current);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private IElement RenderRoute(string routeKey)
        {
            foreach (var route in _routes)
            {
                if (RouteEquals(route.Key, routeKey))
                    return new RouteHost(route).Key(route.Key);
            }

            return Div().Key("not-found:" + routeKey);
        }

        private static bool RouteEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private enum TransitionPhase
        {
            Idle,
            Exiting,
            Entering,
        }

        private sealed class TransitionState
        {
            private TransitionState(
                string displayedRoute,
                string targetRoute,
                double opacity,
                TransitionPhase phase)
            {
                DisplayedRoute = displayedRoute;
                TargetRoute = targetRoute;
                Opacity = opacity;
                Phase = phase;
            }

            public string DisplayedRoute { get; }

            public string TargetRoute { get; }

            public double Opacity { get; }

            public TransitionPhase Phase { get; }

            public static TransitionState Initial(string route)
            {
                return new TransitionState(route, route, 1.0, TransitionPhase.Idle);
            }

            public TransitionState ExitTo(string route)
            {
                return new TransitionState(DisplayedRoute, route, 0.0, TransitionPhase.Exiting);
            }

            public TransitionState Show(string route)
            {
                return new TransitionState(route, route, 0.0, TransitionPhase.Entering);
            }

            public TransitionState Enter()
            {
                return new TransitionState(DisplayedRoute, DisplayedRoute, 1.0, TransitionPhase.Idle);
            }
        }
    }
}
