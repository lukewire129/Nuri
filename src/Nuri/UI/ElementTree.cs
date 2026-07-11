using System.Collections.Generic;
using System.Diagnostics;
using Nuri.Runtime.Diagnostics;

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
            var duplicateComponentKeys = FindDuplicateComponentKeys(element.Children);
            foreach (var key in duplicateComponentKeys)
            {
                var componentTypes = GetComponentTypesForKey(element.Children, key);
                var message = $"Duplicate component key '{key}' under parent '{parentId}' for [{componentTypes}]. Falling back to position-based hook identity.";
                Debug.WriteLine(message);
                NuriDiagnostics.Log(RuntimeLogKind.DuplicateKey, null, null, message);
            }

            var childIndex = 1;
            foreach (var child in element.Children)
            {
                if (child is ComponentBase<TElement, TAnimation> component
                    && !string.IsNullOrWhiteSpace(child.Key)
                    && duplicateComponentKeys.Contains(child.Key))
                    component.LoadNodeNumberByPosition(parentId, childIndex);
                else
                    child.LoadNodeNumber(parentId, childIndex);

                AssignDescendantIds(child.Id, child);
                childIndex++;
            }
        }

        private static HashSet<string> FindDuplicateComponentKeys(IEnumerable<TElement> children)
        {
            var keys = new HashSet<string>(System.StringComparer.Ordinal);
            var duplicates = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var child in children)
            {
                if (child is not ComponentBase<TElement, TAnimation>
                    || string.IsNullOrWhiteSpace(child.Key))
                    continue;

                if (!keys.Add(child.Key))
                    duplicates.Add(child.Key);
            }

            return duplicates;
        }

        private static string GetComponentTypesForKey(IEnumerable<TElement> children, string key)
        {
            var types = new List<string>();
            foreach (var child in children)
            {
                if (child is ComponentBase<TElement, TAnimation>
                    && string.Equals(child.Key, key, System.StringComparison.Ordinal))
                    types.Add(child.GetType().Name);
            }

            return string.Join(", ", types);
        }
    }
}
