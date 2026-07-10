using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Nuri.Runtime.Diagnostics;

namespace Nuri.WPF.DevTools
{
    public sealed class DevToolsWindow : Window
    {
        private readonly TreeView _componentTree = new TreeView();
        private readonly TextBlock _selectedComponentTitle = new TextBlock();
        private readonly TextBlock _selectedComponentMetadata = new TextBlock();
        private readonly TextBlock _componentDetails = new TextBlock { TextWrapping = TextWrapping.Wrap };
        private readonly ListBox _hookList = new ListBox();
        private readonly ListBox _storeList = new ListBox();
        private readonly ListView _applicationLogList = new ListView();
        private readonly ListBox _consoleLogList = new ListBox();
        private readonly TextBlock _consoleDetails = new TextBlock { TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _applicationLogStatus = new TextBlock();
        private readonly ColumnDefinition _visualTreeColumn = new ColumnDefinition { Width = new GridLength(320) };
        private readonly ColumnDefinition _visualTreeSplitterColumn = new ColumnDefinition { Width = new GridLength(5) };
        private readonly HashSet<string> _collapsedTreeNodeIds = new HashSet<string>(StringComparer.Ordinal);
        private RuntimeSnapshot _snapshot = NuriDiagnostics.GetSnapshot();
        private string? _selectedComponentId;
        private string? _highlightComponentId;
        private bool _isRefreshingTree;
        private bool _visualTreeCollapsed;
        private Border? _componentTreePanel;
        private Button? _collapsedTreeToggle;

        public DevToolsWindow()
        {
            Title = "Nuri Runtime DevTools";
            Width = 1180;
            Height = 760;
            MinWidth = 920;
            MinHeight = 620;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;
            ApplyMaterialTheme();
            Content = BuildLayout();
            ConfigureApplicationLogList();
            ConfigureConsoleLogList();

            _componentTree.SelectedItemChanged += (_, args) =>
            {
                if (_isRefreshingTree)
                    return;

                if (args.NewValue is TreeViewItem item && item.Tag is ComponentSnapshot component)
                {
                    SelectComponent(component);
                }
                else
                {
                    ClearSelection();
                }
            };
            _componentTree.PreviewMouseDown += (_, args) =>
            {
                var item = FindAncestor<TreeViewItem>(args.OriginalSource as DependencyObject);
                if (item == null)
                {
                    ClearSelection();
                    return;
                }

                if (item.Tag is ComponentSnapshot component)
                    SelectComponent(component);
            };
            _componentTree.PreviewMouseMove += (_, args) =>
            {
                var item = FindAncestor<TreeViewItem>(args.OriginalSource as DependencyObject);
                if (item?.Tag is ComponentSnapshot component)
                    HighlightComponent(component.ComponentId);
                else
                    ClearHighlight();
            };
            _componentTree.MouseLeave += (_, __) => ClearHighlight();
            _componentTree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnTreeItemExpanded));
            _componentTree.AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(OnTreeItemCollapsed));
            PreviewKeyDown += (_, args) =>
            {
                if (args.Key != Key.Escape)
                    return;

                ClearHighlight();
                args.Handled = true;
            };

            NuriDiagnostics.Changed += OnDiagnosticsChanged;
            Closed += (_, __) =>
            {
                NuriDiagnostics.Changed -= OnDiagnosticsChanged;
                WpfElementHighlighter.Clear();
            };

            RefreshSnapshot();
        }

        private UIElement BuildLayout()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbar = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 227, 230)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 12, 20, 10)
            };
            var toolbarContent = new DockPanel();
            var status = new TextBlock
            {
                Text = "F12 to focus  •  Hover a component to highlight it",
                Foreground = new SolidColorBrush(Color.FromRgb(91, 95, 102)),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(status, Dock.Right);
            toolbarContent.Children.Add(status);

            var title = new StackPanel { Orientation = Orientation.Horizontal };
            title.Children.Add(new TextBlock
            {
                Text = "Nuri DevTools",
                FontSize = 19,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 31, 36))
            });
            title.Children.Add(new TextBlock
            {
                Text = "Runtime inspector",
                Margin = new Thickness(10, 4, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(91, 95, 102))
            });
            toolbarContent.Children.Add(title);
            toolbar.Child = toolbarContent;
            root.Children.Add(toolbar);

            var tabs = new TabControl { Margin = new Thickness(14, 10, 14, 14) };
            tabs.Items.Add(CreateTab("Inspector", BuildApplicationLayout()));
            tabs.Items.Add(CreateTab("Console", BuildConsoleLayout()));
            Grid.SetRow(tabs, 1);
            root.Children.Add(tabs);
            return root;
        }

        private UIElement BuildApplicationLayout()
        {
            var root = new Grid { Margin = new Thickness(2) };
            root.ColumnDefinitions.Add(_visualTreeColumn);
            root.ColumnDefinitions.Add(_visualTreeSplitterColumn);
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(190) });

            var treeHost = new Grid();
            _componentTreePanel = CreateSurface(CreateVisualTreeHeader(), _componentTree);
            treeHost.Children.Add(_componentTreePanel);

            _collapsedTreeToggle = CreateIconButton(PackIconKind.Menu, "Show component tree");
            _collapsedTreeToggle.HorizontalAlignment = HorizontalAlignment.Center;
            _collapsedTreeToggle.VerticalAlignment = VerticalAlignment.Top;
            _collapsedTreeToggle.Margin = new Thickness(0, 6, 0, 0);
            _collapsedTreeToggle.Visibility = Visibility.Collapsed;
            _collapsedTreeToggle.Click += (_, __) => ToggleComponentTree();
            treeHost.Children.Add(_collapsedTreeToggle);

            Grid.SetColumn(treeHost, 0);
            Grid.SetRow(treeHost, 0);
            Grid.SetRowSpan(treeHost, 3);
            root.Children.Add(treeHost);

            var verticalSplitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            Grid.SetColumn(verticalSplitter, 1);
            Grid.SetRow(verticalSplitter, 0);
            Grid.SetRowSpan(verticalSplitter, 3);
            root.Children.Add(verticalSplitter);

            var detailTabs = new TabControl { Margin = new Thickness(0, 10, 0, 0) };
            detailTabs.Items.Add(CreateTab("Details", new ScrollViewer { Content = _componentDetails }));
            detailTabs.Items.Add(CreateTab("Hooks", _hookList));
            detailTabs.Items.Add(CreateTab("Stores", _storeList));

            var detailContent = new Grid();
            detailContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            detailContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var summary = new StackPanel { Margin = new Thickness(0, 0, 0, 2) };
            _selectedComponentTitle.Text = "Select a component";
            _selectedComponentTitle.FontSize = 18;
            _selectedComponentTitle.FontWeight = FontWeights.SemiBold;
            _selectedComponentTitle.Foreground = new SolidColorBrush(Color.FromRgb(31, 31, 36));
            _selectedComponentMetadata.Text = "Click a node to inspect it. Hover to find it in the application.";
            _selectedComponentMetadata.Margin = new Thickness(0, 4, 0, 0);
            _selectedComponentMetadata.Foreground = new SolidColorBrush(Color.FromRgb(91, 95, 102));
            summary.Children.Add(_selectedComponentTitle);
            summary.Children.Add(_selectedComponentMetadata);
            detailContent.Children.Add(summary);
            Grid.SetRow(detailTabs, 1);
            detailContent.Children.Add(detailTabs);

            var detailPanel = CreateSurface("Component inspector", detailContent);
            Grid.SetColumn(detailPanel, 2);
            Grid.SetRow(detailPanel, 0);
            root.Children.Add(detailPanel);

            var applicationLogSplitter = new GridSplitter
            {
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            Grid.SetColumn(applicationLogSplitter, 2);
            Grid.SetRow(applicationLogSplitter, 1);
            root.Children.Add(applicationLogSplitter);

            var logPanel = CreateSurface(CreateApplicationLogHeader(), _applicationLogList);
            Grid.SetColumn(logPanel, 2);
            Grid.SetRow(logPanel, 2);
            root.Children.Add(logPanel);

            return root;
        }

        private UIElement BuildConsoleLayout()
        {
            var root = new Grid { Margin = new Thickness(2) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(170) });

            var logPanel = CreateSurface("Console output", _consoleLogList);
            Grid.SetRow(logPanel, 0);
            root.Children.Add(logPanel);

            var splitter = new GridSplitter
            {
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            Grid.SetRow(splitter, 1);
            root.Children.Add(splitter);

            var detailPanel = CreateSurface("Selected entry", new ScrollViewer { Content = _consoleDetails });
            Grid.SetRow(detailPanel, 2);
            root.Children.Add(detailPanel);

            return root;
        }

        private static Border CreateSurface(string title, UIElement content)
        {
            return CreateSurface(CreateSectionHeader(title), content);
        }

        private static Border CreateSurface(UIElement header, UIElement content)
        {
            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.Children.Add(header);
            Grid.SetRow(content, 1);
            layout.Children.Add(content);

            return new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 227, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(4),
                Padding = new Thickness(12),
                Child = layout
            };
        }

        private void ApplyMaterialTheme()
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(
                    "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml",
                    UriKind.Absolute)
            });
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(
                    "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml",
                    UriKind.Absolute)
            });
        }

        private UIElement CreateVisualTreeHeader()
        {
            var panel = new DockPanel();
            var toggle = CreateIconButton(PackIconKind.Menu, "Hide component tree");
            toggle.Margin = new Thickness(0, 0, 8, 0);
            toggle.Click += (_, __) => ToggleComponentTree();
            DockPanel.SetDock(toggle, Dock.Left);
            panel.Children.Add(toggle);

            panel.Children.Add(new TextBlock
            {
                Text = "Component tree",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            return panel;
        }

        private void ToggleComponentTree()
        {
            _visualTreeCollapsed = !_visualTreeCollapsed;
            _visualTreeColumn.Width = _visualTreeCollapsed ? new GridLength(42) : new GridLength(320);
            _visualTreeSplitterColumn.Width = _visualTreeCollapsed ? new GridLength(0) : new GridLength(5);

            if (_componentTreePanel != null)
                _componentTreePanel.Visibility = _visualTreeCollapsed ? Visibility.Collapsed : Visibility.Visible;

            if (_collapsedTreeToggle != null)
                _collapsedTreeToggle.Visibility = _visualTreeCollapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private UIElement CreateApplicationLogHeader()
        {
            var panel = new DockPanel();
            _applicationLogStatus.Text = "Runtime events";
            _applicationLogStatus.FontWeight = FontWeights.SemiBold;
            _applicationLogStatus.VerticalAlignment = VerticalAlignment.Center;

            var clear = CreateActionButton("Clear");
            clear.Margin = new Thickness(0, 0, 6, 0);
            clear.Click += (_, __) => NuriDiagnostics.ClearLogs();
            DockPanel.SetDock(clear, Dock.Right);
            panel.Children.Add(clear);
            panel.Children.Add(_applicationLogStatus);
            return panel;
        }

        private static TextBlock CreateSectionHeader(string titleText)
        {
            return new TextBlock
            {
                Text = titleText,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(61, 65, 72)),
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        private static Button CreateActionButton(string label)
        {
            return new Button
            {
                Content = label,
                MinWidth = 64,
                Height = 28,
                Padding = new Thickness(10, 0, 10, 0)
            };
        }

        private static Button CreateIconButton(PackIconKind kind, string toolTip)
        {
            return new Button
            {
                Content = new PackIcon
                {
                    Kind = kind,
                    Width = 18,
                    Height = 18
                },
                Width = 34,
                Height = 28,
                Padding = new Thickness(0),
                ToolTip = toolTip
            };
        }

        private void ConfigureApplicationLogList()
        {
            VirtualizingStackPanel.SetIsVirtualizing(_applicationLogList, true);
            VirtualizingStackPanel.SetVirtualizationMode(_applicationLogList, VirtualizationMode.Recycling);
            ScrollViewer.SetCanContentScroll(_applicationLogList, true);

            _applicationLogList.FontFamily = new FontFamily("Consolas");
            _applicationLogList.FontSize = 12;

            var rowStyle = new Style(typeof(ListViewItem));
            rowStyle.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
            rowStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(4, 2, 4, 2)));
            rowStyle.Setters.Add(new Setter(Control.MinHeightProperty, 24.0));
            _applicationLogList.ItemContainerStyle = rowStyle;

            _applicationLogList.View = new GridView
            {
                Columns =
                {
                    CreateColumn("#", "Sequence", 56),
                    CreateColumn("Time", "LocalTime", 92),
                    CreateColumn("Kind", "Kind", 132),
                    CreateColumn("Component", "ComponentId", 170),
                    CreateColumn("Message", "Message", 760)
                }
            };
        }

        private void ConfigureConsoleLogList()
        {
            VirtualizingStackPanel.SetIsVirtualizing(_consoleLogList, true);
            VirtualizingStackPanel.SetVirtualizationMode(_consoleLogList, VirtualizationMode.Recycling);
            ScrollViewer.SetCanContentScroll(_consoleLogList, true);
            _consoleLogList.FontFamily = new FontFamily("Segoe UI");
            _consoleLogList.FontSize = 13;
            _consoleLogList.ItemTemplate = CreateConsoleLogTemplate();
            var itemStyle = new Style(typeof(ListBoxItem));
            itemStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            itemStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
            itemStyle.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
            _consoleLogList.ItemContainerStyle = itemStyle;
            _consoleLogList.SelectionChanged += (_, __) =>
            {
                if (_consoleLogList.SelectedItem is RuntimeLogEntry entry)
                    ShowConsoleDetails(entry);
            };
        }

        private static DataTemplate CreateConsoleLogTemplate()
        {
            var template = new DataTemplate(typeof(RuntimeLogEntry));
            var root = new FrameworkElementFactory(typeof(Border));
            root.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
            root.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235)));
            root.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));

            var dock = new FrameworkElementFactory(typeof(DockPanel));
            var time = new FrameworkElementFactory(typeof(TextBlock));
            time.SetBinding(TextBlock.TextProperty, new Binding("LocalTime"));
            time.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(100, 116, 139)));
            time.SetValue(TextBlock.MarginProperty, new Thickness(18, 0, 0, 0));
            time.SetValue(TextBlock.MinWidthProperty, 96.0);
            time.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            time.SetValue(DockPanel.DockProperty, Dock.Right);

            var message = new FrameworkElementFactory(typeof(TextBlock));
            message.SetBinding(TextBlock.TextProperty, new Binding("Message"));
            message.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            message.SetValue(TextBlock.FontSizeProperty, 13.0);
            message.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(15, 23, 42)));

            dock.AppendChild(time);
            dock.AppendChild(message);
            root.AppendChild(dock);
            template.VisualTree = root;
            return template;
        }

        private static GridViewColumn CreateColumn(string header, string path, double width)
        {
            return new GridViewColumn
            {
                Header = header,
                Width = width,
                DisplayMemberBinding = new Binding(path)
            };
        }

        private static TabItem CreateTab(string header, UIElement content)
        {
            return new TabItem
            {
                Header = header,
                Content = content
            };
        }

        private void OnDiagnosticsChanged(object? sender, EventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(RefreshSnapshot));
        }

        private void RefreshSnapshot()
        {
            _snapshot = NuriDiagnostics.GetSnapshot();
            RefreshTree();
            RefreshStores();
            RefreshApplicationLogs();
            RefreshConsoleLogs();

            if (_highlightComponentId != null && !ContainsComponent(_highlightComponentId))
                ClearHighlight();
        }

        private void RefreshTree()
        {
            var selectedComponentId = GetSelectedComponentId();
            _isRefreshingTree = true;
            try
            {
                _componentTree.Items.Clear();

                foreach (var root in _snapshot.Roots)
                {
                    var rootItem = new TreeViewItem
                    {
                        Header = root.RootId + " (" + root.Renderer + ")",
                        Uid = GetRootTreeNodeId(root.RootId),
                        IsExpanded = IsTreeNodeExpanded(GetRootTreeNodeId(root.RootId))
                    };
                    if (root.RootComponent != null)
                        rootItem.Items.Add(CreateComponentItem(root.RootComponent, selectedComponentId));

                    _componentTree.Items.Add(rootItem);
                }
            }
            finally
            {
                _isRefreshingTree = false;
            }
        }

        private TreeViewItem CreateComponentItem(ComponentSnapshot component, string? selectedComponentId)
        {
            var header = component.TypeName + " [" + component.ComponentId + "] renders=" + component.RenderCount;
            if (component.LastRenderedSequence.HasValue)
                header += " #" + component.LastRenderedSequence.Value;

            var item = new TreeViewItem
            {
                Header = header,
                Tag = component,
                Uid = GetComponentTreeNodeId(component.ComponentId),
                IsExpanded = IsTreeNodeExpanded(GetComponentTreeNodeId(component.ComponentId))
            };

            foreach (var child in component.Children)
                item.Items.Add(CreateComponentItem(child, selectedComponentId));

            if (string.Equals(component.ComponentId, selectedComponentId, StringComparison.Ordinal))
                item.IsSelected = true;

            return item;
        }

        private void OnTreeItemExpanded(object sender, RoutedEventArgs args)
        {
            if (_isRefreshingTree || args.OriginalSource is not TreeViewItem item || string.IsNullOrEmpty(item.Uid))
                return;

            _collapsedTreeNodeIds.Remove(item.Uid);
        }

        private void OnTreeItemCollapsed(object sender, RoutedEventArgs args)
        {
            if (_isRefreshingTree || args.OriginalSource is not TreeViewItem item || string.IsNullOrEmpty(item.Uid))
                return;

            _collapsedTreeNodeIds.Add(item.Uid);
        }

        private bool IsTreeNodeExpanded(string nodeId)
        {
            return !_collapsedTreeNodeIds.Contains(nodeId);
        }

        private static string GetRootTreeNodeId(string rootId)
        {
            return "root:" + rootId;
        }

        private static string GetComponentTreeNodeId(string componentId)
        {
            return "component:" + componentId;
        }

        private string? GetSelectedComponentId()
        {
            return _selectedComponentId;
        }

        private void SelectComponent(ComponentSnapshot component)
        {
            _selectedComponentId = component.ComponentId;
            ShowComponent(component);
        }

        private void HighlightComponent(string componentId)
        {
            if (string.Equals(_highlightComponentId, componentId, StringComparison.Ordinal))
                return;

            _highlightComponentId = componentId;
            WpfElementHighlighter.Highlight(componentId);
        }

        private void ShowComponent(ComponentSnapshot component)
        {
            _selectedComponentTitle.Text = component.TypeName;
            _selectedComponentMetadata.Text = component.ComponentId
                + "  •  " + component.EntryType
                + "  •  renders " + component.RenderCount;
            _componentDetails.Text =
                "ComponentId: " + component.ComponentId + Environment.NewLine +
                "Type: " + component.TypeName + Environment.NewLine +
                "Virtual Entry: " + component.EntryType + Environment.NewLine +
                "Key: " + (component.Key ?? "") + Environment.NewLine +
                "Render Count: " + component.RenderCount + Environment.NewLine +
                "Last Invalidated: " + (component.LastInvalidatedSequence?.ToString() ?? "") + Environment.NewLine +
                "Last Rendered: " + (component.LastRenderedSequence?.ToString() ?? "");

            _hookList.Items.Clear();
            foreach (var hook in component.Hooks)
                _hookList.Items.Add("#" + hook.Index + " " + hook.Kind + " <" + hook.DisplayType + "> " + hook.Summary);
        }

        private void ClearHighlight()
        {
            _highlightComponentId = null;
            WpfElementHighlighter.Clear();
        }

        private void ClearSelection()
        {
            _selectedComponentId = null;
            _highlightComponentId = null;
            _selectedComponentTitle.Text = "Select a component";
            _selectedComponentMetadata.Text = "Click a node to inspect it. Hover to find it in the application.";
            _componentDetails.Text = string.Empty;
            _hookList.Items.Clear();
            WpfElementHighlighter.Clear();
        }

        private static T? FindAncestor<T>(DependencyObject? start)
            where T : DependencyObject
        {
            var current = start;
            while (current != null)
            {
                if (current is T match)
                    return match;

                current = current is FrameworkContentElement contentElement
                    ? contentElement.Parent
                    : VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void RefreshStores()
        {
            _storeList.Items.Clear();
            foreach (var store in _snapshot.Stores)
            {
                _storeList.Items.Add(store.StoreType + " " + store.StoreId + " = " + store.ValueSummary);
                foreach (var subscription in store.Subscriptions)
                {
                    _storeList.Items.Add("  -> " + subscription.ComponentId
                        + " hook #" + subscription.HookIndex
                        + " selected " + subscription.SelectedType
                        + " = " + subscription.SelectedValueSummary);
                }
            }
        }

        private void RefreshApplicationLogs()
        {
            var logs = _snapshot.RecentLogs
                .Where(IsApplicationLog)
                .Reverse()
                .ToArray();
            _applicationLogList.ItemsSource = logs;
            _applicationLogStatus.Text = logs.Length == 0
                ? "Runtime events"
                : "Runtime events  •  " + logs.Length + " recent";
        }

        private void RefreshConsoleLogs()
        {
            _consoleLogList.ItemsSource = _snapshot.RecentLogs
                .Where(IsConsoleLog)
                .Reverse()
                .ToArray();
        }

        private void ShowConsoleDetails(RuntimeLogEntry entry)
        {
            _consoleDetails.Text =
                "Message: " + entry.Message + Environment.NewLine +
                "Time: " + entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine +
                "Kind: " + entry.Kind + Environment.NewLine +
                "Source: " + entry.SourceDisplay + Environment.NewLine +
                "File: " + (entry.SourceFile ?? "") + Environment.NewLine +
                "Class: " + (entry.SourceType ?? "") + Environment.NewLine +
                "Member: " + (entry.SourceMember ?? "") + Environment.NewLine +
                "Line: " + (entry.SourceLine?.ToString() ?? "") + Environment.NewLine +
                "Root: " + (entry.RootId ?? "") + Environment.NewLine +
                "Component: " + (entry.ComponentId ?? "");
        }

        private static bool IsConsoleLog(RuntimeLogEntry entry)
        {
            return entry.Kind == RuntimeLogKind.Console
                || entry.Kind == RuntimeLogKind.AppLog
                || entry.Kind == RuntimeLogKind.FullRebuild;
        }

        private static bool IsApplicationLog(RuntimeLogEntry entry)
        {
            return entry.Kind != RuntimeLogKind.Console
                && entry.Kind != RuntimeLogKind.AppLog;
        }

        private bool ContainsComponent(string componentId)
        {
            return _snapshot.Roots.Any(root => root.RootComponent != null && ContainsComponent(root.RootComponent, componentId));
        }

        private static bool ContainsComponent(ComponentSnapshot component, string componentId)
        {
            return string.Equals(component.ComponentId, componentId, StringComparison.Ordinal)
                || component.Children.Any(child => ContainsComponent(child, componentId));
        }
    }
}
