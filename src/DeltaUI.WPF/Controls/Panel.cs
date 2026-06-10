using DeltaUI.Core.UI;
using DeltaUI.Core.UI.Values;

namespace DeltaUI.WPF
{
    public class Panel : Visual
    {
        public Panel(string type) : base (type)
        {
        }

        public Panel(string type, params IElement[] node) : this (type)
        {
            AddChild (node);
        }

        public void AddChild(IElement[] child)
        {
            string parentId = this.ParentId;
            int id = 1;
            foreach (IElement element in child)
            {
                if (element == null)
                {
                    continue;
                }
                else if (element is Component component)
                {
                    element.LoadNodeNumber (parentId, id);
                    ApplicationRoot.Components.Add (component);
                    // 부모에서 자식의 Render 결과를 자동 추가
                    var renderedChild = component.Render ();

                    var temp = element.GetAttachedProperty ();
                    foreach (var item in temp)
                    {
                        if (item.Value != null && !renderedChild.Properties.ContainsKey (item.Key))
                        {
                            renderedChild.Properties.Add (item.Key, item.Value);
                        }
                    }
                    renderedChild.LoadNodeNumber (parentId, id);

                    ElementTree<IElement, AnimationValue>.AddChild(this, renderedChild, id);
                }
                else
                {
                    ElementTree<IElement, AnimationValue>.AddChild(this, element, id);
                }
                id++;
            }
        }
    }
}
