using System.Collections.Generic;
using System.Linq;

namespace Nuri.UI.Values
{
    /// <summary>
    /// Base type for background, foreground, and border brushes. Use <see cref="Solid"/> or <see cref="LinearGradient"/>.
    /// </summary>
    public abstract class BrushValue
    {
        private BrushValue()
        {
        }

        /// <summary>
        /// A single solid color brush.
        /// </summary>
        public sealed class Solid : BrushValue
        {
            /// <summary>
            /// Creates a solid color brush.
            /// </summary>
            /// <param name="color">The solid color.</param>
            public Solid(ColorValue color)
            {
                Color = color;
            }

            /// <summary>Gets the solid color.</summary>
            public ColorValue Color { get; }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                return obj is Solid other && Color.Equals(other.Color);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return Color.GetHashCode();
            }
        }

        /// <summary>
        /// A linear gradient brush defined by start/end points and gradient stops.
        /// </summary>
        public sealed class LinearGradient : BrushValue
        {
            /// <summary>
            /// Creates a linear gradient brush.
            /// </summary>
            /// <param name="startPoint">The gradient start point.</param>
            /// <param name="endPoint">The gradient end point.</param>
            /// <param name="stops">The gradient stops.</param>
            public LinearGradient(GradientPointValue startPoint, GradientPointValue endPoint, IEnumerable<GradientStopValue> stops)
            {
                StartPoint = startPoint;
                EndPoint = endPoint;
                Stops = new List<GradientStopValue>(stops).AsReadOnly();
            }

            /// <summary>Gets the gradient start point.</summary>
            public GradientPointValue StartPoint { get; }

            /// <summary>Gets the gradient end point.</summary>
            public GradientPointValue EndPoint { get; }

            /// <summary>Gets the read-only gradient stops.</summary>
            public IReadOnlyList<GradientStopValue> Stops { get; }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                return obj is LinearGradient other
                    && StartPoint.Equals(other.StartPoint)
                    && EndPoint.Equals(other.EndPoint)
                    && Stops.SequenceEqual(other.Stops);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = StartPoint.GetHashCode();
                    hashCode = (hashCode * 397) ^ EndPoint.GetHashCode();
                    foreach (var stop in Stops)
                        hashCode = (hashCode * 397) ^ stop.GetHashCode();
                    return hashCode;
                }
            }
        }
    }

    /// <summary>
    /// A point in the 0-1 gradient space used by <see cref="BrushValue.LinearGradient"/>.
    /// </summary>
    public readonly struct GradientPointValue : System.IEquatable<GradientPointValue>
    {
        /// <summary>
        /// Creates a gradient point.
        /// </summary>
        /// <param name="x">The horizontal position (0-1).</param>
        /// <param name="y">The vertical position (0-1).</param>
        public GradientPointValue(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Gets the horizontal position.</summary>
        public double X { get; }

        /// <summary>Gets the vertical position.</summary>
        public double Y { get; }

        /// <summary>Determines whether this point equals another.</summary>
        public bool Equals(GradientPointValue other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is GradientPointValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }
    }

    /// <summary>
    /// A color and offset pair within a <see cref="BrushValue.LinearGradient"/>.
    /// </summary>
    public readonly struct GradientStopValue : System.IEquatable<GradientStopValue>
    {
        /// <summary>
        /// Creates a gradient stop.
        /// </summary>
        /// <param name="color">The stop color.</param>
        /// <param name="offset">The offset along the gradient (0-1).</param>
        public GradientStopValue(ColorValue color, double offset)
        {
            Color = color;
            Offset = offset;
        }

        /// <summary>Gets the stop color.</summary>
        public ColorValue Color { get; }

        /// <summary>Gets the offset along the gradient.</summary>
        public double Offset { get; }

        /// <summary>Determines whether this stop equals another.</summary>
        public bool Equals(GradientStopValue other)
        {
            return Color.Equals(other.Color) && Offset.Equals(other.Offset);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is GradientStopValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Color.GetHashCode() * 397) ^ Offset.GetHashCode();
            }
        }
    }
}
