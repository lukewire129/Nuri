using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nuri.UI.Values;
using Nuri.Constants;
using Nuri.UI.Virtualization;

namespace Nuri.VirtualDom
{
    public static class VirtualTreeDiff
    {
        public static IReadOnlyList<PatchOperation> Diff(VirtualEntry oldEntry, VirtualEntry newEntry)
        {
            if (oldEntry == null)
                throw new ArgumentNullException(nameof(oldEntry));

            if (newEntry == null)
                throw new ArgumentNullException(nameof(newEntry));

            oldEntry.WithIdentity(string.IsNullOrEmpty(oldEntry.Id) ? "0" : oldEntry.Id, oldEntry.ParentId);
            newEntry.WithIdentity(string.IsNullOrEmpty(newEntry.Id) ? "0" : newEntry.Id, newEntry.ParentId);

            var operations = new List<PatchOperation>();
            DiffInto(oldEntry, newEntry, operations);
            return operations;
        }

        private static void DiffInto(VirtualEntry oldEntry, VirtualEntry newEntry, List<PatchOperation> operations)
        {
            if (!oldEntry.IsSameEntry(newEntry))
            {
                operations.Add(new ReplaceEntryPatch(oldEntry, newEntry));
                return;
            }

            DiffProperties(oldEntry, newEntry, operations);
            DiffEvents(oldEntry, newEntry, operations);
            DiffAnimations(oldEntry, newEntry, operations);
            DiffChildren(oldEntry, newEntry, operations);
        }

        private static void DiffProperties(VirtualEntry oldEntry, VirtualEntry newEntry, List<PatchOperation> operations)
        {
            foreach (var property in newEntry.Properties)
            {
                if (property.Key == PropertyKeys.VirtualizedItemsSource
                    && property.Value is IVirtualizedItemsSource newSource)
                {
                    if (oldEntry.Properties.TryGetValue(property.Key, out var oldSourceValue)
                        && oldSourceValue is IVirtualizedItemsSource oldSource)
                    {
                        operations.Add(CreateVirtualizedItemsPatch(newEntry, oldSource, newSource));
                    }
                    else
                    {
                        operations.Add(new UpdatePropertyPatch(newEntry, property.Key, property.Value));
                    }

                    continue;
                }

                if (!oldEntry.Properties.TryGetValue(property.Key, out var oldValue) || !ValuesEqual(oldValue, property.Value))
                    operations.Add(new UpdatePropertyPatch(newEntry, property.Key, property.Value));
            }

            foreach (var property in oldEntry.Properties)
            {
                if (!newEntry.Properties.ContainsKey(property.Key))
                    operations.Add(new RemovePropertyPatch(oldEntry, property.Key));
            }
        }

        private static UpdateVirtualizedItemsPatch CreateVirtualizedItemsPatch(
            VirtualEntry target,
            IVirtualizedItemsSource oldSource,
            IVirtualizedItemsSource newSource)
        {
            var oldIdentities = oldSource.GetIdentities();
            var newIdentities = newSource.GetIdentities();
            var oldIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            var newIndexes = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var index = 0; index < oldIdentities.Count; index++)
                oldIndexes[oldIdentities[index]] = index;
            for (var index = 0; index < newIdentities.Count; index++)
                newIndexes[newIdentities[index]] = index;

            var changes = new List<VirtualizedItemChange>();
            for (var oldIndex = 0; oldIndex < oldIdentities.Count; oldIndex++)
            {
                var identity = oldIdentities[oldIndex];
                if (!newIndexes.ContainsKey(identity))
                    changes.Add(new VirtualizedItemChange(VirtualizedItemChangeType.Remove, identity, oldIndex, -1));
            }

            for (var newIndex = 0; newIndex < newIdentities.Count; newIndex++)
            {
                var identity = newIdentities[newIndex];
                if (!oldIndexes.TryGetValue(identity, out var oldIndex))
                {
                    changes.Add(new VirtualizedItemChange(VirtualizedItemChangeType.Add, identity, -1, newIndex));
                    continue;
                }

                if (oldIndex != newIndex)
                    changes.Add(new VirtualizedItemChange(VirtualizedItemChangeType.Move, identity, oldIndex, newIndex));
                if (!newSource.ItemsEqual(newIndex, oldSource, oldIndex))
                    changes.Add(new VirtualizedItemChange(VirtualizedItemChangeType.Update, identity, oldIndex, newIndex));
            }

            return new UpdateVirtualizedItemsPatch(
                target,
                newSource,
                changes,
                !newSource.HasSameTemplate(oldSource)
                    || newSource.ItemExtent != oldSource.ItemExtent
                    || newSource.MeasuresItemExtent != oldSource.MeasuresItemExtent);
        }

        private static void DiffEvents(VirtualEntry oldEntry, VirtualEntry newEntry, List<PatchOperation> operations)
        {
            foreach (var oldEvent in oldEntry.Events)
            {
                if (!newEntry.Events.TryGetValue(oldEvent.Key, out var newHandler) || !Equals(oldEvent.Value, newHandler))
                    operations.Add(new RemoveEventPatch(oldEntry, oldEvent));
            }

            foreach (var newEvent in newEntry.Events)
            {
                if (!oldEntry.Events.TryGetValue(newEvent.Key, out var oldHandler) || !Equals(oldHandler, newEvent.Value))
                    operations.Add(new AddEventPatch(newEntry, newEvent));
            }
        }

        private static void DiffAnimations(VirtualEntry oldEntry, VirtualEntry newEntry, List<PatchOperation> operations)
        {
            foreach (var oldAnimation in oldEntry.Animations)
            {
                if (!newEntry.Animations.TryGetValue(oldAnimation.Key, out var newAnimation) || !ValuesEqual(oldAnimation.Value, newAnimation))
                    operations.Add(new RemoveAnimationPatch(oldEntry, oldAnimation));
            }

            foreach (var newAnimation in newEntry.Animations)
            {
                if (!oldEntry.Animations.TryGetValue(newAnimation.Key, out var oldAnimation) || !ValuesEqual(oldAnimation, newAnimation.Value))
                    operations.Add(new AddAnimationPatch(newEntry, WithPreviousPropertyValue(oldEntry, newAnimation)));
            }
        }

        private static KeyValuePair<string, object?> WithPreviousPropertyValue(VirtualEntry oldEntry, KeyValuePair<string, object?> animation)
        {
            if (animation.Value is not AnimationValue animationValue)
                return animation;

            if (!oldEntry.Properties.TryGetValue(animationValue.PropertyName, out var previousValue))
                return animation;

            return new KeyValuePair<string, object?>(
                animation.Key,
                new AnimationValue(
                    animationValue.PropertyName,
                    animationValue.To,
                    animationValue.Duration,
                    animationValue.Easing,
                    previousValue));
        }

        private static void DiffChildren(VirtualEntry oldEntry, VirtualEntry newEntry, List<PatchOperation> operations)
        {
            if (CanUseKeyedDiff(oldEntry.Children, oldEntry) && CanUseKeyedDiff(newEntry.Children, newEntry))
            {
                DiffKeyedChildren(oldEntry, newEntry, operations);
                return;
            }

            var max = Math.Max(oldEntry.Children.Count, newEntry.Children.Count);

            for (var index = 0; index < max; index++)
            {
                if (index >= oldEntry.Children.Count)
                {
                    operations.Add(new AddChildPatch(newEntry, newEntry.Children[index], index));
                    continue;
                }

                if (index >= newEntry.Children.Count)
                {
                    operations.Add(new RemoveChildPatch(oldEntry, oldEntry.Children[index], index));
                    continue;
                }

                DiffInto(oldEntry.Children[index], newEntry.Children[index], operations);
            }
        }

        private static bool CanUseKeyedDiff(IReadOnlyList<VirtualEntry> children, VirtualEntry parent)
        {
            if (children.Count == 0)
                return false;

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var child in children)
            {
                if (string.IsNullOrEmpty(child.Key))
                    return false;

                if (!keys.Add(child.Key!))
                {
                    Debug.WriteLine($"Duplicate virtual key '{child.Key}' under parent '{parent.Id}'. Falling back to index-based diff.");
                    return false;
                }
            }

            return true;
        }

        private static void DiffKeyedChildren(VirtualEntry oldEntry, VirtualEntry newEntry, List<PatchOperation> operations)
        {
            var oldByKey = new Dictionary<string, (VirtualEntry Entry, int Index)>(StringComparer.Ordinal);
            for (var i = 0; i < oldEntry.Children.Count; i++)
                oldByKey[oldEntry.Children[i].Key!] = (oldEntry.Children[i], i);

            var newKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var newIndex = 0; newIndex < newEntry.Children.Count; newIndex++)
                newKeys.Add(newEntry.Children[newIndex].Key!);

            for (var oldIndex = 0; oldIndex < oldEntry.Children.Count; oldIndex++)
            {
                var oldChild = oldEntry.Children[oldIndex];
                if (!newKeys.Contains(oldChild.Key!))
                    operations.Add(new RemoveChildPatch(oldEntry, oldChild, oldIndex));
            }

            var retainedIds = new HashSet<string>(oldEntry.Children
                .Where(child => newKeys.Contains(child.Key!))
                .Select(child => child.Id), StringComparer.Ordinal);

            var hasReorder = false;
            for (var newIndex = 0; newIndex < newEntry.Children.Count; newIndex++)
            {
                var newChild = newEntry.Children[newIndex];

                if (!oldByKey.TryGetValue(newChild.Key!, out var oldMatch))
                {
                    var childId = CreateKeyedChildId(oldEntry.Id, newChild.Key!, retainedIds);
                    newChild.RewriteIdentity(childId, oldEntry.Id);
                    retainedIds.Add(childId);
                    operations.Add(new AddChildPatch(newEntry, newChild, newIndex));
                    continue;
                }

                newChild.RewriteIdentity(oldMatch.Entry.Id, oldMatch.Entry.ParentId);
                DiffInto(oldMatch.Entry, newChild, operations);
                hasReorder |= oldMatch.Index != newIndex;
            }

            if (!hasReorder)
                return;

            var retainedChildren = new List<(VirtualEntry OldChild, int OldIndex, int NewIndex)>(newEntry.Children.Count);
            for (var newIndex = 0; newIndex < newEntry.Children.Count; newIndex++)
            {
                var newChild = newEntry.Children[newIndex];
                if (oldByKey.TryGetValue(newChild.Key!, out var oldMatch))
                    retainedChildren.Add((oldMatch.Entry, oldMatch.Index, newIndex));
            }

            var stablePositions = FindStablePositions(retainedChildren);
            for (var i = 0; i < retainedChildren.Count; i++)
            {
                var retainedChild = retainedChildren[i];
                if (!stablePositions[i] && retainedChild.OldIndex != retainedChild.NewIndex)
                    operations.Add(new MoveChildPatch(newEntry, retainedChild.OldChild, retainedChild.OldIndex, retainedChild.NewIndex));
            }
        }

        private static string CreateKeyedChildId(string parentId, string key, HashSet<string> usedIds)
        {
            var id = $"{parentId}#key:{key}";
            if (usedIds.Add(id))
                return id;

            var index = 1;
            while (!usedIds.Add($"{id}:{index}"))
                index++;

            return $"{id}:{index}";
        }

        private static bool[] FindStablePositions(IReadOnlyList<(VirtualEntry OldChild, int OldIndex, int NewIndex)> children)
        {
            var stable = new bool[children.Count];
            if (children.Count == 0)
                return stable;

            var tails = new int[children.Count];
            var previous = new int[children.Count];
            var length = 0;

            for (var i = 0; i < children.Count; i++)
            {
                var value = children[i].OldIndex;
                var low = 0;
                var high = length;

                while (low < high)
                {
                    var middle = (low + high) / 2;
                    if (children[tails[middle]].OldIndex < value)
                        low = middle + 1;
                    else
                        high = middle;
                }

                previous[i] = low > 0 ? tails[low - 1] : -1;
                tails[low] = i;

                if (low == length)
                    length++;
            }

            for (var index = tails[length - 1]; index >= 0; index = previous[index])
                stable[index] = true;

            return stable;
        }

        private static bool ValuesEqual(object? oldValue, object? newValue)
        {
            if (oldValue is System.Collections.IEnumerable oldEnumerable
                && newValue is System.Collections.IEnumerable newEnumerable
                && oldValue is not string
                && newValue is not string)
            {
                return oldEnumerable.Cast<object?>().SequenceEqual(newEnumerable.Cast<object?>());
            }

            return Equals(oldValue, newValue);
        }
    }
}
