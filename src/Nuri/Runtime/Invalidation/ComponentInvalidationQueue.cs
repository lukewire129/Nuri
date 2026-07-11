using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nuri.UI.Dsl;

namespace Nuri.Runtime.Invalidation
{
    public sealed class ComponentInvalidationQueue
    {
        private readonly List<ComponentInvalidation> _dirtyComponents = new List<ComponentInvalidation>();
        private readonly HashSet<InvalidationKey> _dirtyKeys = new HashSet<InvalidationKey>();

        public bool HasPending => _dirtyComponents.Count > 0;

        public void Enqueue(Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            var key = new InvalidationKey(component, component.Id);
            if (_dirtyKeys.Add(key))
                _dirtyComponents.Add(new ComponentInvalidation(component, component.Id));
        }

        public IReadOnlyList<ComponentInvalidation> DrainCoveredByParents()
        {
            var dirtyIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var component in _dirtyComponents)
            {
                if (!string.IsNullOrEmpty(component.ComponentId))
                    dirtyIds.Add(component.ComponentId);
            }

            var result = new List<ComponentInvalidation>(_dirtyComponents.Count);
            var retainedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var component in _dirtyComponents)
            {
                if (string.IsNullOrEmpty(component.ComponentId)
                    || RuntimeTreeIdentity.HasAncestorInSet(component.ComponentId, dirtyIds)
                    || !retainedIds.Add(component.ComponentId))
                    continue;

                result.Add(component);
            }

            _dirtyComponents.Clear();
            _dirtyKeys.Clear();
            return result;
        }

        private readonly struct InvalidationKey : IEquatable<InvalidationKey>
        {
            public InvalidationKey(Component component, string componentId)
            {
                Component = component;
                ComponentId = componentId;
            }

            private Component Component { get; }
            private string ComponentId { get; }

            public bool Equals(InvalidationKey other)
                => ReferenceEquals(Component, other.Component)
                    && string.Equals(ComponentId, other.ComponentId, StringComparison.Ordinal);

            public override bool Equals(object? obj)
                => obj is InvalidationKey other && Equals(other);

            public override int GetHashCode()
                => (RuntimeHelpers.GetHashCode(Component) * 397) ^ StringComparer.Ordinal.GetHashCode(ComponentId);
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
