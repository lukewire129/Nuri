using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.Avalonia
{
    public static class AvaloniaVirtualEntryRenderer
    {
        private static readonly ConditionalWeakTable<Control, Dictionary<string, Control>> ControlIndexes = new ConditionalWeakTable<Control, Dictionary<string, Control>>();
        private static readonly ConditionalWeakTable<Control, Dictionary<string, Delegate>> EventHandlers = new ConditionalWeakTable<Control, Dictionary<string, Delegate>>();
        private static readonly ConditionalWeakTable<Control, Dictionary<string, ITransition>> ActiveTransitions = new ConditionalWeakTable<Control, Dictionary<string, ITransition>>();

        public static Control Build(VirtualEntry entry)
        {
            var control = AvaloniaControlRegistry.Create(entry);
            control.Tag = entry.Id;

            foreach (var property in entry.Properties)
                AvaloniaPropertyMapper.TrySetProperty(control, property.Key, property.Value);

            foreach (var evt in entry.Events)
                AddEventHandler(control, evt);

            foreach (var child in entry.Children)
                AvaloniaControlRegistry.AddChild(control, Build(child));

            return control;
        }

        public static void ApplyDiff(Control root, IReadOnlyList<PatchOperation> operations)
        {
            var controlIndex = ControlIndexes.GetValue(root, BuildControlIndex);
            PrepareAnimations(controlIndex, operations);

            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case ReplaceEntryPatch replace:
                        ReplaceEntry(controlIndex, replace);
                        break;
                    case UpdatePropertyPatch updateProperty:
                        if (controlIndex.TryGetValue(updateProperty.Target.Id, out var updateTarget))
                            AvaloniaPropertyMapper.TrySetProperty(updateTarget, updateProperty.PropertyName, updateProperty.Value);
                        break;
                    case RemovePropertyPatch removeProperty:
                        if (controlIndex.TryGetValue(removeProperty.Target.Id, out var removeTarget))
                            AvaloniaPropertyMapper.TryResetProperty(removeTarget, removeProperty.PropertyName);
                        break;
                    case AddChildPatch addChild:
                        AddChild(controlIndex, addChild);
                        break;
                    case RemoveChildPatch removeChild:
                        RemoveChild(controlIndex, removeChild);
                        break;
                    case MoveChildPatch moveChild:
                        if (controlIndex.TryGetValue(moveChild.Parent.Id, out var moveParent) && controlIndex.TryGetValue(moveChild.Child.Id, out var moveChildControl))
                            AvaloniaControlRegistry.MoveChild(moveParent, moveChildControl, moveChild.NewIndex);
                        break;
                    case AddEventPatch addEvent:
                        if (controlIndex.TryGetValue(addEvent.Target.Id, out var addEventTarget))
                            AddEventHandler(addEventTarget, addEvent.Event);
                        break;
                    case RemoveEventPatch removeEvent:
                        if (controlIndex.TryGetValue(removeEvent.Target.Id, out var removeEventTarget))
                            RemoveEventHandler(removeEventTarget, removeEvent.Event);
                        break;
                    case AddAnimationPatch:
                    case RemoveAnimationPatch:
                        break;
                }
            }
        }

        private static void PrepareAnimations(Dictionary<string, Control> controlIndex, IReadOnlyList<PatchOperation> operations)
        {
            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case RemoveAnimationPatch removeAnimation:
                        if (controlIndex.TryGetValue(removeAnimation.Target.Id, out var removeTarget))
                            RemoveAnimation(removeTarget, removeAnimation.Animation.Key);
                        break;
                    case AddAnimationPatch addAnimation:
                        if (controlIndex.TryGetValue(addAnimation.Target.Id, out var addTarget)
                            && addAnimation.Animation.Value is AnimationValue animationValue)
                        {
                            AddAnimation(addTarget, addAnimation.Animation.Key, animationValue);
                        }
                        break;
                }
            }
        }

        private static void AddAnimation(Control control, string animationName, AnimationValue animation)
        {
            ITransition transition = animation.PropertyName switch
            {
                "Opacity" => new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = animation.Duration,
                    Easing = ToAvaloniaEasing(animation.Easing)
                },
                _ => throw new NotSupportedException($"The property '{animation.PropertyName}' is not supported for Avalonia animation.")
            };

            RemoveAnimation(control, animationName);

            control.Transitions ??= new Transitions();
            control.Transitions.Add(transition);
            ActiveTransitions.GetOrCreateValue(control)[animationName] = transition;
        }

        private static void RemoveAnimation(Control control, string animationName)
        {
            var transitions = ActiveTransitions.GetOrCreateValue(control);
            if (!transitions.TryGetValue(animationName, out var transition))
                return;

            control.Transitions?.Remove(transition);
            transitions.Remove(animationName);
        }

        private static Easing ToAvaloniaEasing(EasingValue? easing)
        {
            if (easing == null)
                return new LinearEasing();

            return easing.Mode switch
            {
                EasingModeValue.In => new CubicEaseIn(),
                EasingModeValue.Out => new CubicEaseOut(),
                _ => new CubicEaseInOut()
            };
        }

        private static void ReplaceEntry(Dictionary<string, Control> controlIndex, ReplaceEntryPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.OldEntry.Id, out var target))
                return;

            if (target.Parent is not Control parent)
                return;

            var replacement = Build(operation.NewEntry);
            RemoveFromIndex(controlIndex, target);
            AvaloniaControlRegistry.ReplaceChild(parent, target, replacement);
            AddToIndex(controlIndex, replacement);
        }

        private static void AddChild(Dictionary<string, Control> controlIndex, AddChildPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Parent.Id, out var parent))
                return;

            var child = Build(operation.Child);
            AvaloniaControlRegistry.AddChild(parent, child, operation.Index);
            AddToIndex(controlIndex, child);
        }

        private static void RemoveChild(Dictionary<string, Control> controlIndex, RemoveChildPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Parent.Id, out var parent) || !controlIndex.TryGetValue(operation.Child.Id, out var child))
                return;

            RemoveFromIndex(controlIndex, child);
            AvaloniaControlRegistry.RemoveChild(parent, child);
        }

        private static void AddEventHandler(Control control, KeyValuePair<string, object?> evt)
        {
            if (!AvaloniaEventMapper.TryAttach(control, evt.Key, evt.Value, out var handlerKey, out var handler))
                return;

            EventHandlers.GetOrCreateValue(control)[handlerKey] = handler;
        }

        private static void RemoveEventHandler(Control control, KeyValuePair<string, object?> evt)
        {
            var key = AvaloniaEventMapper.GetHandlerKey(evt.Key, evt.Value);
            var handlers = EventHandlers.GetOrCreateValue(control);
            if (!handlers.TryGetValue(key, out var handler))
                return;

            AvaloniaEventMapper.Detach(control, evt.Key, handler);
            handlers.Remove(key);
        }

        private static Dictionary<string, Control> BuildControlIndex(Control root)
        {
            var index = new Dictionary<string, Control>(StringComparer.Ordinal);
            AddToIndex(index, root);
            return index;
        }

        private static void AddToIndex(Dictionary<string, Control> index, Control control)
        {
            if (control.Tag is string id && !string.IsNullOrEmpty(id))
                index[id] = control;

            foreach (var child in AvaloniaControlRegistry.GetChildren(control))
                AddToIndex(index, child);
        }

        private static void RemoveFromIndex(Dictionary<string, Control> index, Control control)
        {
            if (control.Tag is string id && !string.IsNullOrEmpty(id))
                index.Remove(id);

            foreach (var child in AvaloniaControlRegistry.GetChildren(control).ToArray())
                RemoveFromIndex(index, child);
        }
    }
}
