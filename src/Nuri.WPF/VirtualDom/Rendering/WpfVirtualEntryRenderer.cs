using Nuri.Constants;
using Nuri.UI.Controls;
using Nuri.VirtualDom;
using Nuri.UI.Values;
using Nuri.Runtime.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Nuri.WPF
{
    public static class WpfVirtualEntryRenderer
    {
        private static readonly ConditionalWeakTable<FrameworkElement, Dictionary<string, FrameworkElement>> ControlIndexes = new ConditionalWeakTable<FrameworkElement, Dictionary<string, FrameworkElement>>();
        private static readonly ConditionalWeakTable<FrameworkElement, Dictionary<string, Delegate>> EventHandlers = new ConditionalWeakTable<FrameworkElement, Dictionary<string, Delegate>>();
        private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new ConcurrentDictionary<(Type, string), PropertyInfo?>();
        private static readonly ConcurrentDictionary<(Type Type, string EventName), EventInfo?> EventCache = new ConcurrentDictionary<(Type, string), EventInfo?>();

        public static void ApplyDiff(FrameworkElement root, IReadOnlyList<PatchOperation> operations)
        {
            var controlIndex = ControlIndexes.GetValue(root, BuildControlIndex);

            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case ReplaceEntryPatch replace:
                        ReplaceEntry(controlIndex, replace);
                        break;
                    case UpdatePropertyPatch updateProperty:
                        UpdateProperty(controlIndex, updateProperty);
                        break;
                    case RemovePropertyPatch removeProperty:
                        RemoveProperty(controlIndex, removeProperty);
                        break;
                    case AddChildPatch addChild:
                        AddChild(controlIndex, addChild);
                        break;
                    case RemoveChildPatch removeChild:
                        RemoveChild(controlIndex, removeChild);
                        break;
                    case MoveChildPatch moveChild:
                        MoveChild(controlIndex, moveChild);
                        break;
                    case RemoveEventPatch removeEvent:
                        RemoveEvent(controlIndex, removeEvent);
                        break;
                    case AddEventPatch addEvent:
                        AddEvent(controlIndex, addEvent);
                        break;
                    case RemoveAnimationPatch removeAnimation:
                        RemoveAnimation(controlIndex, removeAnimation);
                        break;
                    case AddAnimationPatch addAnimation:
                        AddAnimation(controlIndex, addAnimation);
                        break;
                    case UpdateVirtualizedItemsPatch updateVirtualizedItems:
                        UpdateVirtualizedItems(controlIndex, updateVirtualizedItems);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown patch operation: {operation.GetType().Name}");
                }
            }
        }

        private static void UpdateVirtualizedItems(
            Dictionary<string, FrameworkElement> controlIndex,
            UpdateVirtualizedItemsPatch operation)
        {
            if (controlIndex.TryGetValue(operation.Target.Id, out var target)
                && target is WpfVirtualizedItemsHost host)
                host.ApplyPatch(operation);
        }

        public static FrameworkElement Build(VirtualEntry entry)
        {
            var element = CreateElement(entry);
            ApplyProperties(element, entry);
            ApplyEvents(element, entry);
            ApplyChildren(element, entry);

            return element;
        }

        public static FrameworkElement? FindElementById(FrameworkElement root, string id)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (string.IsNullOrWhiteSpace(id))
                return null;

            var controlIndex = ControlIndexes.GetValue(root, BuildControlIndex);
            return controlIndex.TryGetValue(id, out var element) ? element : null;
        }

        private static FrameworkElement CreateElement(VirtualEntry entry)
        {
            var element = WpfControlRegistry.Create(entry);
            element.SetUniqueId(entry.Id);
            return element;
        }

        private static void ApplyProperties(FrameworkElement element, VirtualEntry entry)
        {
            foreach (var property in entry.Properties)
            {
                if (IsHostOnlyWindowProperty(entry, property.Key))
                    continue;

                SetProperty(element, property.Key, property.Value);
            }
        }

        private static void ApplyEvents(FrameworkElement element, VirtualEntry entry)
        {
            foreach (var evt in entry.Events)
            {
                AddEventHandler(element, evt);
            }
        }

        private static void ApplyChildren(FrameworkElement element, VirtualEntry entry)
        {
            foreach (var child in entry.Children)
            {
                var childElement = Build(child);

                WpfControlRegistry.AddChild(element, childElement);
            }
        }

        private static void ReplaceEntry(Dictionary<string, FrameworkElement> controlIndex, ReplaceEntryPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.OldEntry.Id, out var target))
                return;

            var replacement = Build(operation.NewEntry);
            var parent = LogicalTreeHelper.GetParent(target);

            if (parent is FrameworkElement parentElement)
            {
                RemoveFromIndex(controlIndex, target);
                WpfControlRegistry.ReplaceChild(parentElement, target, replacement);
                AddToIndex(controlIndex, replacement);
            }
            else
                throw new InvalidOperationException($"Unsupported parent type for replace: {parent?.GetType().Name ?? "null"}");
        }

        private static void UpdateProperty(Dictionary<string, FrameworkElement> controlIndex, UpdatePropertyPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Target.Id, out var target))
                return;

            if (!IsHostOnlyWindowProperty(operation.Target, operation.PropertyName))
                SetProperty(target, operation.PropertyName, operation.Value);
        }

        private static void RemoveProperty(Dictionary<string, FrameworkElement> controlIndex, RemovePropertyPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Target.Id, out var target))
                return;

            if (IsHostOnlyWindowProperty(operation.Target, operation.PropertyName))
                return;

            var propertyTarget = WpfControlRegistry.GetPropertyTarget(target, operation.PropertyName);

            if (propertyTarget.ResetAttachedProperty(operation.PropertyName))
                return;

            if (WpfPropertyMapper.TryResetProperty(propertyTarget, operation.PropertyName))
                return;

            var property = GetCachedProperty(propertyTarget.GetType(), operation.PropertyName);
            if (property != null && property.CanWrite)
            {
                var defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                property.SetValue(propertyTarget, defaultValue);
                return;
            }

            LogUnsupportedProperty(propertyTarget, operation.PropertyName);
        }

        private static void AddChild(Dictionary<string, FrameworkElement> controlIndex, AddChildPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Parent.Id, out var parent))
                return;

            var child = Build(operation.Child);

            WpfControlRegistry.AddChild(parent, child, operation.Index);
            AddToIndex(controlIndex, child);
        }

        private static void RemoveChild(Dictionary<string, FrameworkElement> controlIndex, RemoveChildPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Child.Id, out var child))
                return;

            var parent = LogicalTreeHelper.GetParent(child);
            if (parent is FrameworkElement parentElement)
            {
                RemoveFromIndex(controlIndex, child);
                WpfControlRegistry.RemoveChild(parentElement, child);
            }
        }

        private static void MoveChild(Dictionary<string, FrameworkElement> controlIndex, MoveChildPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Child.Id, out var child))
                return;

            var parent = LogicalTreeHelper.GetParent(child);
            if (parent is FrameworkElement parentElement)
                WpfControlRegistry.MoveChild(parentElement, child, operation.NewIndex);
        }

        private static void RemoveEvent(Dictionary<string, FrameworkElement> controlIndex, RemoveEventPatch operation)
        {
            controlIndex.TryGetValue(operation.Target.Id, out var target);
            if (target == null)
                return;

            RemoveEventHandler(target, operation.Event);
        }

        private static void AddEvent(Dictionary<string, FrameworkElement> controlIndex, AddEventPatch operation)
        {
            controlIndex.TryGetValue(operation.Target.Id, out var target);
            if (target == null)
                return;

            AddEventHandler(target, operation.Event);
        }

        private static void AddEventHandler(FrameworkElement element, KeyValuePair<string, object?> evt)
        {
            if (!WpfEventMapper.TryCreate(evt.Key, evt.Value, out var wpfEventName, out var handler))
            {
                LogUnsupportedEvent(element, evt.Key, null);
                return;
            }

            var eventInfo = GetCachedEvent(element.GetType(), wpfEventName);
            if (eventInfo == null)
            {
                LogUnsupportedEvent(element, evt.Key, wpfEventName);
                return;
            }

            eventInfo.AddEventHandler(element, handler);
            var handlers = EventHandlers.GetOrCreateValue(element);
            handlers[WpfEventMapper.GetHandlerKey(evt.Key, evt.Value)] = handler;
        }

        private static void RemoveEventHandler(FrameworkElement element, KeyValuePair<string, object?> evt)
        {
            if (!WpfEventMapper.TryCreate(evt.Key, evt.Value, out var wpfEventName, out var fallbackHandler))
                return;

            var eventInfo = GetCachedEvent(element.GetType(), wpfEventName);
            if (eventInfo == null)
                return;

            var handlers = EventHandlers.GetOrCreateValue(element);
            var handlerKey = WpfEventMapper.GetHandlerKey(evt.Key, evt.Value);
            var handler = handlers.TryGetValue(handlerKey, out var cachedHandler) ? cachedHandler : fallbackHandler;
            eventInfo.RemoveEventHandler(element, handler);
            handlers.Remove(handlerKey);
        }

        private static void LogUnsupportedEvent(FrameworkElement element, string eventName, string? wpfEventName)
        {
            if (!NuriDiagnostics.IsEnabled)
                return;

            var controlType = element.GetType();
            var controlTypeName = controlType.Name;
            var dedupeKey = $"WPF:UnsupportedEvent:{controlType.FullName}:{eventName}";
            var message = string.IsNullOrWhiteSpace(wpfEventName)
                ? $"WPF event '{eventName}' could not be mapped for '{controlTypeName}'."
                : $"WPF event '{eventName}' maps to '{wpfEventName}', which is not supported by '{controlTypeName}'.";
            NuriDiagnostics.LogOnce(
                RuntimeLogKind.UnsupportedEvent,
                dedupeKey,
                null,
                element.GetUniqueId(),
                message);
        }

        private static void RemoveAnimation(Dictionary<string, FrameworkElement> controlIndex, RemoveAnimationPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Target.Id, out var target))
                return;

            BeginAnimation(target, operation.Animation.Key, null);
        }

        private static void AddAnimation(Dictionary<string, FrameworkElement> controlIndex, AddAnimationPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Target.Id, out var target) || operation.Animation.Value is not AnimationValue animationValue)
                return;

            var animation = WpfValueMapper.ToWpfAnimation(animationValue);
            if (animation == null)
                return;

            BeginAnimation(target, operation.Animation.Key, animation);
        }

        private static void SetProperty(FrameworkElement element, string propertyName, object? value)
        {
            value = WpfValueMapper.ToWpfValue(value);
            var propertyTarget = WpfControlRegistry.GetPropertyTarget(element, propertyName);

            if (WpfPropertyMapper.TrySetProperty(propertyTarget, propertyName, value))
                return;

            var property = GetCachedProperty(propertyTarget.GetType(), propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(propertyTarget, value);
                return;
            }

            if (value != null && propertyTarget.TryUpdateAttachedProperty(propertyName, value))
                return;

            LogUnsupportedProperty(propertyTarget, propertyName);
        }

        private static void LogUnsupportedProperty(FrameworkElement element, string propertyName)
        {
            if (!NuriDiagnostics.IsEnabled)
                return;

            var controlType = element.GetType();
            var controlTypeName = controlType.Name;
            var dedupeKey = $"WPF:UnsupportedProperty:{controlType.FullName}:{propertyName}";
            NuriDiagnostics.LogOnce(
                RuntimeLogKind.UnsupportedProperty,
                dedupeKey,
                null,
                element.GetUniqueId(),
                $"WPF property '{propertyName}' is not supported by '{controlTypeName}'.");
        }

        private static bool IsHostOnlyWindowProperty(VirtualEntry entry, string propertyName)
        {
            return entry.Type == VirtualControlTypes.Window
                && (propertyName == PropertyKeys.Title
                    || propertyName == PropertyKeys.Width
                    || propertyName == PropertyKeys.Height);
        }

        private static PropertyInfo? GetCachedProperty(Type type, string propertyName)
        {
            return PropertyCache.GetOrAdd((type, propertyName), key => key.Type.GetProperty(key.PropertyName));
        }

        private static EventInfo? GetCachedEvent(Type type, string eventName)
        {
            return EventCache.GetOrAdd((type, eventName), key => key.Type.GetEvent(key.EventName));
        }

        private static void BeginAnimation(FrameworkElement element, string propertyName, AnimationTimeline? animation)
        {
            var dependencyProperty = AnimationMapper.GetDependencyProperty(propertyName);
            if (dependencyProperty == null)
                throw new NotSupportedException($"The property '{propertyName}' is not supported for animation.");

            if (propertyName == "Rotate")
            {
                if (!(element.RenderTransform is RotateTransform rotateTransform))
                {
                    rotateTransform = new RotateTransform();
                    element.RenderTransform = rotateTransform;
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
                return;
            }

            if (propertyName == PropertyKeys.Background && TryBeginBackgroundAnimation(element, animation))
            {
                return;
            }

            if (propertyName == PropertyKeys.Foreground && TryBeginForegroundAnimation(element, animation))
            {
                return;
            }

            element.BeginAnimation(dependencyProperty, animation);
        }

        private static bool TryBeginBackgroundAnimation(FrameworkElement element, AnimationTimeline? animation)
        {
            if (element is Control control)
            {
                control.Background = BeginBrushColorAnimation(control.Background, animation);
                return true;
            }

            if (element is System.Windows.Controls.Panel panel)
            {
                panel.Background = BeginBrushColorAnimation(panel.Background, animation);
                return true;
            }

            if (element is System.Windows.Controls.Border border)
            {
                border.Background = BeginBrushColorAnimation(border.Background, animation);
                return true;
            }

            return false;
        }

        private static bool TryBeginForegroundAnimation(FrameworkElement element, AnimationTimeline? animation)
        {
            if (element is Control control)
            {
                control.Foreground = BeginBrushColorAnimation(control.Foreground, animation);
                return true;
            }

            if (element is TextBlock textBlock)
            {
                textBlock.Foreground = BeginBrushColorAnimation(textBlock.Foreground, animation);
                return true;
            }

            return false;
        }

        private static Brush? BeginBrushColorAnimation(Brush? brush, AnimationTimeline? animation)
        {
            if (brush is not SolidColorBrush solidBrush)
            {
                if (animation == null)
                    return brush;

                solidBrush = new SolidColorBrush(Colors.Transparent);
            }

            if (solidBrush.IsFrozen)
                solidBrush = solidBrush.Clone();

            solidBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            return solidBrush;
        }

        private static Dictionary<string, FrameworkElement> BuildControlIndex(FrameworkElement root)
        {
            var index = new Dictionary<string, FrameworkElement>(StringComparer.Ordinal);
            AddToIndex(index, root);
            return index;
        }

        private static void AddToIndex(Dictionary<string, FrameworkElement> index, FrameworkElement element)
        {
            var id = element.GetUniqueId();
            if (!string.IsNullOrEmpty(id))
                index[id] = element;

            foreach (var child in LogicalTreeHelper.GetChildren(element).OfType<FrameworkElement>())
                AddToIndex(index, child);
        }

        private static void RemoveFromIndex(Dictionary<string, FrameworkElement> index, FrameworkElement element)
        {
            var id = element.GetUniqueId();
            if (!string.IsNullOrEmpty(id))
                index.Remove(id);

            foreach (var child in LogicalTreeHelper.GetChildren(element).OfType<FrameworkElement>())
                RemoveFromIndex(index, child);
        }
    }
}
