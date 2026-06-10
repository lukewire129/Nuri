using DeltaUI.Core.UI.Controls;
using DeltaUI.Core.UI.Values;

namespace DeltaUI.WPF
{
    public class ContentControl : VirtualContentControl<IElement, AnimationValue>, IContent
    {
        public ContentControl(string type) : base(type) { }

        public ContentControl(string type, string kind) : base(type, kind) { }
    }
}
