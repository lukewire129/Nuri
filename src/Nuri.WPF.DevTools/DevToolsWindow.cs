using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Nuri.Runtime.Diagnostics;

namespace Nuri.WPF.DevTools
{
    public sealed class DevToolsWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly TreeView _componentTree = new TreeView();
        private readonly TextBlock _componentDetails = new TextBlock { TextWrapping = TextWrapping.Wrap };
        private readonly ListBox _hookList = new ListBox();
        private readonly ListBox _storeList = new ListBox();
        private readonly ListView _applicationLogList = new ListView();
        private readonly ListBox _consoleLogList = new ListBox();
        private readonly TextBlock _consoleDetails = new TextBlock { TextWrapping = TextWrapping.Wrap };
        private readonly ColumnDefinition _visualTreeColumn = new ColumnDefinition { Width = new GridLength(320) };
        private readonly ColumnDefinition _visualTreeSplitterColumn = new ColumnDefinition { Width = new GridLength(5) };
        private RuntimeSnapshot _snapshot = NuriDiagnostics.GetSnapshot();
        private string? _selectedComponentId;
        private string? _highlightComponentId;
        private bool _isRefreshingTree;
        private bool _visualTreeCollapsed;

        public DevToolsWindow()
        {
            Title = "Nuri Runtime DevTools";
            Width = 1180;
            Height = 760;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;
            Content = BuildLayout();
            ConfigureApplicationLogList();
            ConfigureConsoleLogList();

            _componentTree.SelectedItemChanged += (_, args) =>
            {
                if (_isRefreshingTree)
                    return;

                if (args.NewValue is TreeViewItem item && item.Tag is ComponentSnapshot component)
                {
                    SelectComponent(component, highlight: true);
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
                    SelectComponent(component, highlight: true);
            };
            PreviewKeyDown += (_, args) =>
            {
                if (args.Key != Key.Escape)
                    return;

                ClearHighlight();
                args.Handled = true;
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (_, __) => RefreshSnapshot();
            _timer.Start();

            NuriDiagnostics.Changed += OnDiagnosticsChanged;
            Closed += (_, __) =>
            {
                _timer.Stop();
                NuriDiagnostics.Changed -= OnDiagnosticsChanged;
                WpfElementHighlighter.Clear();
            };

            RefreshSnapshot();
        }

        private UIElement BuildLayout()
        {
            var tabs = new TabControl();
            tabs.Items.Add(CreateTab("Console", BuildConsoleLayout()));
            tabs.Items.Add(CreateTab("Application", BuildApplicationLayout()));
            return tabs;
        }

        private UIElement BuildApplicationLayout()
        {
            var root = new Grid { Margin = new Thickness(12) };
            root.ColumnDefinitions.Add(_visualTreeColumn);
            root.ColumnDefinitions.Add(_visualTreeSplitterColumn);
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(230) });

            var treePanel = CreatePanel("Component Tree", _componentTree);
            treePanel.Header = CreateVisualTreeHeader();
            Grid.SetColumn(treePanel, 0);
            Grid.SetRow(treePanel, 0);
            Grid.SetRowSpan(treePanel, 3);
            root.Children.Add(treePanel);

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

            var detailTabs = new TabControl();
            detailTabs.Items.Add(CreateTab("Component", new ScrollViewer { Content = _componentDetails }));
            detailTabs.Items.Add(CreateTab("Hooks", _hookList));
            detailTabs.Items.Add(CreateTab("Stores", _storeList));
            Grid.SetColumn(detailTabs, 2);
            Grid.SetRow(detailTabs, 0);
            root.Children.Add(detailTabs);

            var horizontalSplitter = new GridSplitter
            {
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            Grid.SetColumn(horizontalSplitter, 2);
            Grid.SetRow(horizontalSplitter, 1);
            root.Children.Add(horizontalSplitter);

            var logPanel = CreatePanel("Application Logs", _applicationLogList);
            logPanel.Header = CreateLogHeader("Application Logs");
            Grid.SetColumn(logPanel, 2);
            Grid.SetRow(logPanel, 2);
            root.Children.Add(logPanel);

            return root;
        }

        private UIElement BuildConsoleLayout()
        {
            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(170) });

            var logPanel = CreatePanel("Console", _consoleLogList);
            logPanel.Header = CreateLogHeader("Console");
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

            var detailPanel = CreatePanel("Details", new ScrollViewer { Content = _consoleDetails });
            Grid.SetRow(detailPanel, 2);
            root.Children.Add(detailPanel);

            return root;
        }

        private static GroupBox CreatePanel(string header, UIElement content)
        {
            return new GroupBox
            {
                Header = header,
                Margin = new Thickness(6),
                Padding = new Thickness(8),
                Content = content
            };
        }

        private UIElement CreateVisualTreeHeader()
        {
            var panel = new DockPanel();
            var clear = new Button
            {
                Content = "Clear Highlight",
                MinWidth = 108,
                Height = 26,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 6, 0)
            };
            clear.Click += (_, __) => ClearHighlight();

            var toggle = new Button
            {
                Content = "Hide",
                MinWidth = 60,
                Height = 26,
                Padding = new Thickness(10, 0, 10, 0)
            };
            toggle.Click += (_, __) =>
            {
                _visualTreeCollapsed = !_visualTreeCollapsed;
                _visualTreeColumn.Width = _visualTreeCollapsed ? new GridLength(96) : new GridLength(320);
                _visualTreeSplitterColumn.Width = _visualTreeCollapsed ? new GridLength(0) : new GridLength(5);
                _componentTree.Visibility = _visualTreeCollapsed ? Visibility.Collapsed : Visibility.Visible;
                toggle.Content = _visualTreeCollapsed ? "Show" : "Hide";
            };

            DockPanel.SetDock(toggle, Dock.Right);
            DockPanel.SetDock(clear, Dock.Right);
            panel.Children.Add(toggle);
            panel.Children.Add(clear);
            panel.Children.Add(new TextBlock
            {
                Text = "Visual Tree",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            return panel;
        }

        private UIElement CreateLogHeader(string titleText)
        {
            var panel = new DockPanel();
            var title = new TextBlock
            {
                Text = titleText,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            var clear = new Button
            {
                Content = "Clear",
                MinWidth = 70,
                Height = 26,
                Padding = new Thickness(10, 0, 10, 0)
            };
            clear.Click += (_, __) => NuriDiagnostics.ClearLogs();

            DockPanel.SetDock(clear, Dock.Right);
            panel.Children.Add(clear);
            panel.Children.Add(title);
            return panel;
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
                    var rootItem = new TreeViewItem { Header = root.RootId + " (" + root.Renderer + ")" };
                    if (root.RootComponent != null)
                        rootItem.Items.Add(CreateComponentItem(root.RootComponent, selectedComponentId));

                    rootItem.IsExpanded = true;
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
                IsExpanded = true
            };

            foreach (var child in component.Children)
                item.Items.Add(CreateComponentItem(child, selectedComponentId));

            if (string.Equals(component.ComponentId, selectedComponentId, StringComparison.Ordinal))
                item.IsSelected = true;

            return item;
        }

        private string? GetSelectedComponentId()
        {
            return _selectedComponentId;
        }

        private void SelectComponent(ComponentSnapshot component, bool highlight)
        {
            _selectedComponentId = component.ComponentId;
            ShowComponent(component);

            if (!highlight)
                return;

            _highlightComponentId = component.ComponentId;
            WpfElementHighlighter.Highlight(component.ComponentId);
        }

        private void ShowComponent(ComponentSnapshot component)
        {
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
            _applicationLogList.ItemsSource = _snapshot.RecentLogs
                .Where(IsApplicationLog)
                .Reverse()
                .ToArray();
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
