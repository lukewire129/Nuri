using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    public class Element : UI.Element<IElement, AnimationValue>, IElement
    {
        public Element()
        {
        }

        public Element(string type)
        {
            Type = type;
        }

        public Element(string type, string kind)
        {
            Type = type;
            Kind = kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is Element other && Type == other.Type && Kind == other.Kind;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type?.GetHashCode() ?? 0) * 397) ^ (Kind?.GetHashCode() ?? 0);
            }
        }
    }
}
