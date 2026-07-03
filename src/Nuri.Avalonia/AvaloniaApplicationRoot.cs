using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using Nuri.Runtime;
using Nuri.UI;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.Avalonia
{
    public sealed class AvaloniaApplicationRoot : IDisposable
    {
        private static int _nextTreeIndex;
        private Control? _currentRootVisual;
        private ApplicationRuntime<IElement>? _runtime;
        private Window? _mainWindow;
        private string _treePrefix = string.Empty;
        private readonly List<Component> _dirtyComponents = new List<Component>();
        private bool _rebuildScheduled;
        private bool _disposed;
        private ApplicationRuntime<IElement> Runtime => _runtime ?? throw new InvalidOperationException("AvaloniaApplicationRoot is not initialized.");

        private AvaloniaApplicationRoot()
        {
        }

        public static AvaloniaApplicationRoot Initialize(IElement rootElement, Window mainWindow)
        {
            var instance = new AvaloniaApplicationRoot();
            instance.InitializeInternal(rootElement, mainWindow);
            return instance;
        }

        public void Rebuild()
        {
            if (_disposed || _currentRootVisual == null)
                return;

            var renderResult = Runtime.CreateRebuild();
            CleanupRemovedComponentState(renderResult.Operations);
            if (_mainWindow != null)
                ApplyWindowProperties(_mainWindow, renderResult.VisualNode);

            AvaloniaVirtualEntryRenderer.ApplyDiff(_currentRootVisual, renderResult.Operations);
            Runtime.Commit(renderResult);
            Component.FlushPendingEffects();
        }

        public void DispatchRebuild()
        {
            Dispatcher.UIThread.Post(Rebuild);
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
            Dispatcher.UIThread.Post(ProcessScheduledRebuild);
        }

        private void InitializeInternal(IElement rootElement, Window mainWindow)
        {
            var treePrefix = $"avalonia{Interlocked.Increment(ref _nextTreeIndex)}";
            _treePrefix = treePrefix;
            rootElement.LoadNodeNumber(treePrefix, 0);
            ElementTree<IElement, AnimationValue>.AssignDescendantIds(rootElement.Id, rootElement);
            _mainWindow = mainWindow;

            _runtime = new ApplicationRuntime<IElement>(() => rootElement, element => element.ToVirtualEntry());

            var renderResult = Runtime.Initialize();
            ApplyWindowProperties(mainWindow, rootElement);
            var rootVisual = AvaloniaVirtualEntryRenderer.Build(renderResult.VirtualEntry);

            mainWindow.Content = rootVisual;
            _currentRootVisual = rootVisual;
            Component.FlushPendingEffects();
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

            var oldEntry = Runtime.CurrentVirtualEntry.FindById(component.Id);
            if (oldEntry == null)
            {
                Rebuild();
                return;
            }

            var newVisual = RenderComponentSubtree(component);
            var newEntry = newVisual.ToVirtualEntry();
            var operations = VirtualTreeDiff.Diff(oldEntry, newEntry);

            CleanupRemovedComponentState(operations);
            AvaloniaVirtualEntryRenderer.ApplyDiff(_currentRootVisual, operations);

            if (Runtime.CurrentVirtualEntry.ReplaceDescendant(component.Id, newEntry))
                Runtime.CommitVirtualEntry(Runtime.CurrentVirtualEntry);
            else
                Rebuild();

            Component.FlushPendingEffects();
        }

        private static void CleanupRemovedComponentState(IEnumerable<PatchOperation> operations)
        {
            foreach (var removeChild in operations.OfType<RemoveChildPatch>())
                Component.DisposeHookState(removeChild.Child.Id);

            foreach (var replaceEntry in operations.OfType<ReplaceEntryPatch>())
                Component.DisposeHookState(replaceEntry.OldEntry.Id);
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
            component.CompleteRenderHooks();

            foreach (var property in component.Properties)
            {
                if (!renderedChild.Properties.ContainsKey(property.Key))
                    renderedChild.Properties[property.Key] = property.Value;
            }

            renderedChild.ParentId = component.ParentId;
            renderedChild.Id = component.Id;
            ElementTree<IElement, AnimationValue>.AssignDescendantIds(component.Id, renderedChild);
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

            Component.DisposeHookState(_treePrefix + "_0");
            _disposed = true;
        }
    }
}
