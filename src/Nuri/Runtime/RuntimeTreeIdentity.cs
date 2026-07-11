using System;
using System.Collections.Generic;

namespace Nuri.Runtime
{
    internal static class RuntimeTreeIdentity
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>(StringComparer.Ordinal);

        public static void Register(string id, string? parentId)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            lock (SyncRoot)
                Nodes[id] = new Node(id, parentId);
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
                    Nodes.Remove(id);
            }
        }

        private sealed class Node
        {
            public Node(string id, string? parentId)
            {
                Id = id;
                ParentId = parentId;
            }

            public string Id { get; }
            public string? ParentId { get; }
        }
    }
}
