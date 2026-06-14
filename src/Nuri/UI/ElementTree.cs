using System.Collections.Generic;

namespace Nuri.UI
{
    public static class ElementTree<TElement, TAnimation>
        where TElement : class, IElement<TElement, TAnimation>
    {
        public static TElement Create<TNode>(string type)
            where TNode : TElement, new()
        {
            var node = new TNode();
            node.Type = type;
            return node;
        }

        public static TElement Create<TNode>(string type, string kind)
            where TNode : TElement, new()
        {
            var node = new TNode();
            node.Type = type;
            node.Kind = kind;
            return node;
        }

        public static TElement AddChild(TElement parent, TElement child, int childIndex)
        {
            child.LoadNodeNumber(parent.Id, childIndex);
            AssignDescendantIds(child.Id, child);
            parent.Children.Add(child);
            return parent;
        }

        public static TElement AddChildren(TElement parent, IEnumerable<TElement?> children)
        {
            var childIndex = 1;
            foreach (var child in children)
            {
                if (child != null)
                {
                    AddChild(parent, child, childIndex);
                }

                childIndex++;
            }

            return parent;
        }

        public static TElement SetContent(TElement parent, TElement child)
        {
            child.LoadNodeNumber(parent.Id, 1);
            AssignDescendantIds(child.Id, child);
            parent.Children.Add(child);
            return parent;
        }

        public static TElement SetContent(TElement parent, object content)
        {
            return parent.SetProperty("Content", content);
        }

        public static void AssignDescendantIds(string parentId, TElement element)
        {
            var childIndex = 1;
            foreach (var child in element.Children)
            {
                child.LoadNodeNumber(parentId, childIndex);
                AssignDescendantIds(child.Id, child);
                childIndex++;
            }
        }
    }
}
