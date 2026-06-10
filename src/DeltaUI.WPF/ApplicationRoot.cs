using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using DeltaUI.Core.Runtime;
using DeltaUI.Core.VirtualDom;

namespace DeltaUI.WPF
{
    public class ApplicationRoot
    {
        public static List<IElement> Components { get; set; } = new ();
        private static ApplicationRoot? _instance = new ();
        private FrameworkElement? _currentRootVisual;
        private ApplicationRuntime<IElement>? _runtime;
        private EventHandler? _pendingStateIndexInitialize;
        private readonly List<Component> _dirtyComponents = new ();
        private bool _rebuildScheduled;

        public event EventHandler? StateIndexInitialize
        {
            add
            {
                if (_runtime == null)
                    _pendingStateIndexInitialize += value;
                else
                    _runtime.StateIndexInitialize += value;
            }
            remove
            {
                if (_runtime == null)
                    _pendingStateIndexInitialize -= value;
                else
                    _runtime.StateIndexInitialize -= value;
            }
        }

        public static ApplicationRoot Instance => _instance ?? throw new InvalidOperationException ("ApplicationRoot is not initialized.");
        private ApplicationRuntime<IElement> Runtime => _runtime ?? throw new InvalidOperationException ("ApplicationRoot is not initialized.");

        static ApplicationRoot()
        {

        }
        private ApplicationRoot()
        {
        }

        public static void Initialize(Component rootComponent, Window mainWindow)
        {
            Instance.InitializeInternal (rootComponent);
            Instance.InitializeInternal (mainWindow);
        }

        private void InitializeInternal(Component rootComponent)
        {
            _runtime = new ApplicationRuntime<IElement> (() =>
            {
                rootComponent.ResetStateIndexForRender ();
                return rootComponent.Render ();
            }, element => element.ToVirtualEntry ());
            _runtime.StateIndexInitialize += _pendingStateIndexInitialize;
            _pendingStateIndexInitialize = null;
        }

        private void InitializeInternal(Window mainWindow)
        {
            var renderResult = Runtime.Initialize ();
            var rootVisual = WpfVirtualEntryRenderer.Build (renderResult.VirtualEntry);

            mainWindow.Content = rootVisual;
            _currentRootVisual = rootVisual;
        }
        public void Rebuild()
        {
            Debug.WriteLine ("[Rebuild] Resetting _stateIndex to 0.");
            var renderResult = Runtime.CreateRebuild ();
            var diffOperations = renderResult.Operations;

            Debug.WriteLine ("[Rebuild] Render completed. New VisualNode created.");
            var removeChilds = diffOperations.OfType<RemoveChildPatch> ().ToList ();
            foreach (var child in removeChilds)
            {
                var realComponent = Components.FirstOrDefault (x => x.Id == child.Child.Id);
                if (realComponent == null)
                    continue;

                if(realComponent is Component component)
                {
                    component.Dispose ();
                    Components.Remove (realComponent);
                }
            }

            if (_currentRootVisual is FrameworkElement rootElement)
            {
                Debug.WriteLine ("[Rebuild] Applying diff operations to root element.");
                WpfVirtualEntryRenderer.ApplyDiff (rootElement, diffOperations);
            }

            Debug.WriteLine ("[Rebuild] Updating runtime state.");
            Runtime.Commit (renderResult);
        }

        public void RequestRebuild(Component component)
        {
            if (!_dirtyComponents.Any (dirty => ReferenceEquals (dirty, component)))
                _dirtyComponents.Add (component);

            if (_rebuildScheduled)
                return;

            _rebuildScheduled = true;

            var dispatcher = _currentRootVisual?.Dispatcher ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                ProcessDirtyComponents ();
                return;
            }

            dispatcher.BeginInvoke ((Action)ProcessDirtyComponents, DispatcherPriority.Render);
        }

        public void Rebuild(Component component)
        {
            if (component.Id == "0")
            {
                Rebuild ();
                return;
            }

            var oldEntry = Runtime.CurrentVirtualEntry.FindById (component.Id);
            if (oldEntry == null)
            {
                Rebuild ();
                return;
            }

            var newVisual = RenderComponentSubtree (component);
            var newEntry = newVisual.ToVirtualEntry ();
            var diffOperations = VirtualTreeDiff.Diff (oldEntry, newEntry);

            DisposeRemovedComponents (diffOperations);

            if (_currentRootVisual is FrameworkElement rootElement)
                WpfVirtualEntryRenderer.ApplyDiff (rootElement, diffOperations);

            if (Runtime.CurrentVirtualEntry.ReplaceDescendant (component.Id, newEntry))
                Runtime.CommitVirtualEntry (Runtime.CurrentVirtualEntry);
            else
                Rebuild ();
        }

        private void ProcessDirtyComponents()
        {
            var dirtyComponents = _dirtyComponents.ToList ();
            _dirtyComponents.Clear ();
            _rebuildScheduled = false;

            if (dirtyComponents.Count == 0)
                return;

            if (dirtyComponents.Any (component => component.Id == "0"))
            {
                Rebuild ();
                return;
            }

            foreach (var component in FilterCoveredDirtyComponents (dirtyComponents))
                Rebuild (component);
        }

        private static IEnumerable<Component> FilterCoveredDirtyComponents(IEnumerable<Component> dirtyComponents)
        {
            var ordered = dirtyComponents
                .Where (component => !string.IsNullOrEmpty (component.Id))
                .OrderBy (component => component.Id.Length)
                .ToList ();

            for (var i = 0; i < ordered.Count; i++)
            {
                var component = ordered[i];
                var isCoveredByParent = ordered.Take (i).Any (parent => IsDescendantId (component.Id, parent.Id));
                if (!isCoveredByParent)
                    yield return component;
            }
        }

        private static bool IsDescendantId(string childId, string parentId)
        {
            return childId.Length > parentId.Length
                && childId.StartsWith (parentId + "_", StringComparison.Ordinal);
        }

        private static IElement RenderComponentSubtree(Component component)
        {
            component.ResetStateIndexForRender ();
            var renderedChild = component.Render ();

            foreach (var item in component.GetAttachedProperty ())
            {
                if (item.Value != null && !renderedChild.Properties.ContainsKey (item.Key))
                    renderedChild.Properties.Add (item.Key, item.Value);
            }

            renderedChild.ParentId = component.ParentId;
            renderedChild.Id = component.Id;
            DeltaUI.Core.UI.ElementTree<IElement, DeltaUI.Core.UI.Values.AnimationValue>.AssignDescendantIds (component.Id, renderedChild);
            return renderedChild;
        }

        private void DisposeRemovedComponents(IEnumerable<PatchOperation> diffOperations)
        {
            var removeChilds = diffOperations.OfType<RemoveChildPatch> ().ToList ();
            foreach (var child in removeChilds)
            {
                var realComponent = Components.FirstOrDefault (x => x.Id == child.Child.Id);
                if (realComponent == null)
                    continue;

                if(realComponent is Component component)
                {
                    component.Dispose ();
                    Components.Remove (realComponent);
                }
            }
        }
    }
}
