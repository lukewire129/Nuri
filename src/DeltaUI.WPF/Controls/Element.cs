using DeltaUI.Core.UI.Controls;
using DeltaUI.Core.UI.Values;
using System;

namespace DeltaUI.WPF
{
    public class Element : VirtualElement<IElement, AnimationValue>, IElement
    {
        public Element()
        {
        }

        public Element(string type) : base(type)
        {
        }

        public Element(string type, string kind) : base(type, kind)
        {
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is Element other))
                return false;

            return Type == other.Type && Kind == other.Kind;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Kind);
        }
    }

    public static partial class ElementVisualExtention
    {
        public static T Transitions<T>(this T node, string property, int msec, Easing easing) where T : IElement
        {
            if (AnimationMapper.factory.Contains(property))
            {
                int Easingtype = (int)easing / 2;
                int easingMode = (int)easing % 2;  // 2 : InOut, 1 : Out, 0 : In
                EasingValue? easingValue = null;
                if (Easingtype == 0)
                {
                    easingValue = new EasingValue(EasingKind.Cubic, GetType(easingMode));
                }

                return node.Transitions (property, msec, easingValue);
            }
            else
            {
                throw new NotSupportedException ($"Property '{property}' is not supported for animation.");
            }
        }
        public static T Transitions<T>(this T node, string property, int msec, EasingValue? easing = null) where T : IElement
        {
            if (AnimationMapper.factory.Contains(property))
            {
                var animation = new AnimationValue(property, node.Properties[property], TimeSpan.FromMilliseconds(msec), easing);
                node.AddAnimation (property, animation);
            }
            else
            {
                throw new NotSupportedException ($"Property '{property}' is not supported for animation.");
            }
            return node;
        }

        private static EasingModeValue GetType(int value)
        {
            return value switch
            {
                0 => EasingModeValue.In,
                1 => EasingModeValue.Out,
                2 => EasingModeValue.InOut,
                _ => EasingModeValue.In
            };
        }
    }
}
