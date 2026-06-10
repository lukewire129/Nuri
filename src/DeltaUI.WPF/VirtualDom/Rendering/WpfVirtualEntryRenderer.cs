using DeltaUI.Core.VirtualDom;
using DeltaUI.Core.UI.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DeltaUI.WPF
{
    public static class WpfVirtualEntryRenderer
    {
        private static readonly ConditionalWeakTable<FrameworkElement, Dictionary<string, FrameworkElement>> ControlIndexes = new ConditionalWeakTable<FrameworkElement, Dictionary<string, FrameworkElement>>();

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
                    default:
                        throw new InvalidOperationException($"Unknown patch operation: {operation.GetType().Name}");
                }
            }
        }

        public static FrameworkElement Build(VirtualEntry entry)
        {
            var element = CreateElement(entry);
            ApplyProperties(element, entry.Properties);
            ApplyEvents(element, entry);
            ApplyChildren(element, entry);

            return element;
        }

        private static FrameworkElement CreateElement(VirtualEntry entry)
        {
            var element = WpfControlRegistry.Create(entry);
            element.SetUniqueId(entry.Id);
            return element;
        }

        private static void ApplyProperties(FrameworkElement element, IReadOnlyDictionary<string, object?> properties)
        {
            foreach (var property in properties)
            {
                SetProperty(element, property.Key, property.Value);
            }
        }

        private static void ApplyEvents(FrameworkElement element, VirtualEntry entry)
        {
            foreach (var evt in entry.Events)
            {
                var eventInfo = element.GetType().GetEvent(evt.Key);
                eventInfo?.AddEventHandler(element, evt.Value);
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

            SetProperty(target, operation.PropertyName, operation.Value);
        }

        private static void RemoveProperty(Dictionary<string, FrameworkElement> controlIndex, RemovePropertyPatch operation)
        {
            if (!controlIndex.TryGetValue(operation.Target.Id, out var target))
                return;

            var propertyTarget = WpfControlRegistry.GetPropertyTarget(target, operation.PropertyName);

            if (propertyTarget.ResetAttachedProperty(operation.PropertyName))
                return;

            if (WpfPropertyMapper.TryResetProperty(propertyTarget, operation.PropertyName))
                return;

            var property = propertyTarget.GetType().GetProperty(operation.PropertyName);
            if (property != null && property.CanWrite)
            {
                var defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                property.SetValue(propertyTarget, defaultValue);
            }
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
            var eventInfo = target?.GetType().GetEvent(operation.Event.Key);
            eventInfo?.RemoveEventHandler(target, operation.Event.Value);
        }

        private static void AddEvent(Dictionary<string, FrameworkElement> controlIndex, AddEventPatch operation)
        {
            controlIndex.TryGetValue(operation.Target.Id, out var target);
            var eventInfo = target?.GetType().GetEvent(operation.Event.Key);
            eventInfo?.AddEventHandler(target, operation.Event.Value);
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

            var property = propertyTarget.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(propertyTarget, value);
            }
            else
            {
                if (value != null)
                    propertyTarget.UpdateAttachedProperty(propertyName, value);
            }
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

            if (propertyName == "Background" && TryBeginBackgroundAnimation(element, animation))
            {
                return;
            }

            if (propertyName == "Foreground" && TryBeginForegroundAnimation(element, animation))
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
