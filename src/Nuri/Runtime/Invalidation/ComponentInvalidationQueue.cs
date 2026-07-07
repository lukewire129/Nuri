using System;
using System.Collections.Generic;
using System.Linq;
using Nuri.UI.Dsl;

namespace Nuri.Runtime.Invalidation
{
    public sealed class ComponentInvalidationQueue
    {
        private readonly List<ComponentInvalidation> _dirtyComponents = new List<ComponentInvalidation>();

        public bool HasPending => _dirtyComponents.Count > 0;

        public void Enqueue(Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (!_dirtyComponents.Any(dirty => ReferenceEquals(dirty.Component, component) && string.Equals(dirty.ComponentId, component.Id, StringComparison.Ordinal)))
                _dirtyComponents.Add(new ComponentInvalidation(component, component.Id));
        }

        public IReadOnlyList<ComponentInvalidation> DrainCoveredByParents()
        {
            var ordered = _dirtyComponents
                .Where(component => !string.IsNullOrEmpty(component.ComponentId))
                .OrderBy(component => component.ComponentId.Length)
                .ToList();

            _dirtyComponents.Clear();

            var result = new List<ComponentInvalidation>();
            foreach (var component in ordered)
            {
                if (!result.Any(parent => IsDescendantId(component.ComponentId, parent.ComponentId)))
                    result.Add(component);
            }

            return result;
        }

        private static bool IsDescendantId(string childId, string parentId)
        {
            return childId.Length > parentId.Length
                && childId.StartsWith(parentId + "_", StringComparison.Ordinal);
        }
    }

    public sealed class ComponentInvalidation
    {
        public ComponentInvalidation(Component component, string componentId)
        {
            Component = component ?? throw new ArgumentNullException(nameof(component));
            ComponentId = componentId ?? string.Empty;
        }

        public Component Component { get; }

        public string ComponentId { get; }
    }
}
