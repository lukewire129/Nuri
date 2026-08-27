using System;

namespace Nuri.UI.Values
{
    /// <summary>
    /// Describes a transition of a single property from an optional start value to a target value over a duration with optional easing.
    /// </summary>
    public sealed class AnimationValue
    {
        /// <summary>
        /// Creates an animation for a property.
        /// </summary>
        /// <param name="propertyName">The property name to animate.</param>
        /// <param name="to">The target value.</param>
        /// <param name="duration">The transition duration.</param>
        /// <param name="easing">Optional easing mode.</param>
        /// <param name="from">Optional explicit start value.</param>
        public AnimationValue(string propertyName, object? to, TimeSpan duration, EasingValue? easing = null, object? from = null)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            To = to;
            Duration = duration;
            Easing = easing;
            From = from;
        }

        /// <summary>Gets the animated property name.</summary>
        public string PropertyName { get; }

        /// <summary>Gets the target value.</summary>
        public object? To { get; }

        /// <summary>Gets the optional explicit start value.</summary>
        public object? From { get; }

        /// <summary>Gets the transition duration.</summary>
        public TimeSpan Duration { get; }

        /// <summary>Gets the optional easing mode.</summary>
        public EasingValue? Easing { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is AnimationValue other
                && string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal)
                && Equals(To, other.To)
                && Equals(From, other.From)
                && Duration.Equals(other.Duration)
                && Equals(Easing, other.Easing);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PropertyName.GetHashCode();
                hashCode = (hashCode * 397) ^ (To?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (From?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Duration.GetHashCode();
                hashCode = (hashCode * 397) ^ (Easing?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Describes an easing function (kind plus in/out mode) applied to an <see cref="AnimationValue"/>.
    /// </summary>
    public sealed class EasingValue
    {
        /// <summary>
        /// Creates an easing value.
        /// </summary>
        /// <param name="kind">The easing kind.</param>
        /// <param name="mode">The easing mode (in, out, or in-out).</param>
        public EasingValue(EasingKind kind, EasingModeValue mode)
        {
            Kind = kind;
            Mode = mode;
        }

        /// <summary>Gets the easing kind.</summary>
        public EasingKind Kind { get; }

        /// <summary>Gets the easing mode.</summary>
        public EasingModeValue Mode { get; }

        /// <summary>Gets the cubic in easing.</summary>
        public static EasingValue CubicIn => new EasingValue(EasingKind.Cubic, EasingModeValue.In);

        /// <summary>Gets the cubic out easing.</summary>
        public static EasingValue CubicOut => new EasingValue(EasingKind.Cubic, EasingModeValue.Out);

        /// <summary>Gets the cubic in-out easing.</summary>
        public static EasingValue CubicInOut => new EasingValue(EasingKind.Cubic, EasingModeValue.InOut);

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is EasingValue other && Kind == other.Kind && Mode == other.Mode;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ (int)Mode;
            }
        }
    }

    /// <summary>The family of easing function applied to an animation.</summary>
    public enum EasingKind
    {
        /// <summary>A cubic easing curve.</summary>
        Cubic
    }

    /// <summary>The portion of the animation the easing is applied to.</summary>
    public enum EasingModeValue
    {
        /// <summary>Ease in (accelerate at the start).</summary>
        In,
        /// <summary>Ease out (decelerate at the end).</summary>
        Out,
        /// <summary>Ease in and out.</summary>
        InOut
    }
}
