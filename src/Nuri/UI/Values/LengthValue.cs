using System;

namespace Nuri.UI.Values
{
    public readonly struct LengthValue : IEquatable<LengthValue>
    {
        private LengthValue(double value, LengthUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        public double Value { get; }

        public LengthUnit Unit { get; }

        public static LengthValue Pixels(double value)
        {
            return new LengthValue(value, LengthUnit.Pixel);
        }

        public static LengthValue Star(double value = 1)
        {
            return new LengthValue(value, LengthUnit.Star);
        }

        public static LengthValue Auto()
        {
            return new LengthValue(0, LengthUnit.Auto);
        }

        public bool Equals(LengthValue other)
        {
            return Value.Equals(other.Value) && Unit == other.Unit;
        }

        public override bool Equals(object? obj)
        {
            return obj is LengthValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Value.GetHashCode() * 397) ^ (int)Unit;
            }
        }
    }

    public enum LengthUnit
    {
        Pixel,
        Star,
        Auto
    }
}
