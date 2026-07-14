using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Nuri.UI.Controls;

namespace Nuri.WPF
{
    internal static class WpfControlRegistry
    {
        public static FrameworkElement Create(Nuri.VirtualDom.VirtualEntry entry)
        {
            return Create(entry.Type, entry.Kind);
        }

        public static FrameworkElement Create(string type, string? kind = null)
        {
            kind ??= string.Empty;
            switch (type)
            {
                case VirtualControlTypes.Div:
                    return CreateDivElement(kind);
                case VirtualControlTypes.Window:
                    return new DivElement(new Grid());
                case VirtualControlTypes.Image:
                    return new Image();
                case VirtualControlTypes.Input:
                    return kind switch
                    {
                        InputTypes.Button => new Button(),
                        InputTypes.Checkbox => new CheckBox(),
                        InputTypes.Destructive => new Button(),
                        InputTypes.Link => new Button(),
                        InputTypes.Password => new PasswordBox(),
                        InputTypes.Primary => new Button(),
                        InputTypes.Radio => new RadioButton(),
                        InputTypes.Secondary => new Button(),
                        InputTypes.Submit => new Button(),
                        InputTypes.Toggle => new System.Windows.Controls.Primitives.ToggleButton(),
                        _ => new TextBox()
                    };
                case VirtualControlTypes.Items:
                    return kind switch
                    {
                        ItemsTypes.Table => new DataGrid(),
                        ItemsTypes.Tree => new TreeView(),
                        ItemsTypes.Virtualized => new WpfVirtualizedItemsHost(),
                        _ => new ItemsControl()
                    };
                case VirtualControlTypes.Overlay:
                    return new DivElement(new System.Windows.Controls.Grid());
                case VirtualControlTypes.Select:
                    return kind == SelectTypes.Multi ? new ListBox { SelectionMode = SelectionMode.Multiple } : new ComboBox();
                case VirtualControlTypes.Text:
                    return new TextBlock();
                default:
                    throw new InvalidOperationException($"Unknown WPF element type: {type}");
            }
        }

        public static FrameworkElement GetPropertyTarget(FrameworkElement element, string propertyName)
        {
            if (element is DivElement div && IsHostProperty(propertyName))
                return div.ChildHost;

            return element;
        }

        public static void AddChild(FrameworkElement parent, FrameworkElement child, int? index = null)
        {
            parent = GetChildHost(parent);

            if (parent is System.Windows.Controls.Panel panel)
            {
                if (index >= 0 && index <= panel.Children.Count)
                    panel.Children.Insert(index.Value, child);
                else
                    panel.Children.Add(child);

                return;
            }

            if (parent is System.Windows.Controls.ItemsControl itemsControl)
            {
                if (index >= 0 && index <= itemsControl.Items.Count)
                    itemsControl.Items.Insert(index.Value, child);
                else
                    itemsControl.Items.Add(child);

                return;
            }

            if (parent is System.Windows.Controls.HeaderedContentControl headeredContentControl)
            {
                headeredContentControl.Content = child;
                return;
            }

            if (parent is System.Windows.Controls.ContentControl contentControl)
            {
                contentControl.Content = child;
                return;
            }

            if (parent is System.Windows.Controls.Decorator decorator)
            {
                decorator.Child = child;
                return;
            }

            throw new InvalidOperationException($"Element '{parent.GetType().Name}' cannot contain child elements.");
        }

        public static void RemoveChild(FrameworkElement parent, FrameworkElement child)
        {
            parent = GetChildHost(parent);

            if (parent is System.Windows.Controls.Panel panel)
            {
                panel.Children.Remove(child);
                return;
            }

            if (parent is System.Windows.Controls.ItemsControl itemsControl)
            {
                itemsControl.Items.Remove(child);
                return;
            }

            if (parent is System.Windows.Controls.HeaderedContentControl headeredContentControl && headeredContentControl.Content == child)
            {
                headeredContentControl.Content = null;
                return;
            }

            if (parent is System.Windows.Controls.ContentControl contentControl && contentControl.Content == child)
            {
                contentControl.Content = null;
                return;
            }

            if (parent is System.Windows.Controls.Decorator decorator && decorator.Child == child)
            {
                decorator.Child = null;
            }
        }

        public static void MoveChild(FrameworkElement parent, FrameworkElement child, int newIndex)
        {
            parent = GetChildHost(parent);

            if (parent is System.Windows.Controls.Panel panel)
            {
                var oldIndex = panel.Children.IndexOf(child);
                if (oldIndex < 0 || oldIndex == newIndex)
                    return;

                panel.Children.RemoveAt(oldIndex);
                if (newIndex >= 0 && newIndex <= panel.Children.Count)
                    panel.Children.Insert(newIndex, child);
                else
                    panel.Children.Add(child);

                return;
            }

            if (parent is System.Windows.Controls.ItemsControl itemsControl)
            {
                var oldIndex = itemsControl.Items.IndexOf(child);
                if (oldIndex < 0 || oldIndex == newIndex)
                    return;

                itemsControl.Items.RemoveAt(oldIndex);
                if (newIndex >= 0 && newIndex <= itemsControl.Items.Count)
                    itemsControl.Items.Insert(newIndex, child);
                else
                    itemsControl.Items.Add(child);
            }
        }

        public static void ReplaceChild(FrameworkElement parent, FrameworkElement oldChild, FrameworkElement newChild)
        {
            parent = GetChildHost(parent);

            if (parent is System.Windows.Controls.Panel panel)
            {
                var index = panel.Children.IndexOf(oldChild);
                if (index >= 0)
                {
                    panel.Children.RemoveAt(index);
                    panel.Children.Insert(index, newChild);
                }

                return;
            }

            if (parent is System.Windows.Controls.ItemsControl itemsControl)
            {
                var index = itemsControl.Items.IndexOf(oldChild);
                if (index >= 0)
                {
                    itemsControl.Items.RemoveAt(index);
                    itemsControl.Items.Insert(index, newChild);
                }

                return;
            }

            if (parent is System.Windows.Controls.HeaderedContentControl headeredContentControl && headeredContentControl.Content == oldChild)
            {
                headeredContentControl.Content = newChild;
                return;
            }

            if (parent is System.Windows.Controls.ContentControl contentControl && contentControl.Content == oldChild)
            {
                contentControl.Content = newChild;
                return;
            }

            if (parent is System.Windows.Controls.Decorator decorator && decorator.Child == oldChild)
            {
                decorator.Child = newChild;
                return;
            }

            throw new InvalidOperationException($"Unsupported parent type for replace: {parent.GetType().Name}");
        }

        private static FrameworkElement GetChildHost(FrameworkElement element)
        {
            return element is DivElement div ? div.ChildHost : element;
        }

        private static FrameworkElement CreateDivHost(string kind)
        {
            switch (kind)
            {
                case DivTypes.Grid:
                    return new System.Windows.Controls.Grid();
                case DivTypes.Row:
                    return new StackPanel { Orientation = Orientation.Horizontal };
                case DivTypes.Wrap:
                    return new WrapPanel();
                case DivTypes.Scroll:
                    return new StackPanel { Orientation = Orientation.Vertical };
                case DivTypes.Block:
                    return new System.Windows.Controls.Grid();
                case DivTypes.Column:
                case "":
                    return new StackPanel { Orientation = Orientation.Vertical };
                default:
                    throw new InvalidOperationException($"Unknown Div kind: {kind}");
            }
        }

        private static DivElement CreateDivElement(string kind)
        {
            if (kind == DivTypes.Scroll)
            {
                var childHost = new StackPanel { Orientation = Orientation.Vertical };
                var scrollViewer = new ScrollViewer
                {
                    Content = childHost,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };

                return new DivElement(scrollViewer, childHost);
            }

            return new DivElement(CreateDivHost(kind));
        }

        private static bool IsHostProperty(string propertyName)
        {
            return propertyName == "Orientation"
                || propertyName == "RowDefinitions"
                || propertyName == "ColumnDefinitions";
        }
    }

    internal sealed class DivElement : Border
    {
        public DivElement(FrameworkElement childHost)
            : this(childHost, childHost)
        {
        }

        public DivElement(FrameworkElement visualRoot, FrameworkElement childHost)
        {
            ChildHost = childHost;
            Child = visualRoot;
        }

        public FrameworkElement ChildHost { get; }
    }
}
