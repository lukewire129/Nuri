using Nuri.VirtualDom;
using System.Collections.Generic;
using System.Linq;

namespace Nuri.WPF
{
    public static class VirtualEntryAdapter
    {
        public static VirtualEntry ToVirtualEntry(this Nuri.UI.Dsl.IElement element)
        {
            if (element is Nuri.UI.Dsl.Component component)
                return ToVirtualEntry(component);

            var properties = element.Properties.Select(property => new KeyValuePair<string, object?>(property.Key, property.Value));
            var animations = element.Animations.Select(animation => new KeyValuePair<string, object?>(animation.Key, animation.Value));
            var children = element.Children.Select(ToVirtualEntry);
            var events = element.Events
                .Select(evt => new KeyValuePair<string, object?>(evt.Key, evt.Value))
                .Concat(element.VirtualEvents.Select(evt => new KeyValuePair<string, object?>(evt.Key, evt.Value)));

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

            foreach (var property in component.Properties)
            {
                if (!rendered.Properties.ContainsKey(property.Key))
                    rendered.Properties[property.Key] = property.Value;
            }

            rendered.ParentId = component.ParentId;
            rendered.Id = component.Id;
            Nuri.UI.ElementTree<Nuri.UI.Dsl.IElement, Nuri.UI.Values.AnimationValue>.AssignDescendantIds(component.Id, rendered);

            return rendered.ToVirtualEntry();
        }

        private static string? GetKey(string key, string name)
        {
            if (!string.IsNullOrWhiteSpace(key))
                return key;

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

    }
}
