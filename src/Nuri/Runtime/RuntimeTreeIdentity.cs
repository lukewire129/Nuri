using System;
using System.Collections.Generic;

namespace Nuri.Runtime
{
    internal static class RuntimeTreeIdentity
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, RuntimeTreeNode> Nodes = new Dictionary<string, RuntimeTreeNode>(StringComparer.Ordinal);

        internal static int RegisteredNodeCount
        {
            get
            {
                lock (SyncRoot)
                    return Nodes.Count;
            }
        }

        public static RuntimeTreeNode Register(string id, string? parentId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Runtime node id must not be empty.", nameof(id));

            lock (SyncRoot)
            {
                if (Nodes.TryGetValue(id, out var existing))
                {
                    existing.ParentId = parentId;
                    return existing;
                }

                var node = new RuntimeTreeNode(id, parentId);
                Nodes[id] = node;
                return node;
            }
        }

        public static RuntimeTreeNode GetNode(string id)
        {
            lock (SyncRoot)
            {
                if (!Nodes.TryGetValue(id, out var node))
                {
                    node = new RuntimeTreeNode(id, null);
                    Nodes[id] = node;
                }

                return node;
            }
        }

        public static void Unregister(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            lock (SyncRoot)
            {
                Nodes.Remove(id);
            }
        }

        public static IReadOnlyList<RuntimeTreeNode> GetSubtreeNodes(string rootId)
        {
            lock (SyncRoot)
            {
                var result = new List<RuntimeTreeNode>();
                foreach (var node in Nodes.Values)
                {
                    if (IsDescendantOrSelf(node.Id, rootId))
                        result.Add(node);
                }

                return result;
            }
        }

        public static bool IsDescendantOrSelf(string id, string rootId)
        {
            if (string.Equals(id, rootId, StringComparison.Ordinal))
                return true;

            lock (SyncRoot)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal);
                var currentId = id;
                while (Nodes.TryGetValue(currentId, out var node)
                    && !string.IsNullOrWhiteSpace(node.ParentId)
                    && visited.Add(currentId))
                {
                    if (string.Equals(node.ParentId, rootId, StringComparison.Ordinal))
                        return true;

                    currentId = node.ParentId!;
                }
            }

            return false;
        }

        public static int GetDepth(string id)
        {
            lock (SyncRoot)
            {
                var depth = 0;
                var visited = new HashSet<string>(StringComparer.Ordinal);
                var currentId = id;
                while (Nodes.TryGetValue(currentId, out var node)
                    && !string.IsNullOrWhiteSpace(node.ParentId)
                    && visited.Add(currentId))
                {
                    depth++;
                    currentId = node.ParentId!;
                }

                return depth;
            }
        }

        public static bool HasAncestorInSet(string id, HashSet<string> candidateAncestorIds)
        {
            if (candidateAncestorIds == null || candidateAncestorIds.Count == 0)
                return false;

            lock (SyncRoot)
            {
                var currentId = id;
                var remaining = Nodes.Count + 1;
                while (remaining-- > 0
                    && Nodes.TryGetValue(currentId, out var node)
                    && !string.IsNullOrWhiteSpace(node.ParentId))
                {
                    if (candidateAncestorIds.Contains(node.ParentId!))
                        return true;

                    currentId = node.ParentId!;
                }

                return false;
            }
        }

        public static void RemoveSubtree(string rootId)
        {
            lock (SyncRoot)
            {
                var removed = new List<string>();
                foreach (var id in Nodes.Keys)
                {
                    if (IsDescendantOrSelf(id, rootId))
                        removed.Add(id);
                }

                foreach (var id in removed)
                {
                    Nodes.Remove(id);
                }
            }
        }

        internal sealed class RuntimeTreeNode
        {
            public RuntimeTreeNode(string id, string? parentId)
            {
                Id = id;
                ParentId = parentId;
            }

            public string Id { get; }
            public string? ParentId { get; set; }
        }
    }
}
