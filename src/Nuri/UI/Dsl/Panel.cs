using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    public class Panel : Visual
    {
        public Panel(string type) : base(type)
        {
        }

        public Panel(string type, params IElement[] children) : this(type)
        {
            AddChildren(children);
        }

        public void AddChildren(IElement[] children)
        {
            var childIndex = 1;
            foreach (var child in children)
            {
                if (child != null)
                    ElementTree<IElement, AnimationValue>.AddChild(this, child, childIndex);

                childIndex++;
            }
        }
    }
}
