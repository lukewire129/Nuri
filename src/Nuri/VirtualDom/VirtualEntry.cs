using System;
using System.Collections.Generic;
using System.Linq;

namespace Nuri.VirtualDom
{
    public sealed class VirtualEntry
    {
        private readonly List<VirtualEntry> _children;
        private readonly List<IDisposable> _owners;

        public VirtualEntry(
            string type,
            string? kind = null,
            string? key = null,
            IEnumerable<KeyValuePair<string, object?>>? properties = null,
            IEnumerable<KeyValuePair<string, object?>>? events = null,
            IEnumerable<KeyValuePair<string, object?>>? animations = null,
            IEnumerable<VirtualEntry>? children = null,
            IEnumerable<IDisposable>? owners = null)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Kind = kind ?? string.Empty;
            Key = key;
            Properties = ToDictionary(properties ?? Enumerable.Empty<KeyValuePair<string, object?>>());
            Events = ToDictionary(events ?? Enumerable.Empty<KeyValuePair<string, object?>>());
            Animations = ToDictionary(animations ?? Enumerable.Empty<KeyValuePair<string, object?>>());
            _children = new List<VirtualEntry>(children ?? Enumerable.Empty<VirtualEntry>());
            _owners = new List<IDisposable>(owners ?? Enumerable.Empty<IDisposable>());
        }

        public string Type { get; }

        public string Kind { get; }

        public string? Key { get; }

        public string Id { get; internal set; } = string.Empty;

        public string? ParentId { get; internal set; }

        public IReadOnlyDictionary<string, object?> Properties { get; }

        public IReadOnlyDictionary<string, object?> Events { get; }

        public IReadOnlyDictionary<string, object?> Animations { get; }

        public IReadOnlyList<VirtualEntry> Children => _children;

        public IReadOnlyList<IDisposable> Owners => _owners;

        public VirtualEntry WithOwner(IDisposable owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (!_owners.Any(existing => ReferenceEquals(existing, owner)))
                _owners.Add(owner);

            return this;
        }

        public VirtualEntry WithIdentity(string id, string? parentId, bool rewriteChildren = true)
        {
            Id = id;
            ParentId = parentId;

            if (!rewriteChildren)
                return this;

            for (var i = 0; i < Children.Count; i++)
            {
                var childId = string.IsNullOrEmpty(Children[i].Id) ? $"{id}.{i}" : Children[i].Id;
                var childParentId = string.IsNullOrEmpty(Children[i].ParentId) ? id : Children[i].ParentId;
                Children[i].WithIdentity(childId, childParentId);
            }

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

            foreach (var child in _children)
            {
                var result = child.FindById(id);
                if (result != null)
                    return result;
            }

            return null;
        }

        public bool ReplaceDescendant(string id, VirtualEntry replacement)
        {
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

        private static Dictionary<string, TValue> ToDictionary<TValue>(IEnumerable<KeyValuePair<string, TValue>> values)
        {
            var dictionary = new Dictionary<string, TValue>();
            foreach (var value in values)
            {
                dictionary[value.Key] = value.Value;
            }

            return dictionary;
        }
    }
}
