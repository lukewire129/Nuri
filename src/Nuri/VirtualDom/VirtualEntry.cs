using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Nuri.VirtualDom
{
    public sealed class VirtualEntry
    {
        private readonly List<VirtualEntry>? _children;

        public VirtualEntry(
            string type,
            string? kind = null,
            string? key = null,
            IEnumerable<KeyValuePair<string, object?>>? properties = null,
            IEnumerable<KeyValuePair<string, object?>>? events = null,
            IEnumerable<KeyValuePair<string, object?>>? animations = null,
            IEnumerable<VirtualEntry>? children = null)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Kind = kind ?? string.Empty;
            Key = key;
            Properties = ToReadOnlyDictionary(properties);
            Events = ToReadOnlyDictionary(events);
            Animations = ToReadOnlyDictionary(animations);
            _children = ToChildList(children);
        }

        public string Type { get; }

        public string Kind { get; }

        public string? Key { get; }

        public string Id { get; internal set; } = string.Empty;

        public string? ParentId { get; internal set; }

        public string? ComponentId { get; private set; }

        public IReadOnlyDictionary<string, object?> Properties { get; }

        public IReadOnlyDictionary<string, object?> Events { get; }

        public IReadOnlyDictionary<string, object?> Animations { get; }

        public IReadOnlyList<VirtualEntry> Children =>
            _children is null ? Array.Empty<VirtualEntry>() : _children;

        public VirtualEntry WithIdentity(string id, string? parentId, bool rewriteChildren = true)
        {
            Id = id;
            ParentId = parentId;

            if (!rewriteChildren)
                return this;

            if (_children != null)
            {
                for (var i = 0; i < _children.Count; i++)
                {
                    var childId = string.IsNullOrEmpty(_children[i].Id) ? $"{id}.{i}" : _children[i].Id;
                    var childParentId = string.IsNullOrEmpty(_children[i].ParentId) ? id : _children[i].ParentId;
                    _children[i].WithIdentity(childId, childParentId);
                }
            }

            return this;
        }

        public VirtualEntry RewriteIdentity(string id, string? parentId)
        {
            Id = id;
            ParentId = parentId;

            if (_children != null)
            {
                for (var i = 0; i < _children.Count; i++)
                    _children[i].RewriteIdentity($"{id}.{i}", id);
            }

            return this;
        }

        public VirtualEntry WithComponentId(string componentId)
        {
            ComponentId = componentId;
            return this;
        }

        public bool IsSameEntry(VirtualEntry other)
        {
            if (other == null)
                return false;

            if (!string.Equals(Type, other.Type, StringComparison.Ordinal))
                return false;

            if (!string.Equals(Kind, other.Kind, StringComparison.Ordinal))
                return false;

            if (Key != null || other.Key != null)
                return string.Equals(Key, other.Key, StringComparison.Ordinal);

            return string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public VirtualEntry? FindById(string id)
        {
            if (string.Equals(Id, id, StringComparison.Ordinal))
                return this;

            if (_children != null)
            {
                foreach (var child in _children)
                {
                    var result = child.FindById(id);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        public VirtualEntry? FindByComponentId(string componentId)
        {
            if (string.Equals(ComponentId, componentId, StringComparison.Ordinal))
                return this;

            if (_children != null)
            {
                foreach (var child in _children)
                {
                    var result = child.FindByComponentId(componentId);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        public bool ReplaceDescendant(string id, VirtualEntry replacement)
        {
            if (_children == null)
                return false;

            for (var i = 0; i < _children.Count; i++)
            {
                if (string.Equals(_children[i].Id, id, StringComparison.Ordinal))
                {
                    _children[i] = replacement;
                    return true;
                }

                if (_children[i].ReplaceDescendant(id, replacement))
                    return true;
            }

            return false;
        }

        public bool ReplaceDescendantByComponentId(string componentId, VirtualEntry replacement)
        {
            if (_children == null)
                return false;

            for (var i = 0; i < _children.Count; i++)
            {
                if (string.Equals(_children[i].ComponentId, componentId, StringComparison.Ordinal))
                {
                    _children[i] = replacement;
                    return true;
                }

                if (_children[i].ReplaceDescendantByComponentId(componentId, replacement))
                    return true;
            }

            return false;
        }

        private static IReadOnlyDictionary<string, TValue> ToReadOnlyDictionary<TValue>(
            IEnumerable<KeyValuePair<string, TValue>>? values)
        {
            if (values == null)
                return EmptyReadOnlyDictionary<TValue>.Instance;

            var capacity = values is ICollection<KeyValuePair<string, TValue>> collection
                ? collection.Count
                : 0;
            if (capacity == 0 && values is IReadOnlyCollection<KeyValuePair<string, TValue>> readOnlyCollection)
                capacity = readOnlyCollection.Count;

            using var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext())
                return EmptyReadOnlyDictionary<TValue>.Instance;

            var dictionary = capacity > 0
                ? new Dictionary<string, TValue>(capacity)
                : new Dictionary<string, TValue>();
            do
            {
                var value = enumerator.Current;
                dictionary[value.Key] = value.Value;
            }
            while (enumerator.MoveNext());

            return dictionary;
        }

        private static List<VirtualEntry>? ToChildList(IEnumerable<VirtualEntry>? children)
        {
            if (children == null)
                return null;

            var capacity = children is ICollection<VirtualEntry> collection
                ? collection.Count
                : 0;
            if (capacity == 0 && children is IReadOnlyCollection<VirtualEntry> readOnlyCollection)
                capacity = readOnlyCollection.Count;

            using var enumerator = children.GetEnumerator();
            if (!enumerator.MoveNext())
                return null;

            var result = capacity > 0
                ? new List<VirtualEntry>(capacity)
                : new List<VirtualEntry>();
            do
            {
                result.Add(enumerator.Current);
            }
            while (enumerator.MoveNext());

            return result;
        }

        private static class EmptyReadOnlyDictionary<TValue>
        {
            public static readonly IReadOnlyDictionary<string, TValue> Instance =
                new ReadOnlyDictionary<string, TValue>(new Dictionary<string, TValue>());
        }
    }
}
