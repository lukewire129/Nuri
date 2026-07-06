using System;
using System.Collections.Generic;
using System.Linq;
using Nuri.UI.Dsl;

namespace Nuri.Runtime.Invalidation
{
    public sealed class ComponentInvalidationQueue
    {
        private readonly List<Component> _dirtyComponents = new List<Component>();

        public bool HasPending => _dirtyComponents.Count > 0;

        public void Enqueue(Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (!_dirtyComponents.Any(dirty => ReferenceEquals(dirty, component)))
                _dirtyComponents.Add(component);
        }

        public IReadOnlyList<Component> DrainCoveredByParents()
        {
            var ordered = _dirtyComponents
                .Where(component => !string.IsNullOrEmpty(component.Id))
                .OrderBy(component => component.Id.Length)
                .ToList();

            _dirtyComponents.Clear();

            var result = new List<Component>();
            foreach (var component in ordered)
            {
                if (!result.Any(parent => IsDescendantId(component.Id, parent.Id)))
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
}
