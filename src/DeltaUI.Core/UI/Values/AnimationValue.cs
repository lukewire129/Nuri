using System;

namespace DeltaUI.Core.UI.Values
{
    public sealed class AnimationValue
    {
        public AnimationValue(string propertyName, object? to, TimeSpan duration, EasingValue? easing = null)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            To = to;
            Duration = duration;
            Easing = easing;
        }

        public string PropertyName { get; }

        public object? To { get; }

        public TimeSpan Duration { get; }

        public EasingValue? Easing { get; }

        public override bool Equals(object? obj)
        {
            return obj is AnimationValue other
                && string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal)
                && Equals(To, other.To)
                && Duration.Equals(other.Duration)
                && Equals(Easing, other.Easing);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PropertyName.GetHashCode();
                hashCode = (hashCode * 397) ^ (To?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Duration.GetHashCode();
                hashCode = (hashCode * 397) ^ (Easing?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    public sealed class EasingValue
    {
        public EasingValue(EasingKind kind, EasingModeValue mode)
        {
            Kind = kind;
            Mode = mode;
        }

        public EasingKind Kind { get; }

        public EasingModeValue Mode { get; }

        public override bool Equals(object? obj)
        {
            return obj is EasingValue other && Kind == other.Kind && Mode == other.Mode;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ (int)Mode;
            }
        }
    }

    public enum EasingKind
    {
        Cubic
    }

    public enum EasingModeValue
    {
        In,
        Out,
        InOut
    }
}
