using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using Nuri.Runtime;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.WPF
{
    public sealed class ApplicationRoot : IDisposable
    {
        private static int _nextTreeIndex;
        private FrameworkElement? _currentRootVisual;
        private ApplicationRuntime<IElement>? _runtime;
        private Window? _mainWindow;
        private string _treePrefix = string.Empty;
        private readonly List<Component> _dirtyComponents = new List<Component>();
        private bool _rebuildScheduled;
        private bool _disposed;
        private ApplicationRuntime<IElement> Runtime => _runtime ?? throw new InvalidOperationException("ApplicationRoot is not initialized.");

        private ApplicationRoot()
        {
        }

        public static ApplicationRoot Initialize(IElement rootElement, Window mainWindow)
        {
            var instance = new ApplicationRoot();
            instance.InitializeInternal(rootElement, mainWindow);
            return instance;
        }

        public void Rebuild()
        {
            if (_disposed)
                return;

            if (_currentRootVisual == null)
                return;

            var renderResult = Runtime.CreateRebuild();
            if (_mainWindow != null)
                ApplyWindowProperties(_mainWindow, renderResult.VisualNode);
            WpfVirtualEntryRenderer.ApplyDiff(_currentRootVisual, renderResult.Operations);
            Runtime.Commit(renderResult);
        }

        public void DispatchRebuild()
        {
            var dispatcher = _currentRootVisual?.Dispatcher ?? _mainWindow?.Dispatcher ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                Rebuild();
                return;
            }

            dispatcher.BeginInvoke((Action)Rebuild, DispatcherPriority.Render);
        }

        private void InitializeInternal(IElement rootElement, Window mainWindow)
        {
            var treePrefix = $"window{Interlocked.Increment(ref _nextTreeIndex)}";
            _treePrefix = treePrefix;
            rootElement.LoadNodeNumber(treePrefix, 0);
            Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(rootElement.Id, rootElement);
            _mainWindow = mainWindow;

            _runtime = new ApplicationRuntime<IElement>(() =>
            {
                return rootElement;
            }, element => element.ToVirtualEntry());

            var renderResult = Runtime.Initialize();
            ApplyWindowProperties(mainWindow, rootElement);
            var rootVisual = WpfVirtualEntryRenderer.Build(renderResult.VirtualEntry);

            mainWindow.Content = rootVisual;
            _currentRootVisual = rootVisual;
        }

        public void ScheduleComponentRebuild(Component component)
        {
            if (!IsInThisTree(component))
                return;

            if (!_dirtyComponents.Any(dirty => ReferenceEquals(dirty, component)))
                _dirtyComponents.Add(component);

            if (_rebuildScheduled)
                return;

            _rebuildScheduled = true;

            var dispatcher = _currentRootVisual?.Dispatcher ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                ProcessScheduledRebuild();
                return;
            }

            dispatcher.BeginInvoke((Action)ProcessScheduledRebuild, DispatcherPriority.Render);
        }

        private void ProcessScheduledRebuild()
        {
            var dirtyComponents = _dirtyComponents.ToList();
            _dirtyComponents.Clear();
            _rebuildScheduled = false;

            if (dirtyComponents.Count == 0)
                return;

            if (dirtyComponents.Any(component => string.Equals(component.Id, Runtime.CurrentVirtualEntry.Id, StringComparison.Ordinal)))
            {
                Rebuild();
                return;
            }

            foreach (var component in FilterCoveredDirtyComponents(dirtyComponents))
                Rebuild(component);
        }

        private void Rebuild(Component component)
        {
            if (_currentRootVisual == null)
                return;

            if (string.Equals(component.Id, Runtime.CurrentVirtualEntry.Id, StringComparison.Ordinal))
            {
                Rebuild();
                return;
            }

            var oldEntry = Runtime.CurrentVirtualEntry.FindById(component.Id);
            if (oldEntry == null)
            {
                Rebuild();
                return;
            }

            var newVisual = RenderComponentSubtree(component);
            var newEntry = newVisual.ToVirtualEntry();
            var operations = VirtualTreeDiff.Diff(oldEntry, newEntry);

            WpfVirtualEntryRenderer.ApplyDiff(_currentRootVisual, operations);

            if (Runtime.CurrentVirtualEntry.ReplaceDescendant(component.Id, newEntry))
                Runtime.CommitVirtualEntry(Runtime.CurrentVirtualEntry);
            else
                Rebuild();
        }

        private static IEnumerable<Component> FilterCoveredDirtyComponents(IEnumerable<Component> dirtyComponents)
        {
            var ordered = dirtyComponents
                .Where(component => !string.IsNullOrEmpty(component.Id))
                .OrderBy(component => component.Id.Length)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var component = ordered[i];
                var isCoveredByParent = ordered.Take(i).Any(parent => IsDescendantId(component.Id, parent.Id));
                if (!isCoveredByParent)
                    yield return component;
            }
        }

        private static bool IsDescendantId(string childId, string parentId)
        {
            return childId.Length > parentId.Length
                && childId.StartsWith(parentId + "_", StringComparison.Ordinal);
        }

        private bool IsInThisTree(Component component)
        {
            return !string.IsNullOrEmpty(component.Id)
                && component.Id.StartsWith(_treePrefix + "_", StringComparison.Ordinal);
        }

        private static IElement RenderComponentSubtree(Component component)
        {
            component.ResetStateIndexForRender();
            var renderedChild = component.Render();

            foreach (var property in component.Properties)
            {
                if (!renderedChild.Properties.ContainsKey(property.Key))
                    renderedChild.Properties[property.Key] = property.Value;
            }

            renderedChild.ParentId = component.ParentId;
            renderedChild.Id = component.Id;
            Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(component.Id, renderedChild);
            return renderedChild;
        }

        private static void ApplyWindowProperties(Window mainWindow, IElement rootElement)
        {
            if (rootElement.Properties.TryGetValue("Title", out var title) && title is string titleText)
                mainWindow.Title = titleText;

            if (rootElement.Properties.TryGetValue("Width", out var width) && width is not null)
                mainWindow.Width = Convert.ToDouble(width);

            if (rootElement.Properties.TryGetValue("Height", out var height) && height is not null)
                mainWindow.Height = Convert.ToDouble(height);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
        }
    }
}
