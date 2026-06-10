using System.Collections.Generic;
using System.Linq;

namespace DeltaUI.Core.UI.Values
{
    public abstract class BrushValue
    {
        private BrushValue()
        {
        }

        public sealed class Solid : BrushValue
        {
            public Solid(ColorValue color)
            {
                Color = color;
            }

            public ColorValue Color { get; }

            public override bool Equals(object? obj)
            {
                return obj is Solid other && Color.Equals(other.Color);
            }

            public override int GetHashCode()
            {
                return Color.GetHashCode();
            }
        }

        public sealed class LinearGradient : BrushValue
        {
            public LinearGradient(GradientPointValue startPoint, GradientPointValue endPoint, IEnumerable<GradientStopValue> stops)
            {
                StartPoint = startPoint;
                EndPoint = endPoint;
                Stops = new List<GradientStopValue>(stops).AsReadOnly();
            }

            public GradientPointValue StartPoint { get; }

            public GradientPointValue EndPoint { get; }

            public IReadOnlyList<GradientStopValue> Stops { get; }

            public override bool Equals(object? obj)
            {
                return obj is LinearGradient other
                    && StartPoint.Equals(other.StartPoint)
                    && EndPoint.Equals(other.EndPoint)
                    && Stops.SequenceEqual(other.Stops);
            }

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

    public readonly struct GradientPointValue : System.IEquatable<GradientPointValue>
    {
        public GradientPointValue(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }

        public bool Equals(GradientPointValue other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object? obj)
        {
            return obj is GradientPointValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }
    }

    public readonly struct GradientStopValue : System.IEquatable<GradientStopValue>
    {
        public GradientStopValue(ColorValue color, double offset)
        {
            Color = color;
            Offset = offset;
        }

        public ColorValue Color { get; }

        public double Offset { get; }

        public bool Equals(GradientStopValue other)
        {
            return Color.Equals(other.Color) && Offset.Equals(other.Offset);
        }

        public override bool Equals(object? obj)
        {
            return obj is GradientStopValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Color.GetHashCode() * 397) ^ Offset.GetHashCode();
            }
        }
    }
}
