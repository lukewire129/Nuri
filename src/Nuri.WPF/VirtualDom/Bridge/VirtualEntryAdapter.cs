using Nuri.VirtualDom;
using System.Collections.Generic;

namespace Nuri.WPF
{
    public static class VirtualEntryAdapter
    {
        public static VirtualEntry ToVirtualEntry(this Nuri.UI.Dsl.IElement element)
        {
            if (element is Nuri.UI.Dsl.Component component)
                return ToVirtualEntry(component);

            var properties = new List<KeyValuePair<string, object?>>(element.Properties.Count);
            foreach (var property in element.Properties)
                properties.Add(new KeyValuePair<string, object?>(property.Key, property.Value));

            var animations = new List<KeyValuePair<string, object?>>(element.Animations.Count);
            foreach (var animation in element.Animations)
                animations.Add(new KeyValuePair<string, object?>(animation.Key, animation.Value));

            var children = new List<VirtualEntry>(element.Children.Count);
            foreach (var child in element.Children)
                children.Add(child.ToVirtualEntry());

            var events = new List<KeyValuePair<string, object?>>(element.Events.Count + element.VirtualEvents.Count);
            foreach (var evt in element.Events)
                events.Add(new KeyValuePair<string, object?>(evt.Key, evt.Value));
            foreach (var evt in element.VirtualEvents)
                events.Add(new KeyValuePair<string, object?>(evt.Key, evt.Value));

            var entry = new VirtualEntry(
                element.Type,
                kind: element.Kind,
                key: GetKey(element.Key, element.Name),
                properties: properties,
                events: events,
                animations: animations,
                children: children);

            return entry.WithIdentity(string.IsNullOrWhiteSpace(element.Id) ? "0" : element.Id, element.ParentId, rewriteChildren: false);
        }

        private static VirtualEntry ToVirtualEntry(Nuri.UI.Dsl.Component component)
        {
            component.ResetStateIndexForRender();
            var rendered = component.Render();
            component.CompleteRenderHooks();

            foreach (var property in component.Properties)
            {
                if (!rendered.Properties.ContainsKey(property.Key))
                    rendered.Properties[property.Key] = property.Value;
            }

            ApplyComponentKey(component, rendered);

            var renderedId = GetRenderedRootId(component, rendered);
            rendered.ParentId = component.ParentId;
            rendered.Id = renderedId;
            Nuri.UI.ElementTree<Nuri.UI.Dsl.IElement, Nuri.UI.Values.AnimationValue>.AssignDescendantIds(renderedId, rendered);

            return rendered.ToVirtualEntry().WithComponentId(component.Id);
        }

        private static string GetRenderedRootId(Nuri.UI.Dsl.Component component, Nuri.UI.Dsl.IElement rendered)
        {
            if (!string.IsNullOrWhiteSpace(component.Key)
                && string.Equals(component.Key, rendered.Key, System.StringComparison.Ordinal))
                return component.Id;

            return !string.IsNullOrWhiteSpace(rendered.Key)
                ? component.Id + "#key:" + rendered.Key
                : component.Id;
        }

        private static void ApplyComponentKey(Nuri.UI.Dsl.Component component, Nuri.UI.Dsl.IElement rendered)
        {
            if (string.IsNullOrWhiteSpace(rendered.Key) && !string.IsNullOrWhiteSpace(component.Key))
                rendered.Key = component.Key;
        }

        private static string? GetKey(string key, string name)
        {
            if (!string.IsNullOrWhiteSpace(key))
                return key;

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

    }
}
