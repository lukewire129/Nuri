using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Nuri.UI.Controls;
using Nuri.VirtualDom;

namespace Nuri.Avalonia
{
    internal static class AvaloniaControlRegistry
    {
        public static Control Create(VirtualEntry entry)
        {
            return Create(entry.Type, entry.Kind);
        }

        public static Control Create(string type, string? kind = null)
        {
            kind ??= string.Empty;
            switch (type)
            {
                case VirtualControlTypes.Window:
                case VirtualControlTypes.Div:
                    return CreateDiv(kind);
                case VirtualControlTypes.Text:
                    return new TextBlock();
                case VirtualControlTypes.Input:
                    return kind == InputTypes.Button || kind == InputTypes.Primary || kind == InputTypes.Secondary || kind == InputTypes.Submit || kind == InputTypes.Destructive
                        ? new Button()
                        : new TextBox();
                default:
                    return new StackPanel();
            }
        }

        public static void AddChild(Control parent, Control child, int? index = null)
        {
            if (parent is Panel panel)
            {
                if (index >= 0 && index <= panel.Children.Count)
                    panel.Children.Insert(index.Value, child);
                else
                    panel.Children.Add(child);
                return;
            }

            if (parent is ContentControl contentControl)
            {
                contentControl.Content = child;
                return;
            }

            if (parent is Decorator decorator)
            {
                decorator.Child = child;
                return;
            }

            throw new InvalidOperationException($"Element '{parent.GetType().Name}' cannot contain child elements.");
        }

        public static void RemoveChild(Control parent, Control child)
        {
            if (parent is Panel panel)
            {
                panel.Children.Remove(child);
                return;
            }

            if (parent is ContentControl contentControl && ReferenceEquals(contentControl.Content, child))
            {
                contentControl.Content = null;
                return;
            }

            if (parent is Decorator decorator && ReferenceEquals(decorator.Child, child))
                decorator.Child = null;
        }

        public static void MoveChild(Control parent, Control child, int newIndex)
        {
            if (parent is not Panel panel)
                return;

            var oldIndex = panel.Children.IndexOf(child);
            if (oldIndex < 0 || oldIndex == newIndex)
                return;

            panel.Children.RemoveAt(oldIndex);
            if (newIndex >= 0 && newIndex <= panel.Children.Count)
                panel.Children.Insert(newIndex, child);
            else
                panel.Children.Add(child);
        }

        public static void ReplaceChild(Control parent, Control oldChild, Control newChild)
        {
            if (parent is Panel panel)
            {
                var index = panel.Children.IndexOf(oldChild);
                if (index >= 0)
                {
                    panel.Children.RemoveAt(index);
                    panel.Children.Insert(index, newChild);
                }
                return;
            }

            if (parent is ContentControl contentControl && ReferenceEquals(contentControl.Content, oldChild))
            {
                contentControl.Content = newChild;
                return;
            }

            if (parent is Decorator decorator && ReferenceEquals(decorator.Child, oldChild))
                decorator.Child = newChild;
        }

        public static IEnumerable<Control> GetChildren(Control parent)
        {
            if (parent is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Control control)
                        yield return control;
                }
                yield break;
            }

            if (parent is ContentControl contentControl && contentControl.Content is Control content)
                yield return content;

            if (parent is Decorator decorator && decorator.Child is Control decoratorChild)
                yield return decoratorChild;
        }

        private static Control CreateDiv(string kind)
        {
            if (kind == DivTypes.Grid)
                return new Grid();

            if (kind == DivTypes.Row)
                return new AvaloniaDistributedStackPanel(Orientation.Horizontal);

            if (kind == DivTypes.Column || string.IsNullOrEmpty(kind))
                return new AvaloniaDistributedStackPanel(Orientation.Vertical);

            return new StackPanel { Orientation = Orientation.Vertical };
        }
    }
}
