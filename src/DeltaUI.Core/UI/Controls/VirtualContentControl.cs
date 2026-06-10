namespace DeltaUI.Core.UI.Controls
{
    public class VirtualContentControl<TElement, TAnimation> : VirtualElement<TElement, TAnimation>
        where TElement : class, IElement<TElement, TAnimation>
    {
        public VirtualContentControl(string type) : base(type)
        {
        }

        public VirtualContentControl(string type, string kind) : base(type, kind)
        {
        }

        public TElement Content(TElement element)
        {
            return ElementTree<TElement, TAnimation>.SetContent((TElement)(object)this, element);
        }

        public TElement Content(object content)
        {
            return ElementTree<TElement, TAnimation>.SetContent((TElement)(object)this, content);
        }
    }
}
