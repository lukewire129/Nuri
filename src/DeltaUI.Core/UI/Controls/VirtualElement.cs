using DeltaUI.Core.UI;

namespace DeltaUI.Core.UI.Controls
{
    public class VirtualElement<TElement, TAnimation> : Element<TElement, TAnimation>
    {
        public VirtualElement()
        {
        }

        public VirtualElement(string type)
        {
            Type = type;
        }

        public VirtualElement(string type, string kind)
        {
            Type = type;
            Kind = kind;
        }
    }
}
