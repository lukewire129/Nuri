using System.Threading;
using Duxel.Core;
using Nuri.Runtime;
using Nuri.Runtime.Invalidation;
using Nuri.Runtime.Lifecycle;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using Nuri.VirtualDom;

namespace Nuri.Duxel;

public sealed class NuriDuxelScreen : UiScreen, IDisposable
{
    private static int _nextTreeIndex;
    private readonly Action _requestFrame;
    private readonly ApplicationRuntime<IElement> _runtime;
    private readonly DuxelVirtualEntryRenderer _renderer = new();
    private readonly ComponentInvalidationQueue _invalidations = new();
    private readonly string _rootId;
    private readonly string _surfaceTitle;
    private bool _initialized;
    private bool _disposed;

    public NuriDuxelScreen(IElement rootElement, Action requestFrame, string surfaceTitle = "Nuri")
    {
        ArgumentNullException.ThrowIfNull(rootElement);
        _requestFrame = requestFrame ?? throw new ArgumentNullException(nameof(requestFrame));
        _surfaceTitle = string.IsNullOrWhiteSpace(surfaceTitle) ? "Nuri" : surfaceTitle;

        var treePrefix = $"duxel{Interlocked.Increment(ref _nextTreeIndex)}";
        rootElement.LoadNodeNumber(treePrefix, 0);
        Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(rootElement.Id, rootElement);
        _rootId = rootElement.Id;

        _runtime = new ApplicationRuntime<IElement>(() => rootElement, element => element.ToVirtualEntry());
        Component.AnyStateChanged += OnAnyStateChanged;
    }

    public override void Render(UiImmediateContext ui)
    {
        if (_disposed)
        {
            return;
        }

        ApplicationRenderResult<IElement>? pendingCommit = null;
        var shouldFlushEffects = false;
        if (!_initialized)
        {
            pendingCommit = _runtime.Initialize();
            _initialized = true;
            shouldFlushEffects = true;
        }
        else if (_invalidations.HasPending)
        {
            pendingCommit = RebuildInvalidatedComponents();
            shouldFlushEffects = true;
        }

        var entry = pendingCommit?.VirtualEntry ?? _runtime.CurrentVirtualEntry;
        ui.BeginWindow($"{_surfaceTitle}##{_rootId}");
        try
        {
            _renderer.Render(ui, entry);
        }
        finally
        {
            ui.EndWindow();
        }

        if (pendingCommit is not null)
        {
            _runtime.Commit(pendingCommit);
        }

        if (shouldFlushEffects)
        {
            ComponentLifecycle.FlushPendingEffects();
        }

        if (_invalidations.HasPending)
        {
            _requestFrame();
        }
    }

    private ApplicationRenderResult<IElement>? RebuildInvalidatedComponents()
    {
        ApplicationRenderResult<IElement>? rootRebuild = null;
        foreach (var invalidation in _invalidations.DrainCoveredByParents())
        {
            if (string.Equals(invalidation.ComponentId, _runtime.CurrentVirtualEntry.Id, StringComparison.Ordinal))
            {
                rootRebuild = _runtime.CreateRebuild();
                ComponentLifecycle.CleanupRemovedComponentState(rootRebuild.Operations);
                break;
            }

            if (!TryRebuildSubtree(invalidation.Component, invalidation.ComponentId))
            {
                rootRebuild = _runtime.CreateRebuild();
                ComponentLifecycle.CleanupRemovedComponentState(rootRebuild.Operations);
                break;
            }
        }

        return rootRebuild;
    }

    private bool TryRebuildSubtree(Component component, string componentId)
    {
        var oldEntry = _runtime.CurrentVirtualEntry.FindByComponentId(componentId)
            ?? _runtime.CurrentVirtualEntry.FindById(componentId);
        if (oldEntry is null)
        {
            return false;
        }

        var newEntry = component.ToVirtualEntry();
        newEntry.RewriteIdentity(oldEntry.Id, oldEntry.ParentId);
        newEntry.WithComponentId(componentId);

        var operations = VirtualTreeDiff.Diff(oldEntry, newEntry);
        ComponentLifecycle.CleanupRemovedComponentState(operations);

        if (!_runtime.CurrentVirtualEntry.ReplaceDescendantByComponentId(componentId, newEntry)
            && !_runtime.CurrentVirtualEntry.ReplaceDescendant(oldEntry.Id, newEntry))
        {
            return false;
        }

        _runtime.CommitVirtualEntry(_runtime.CurrentVirtualEntry);
        return true;
    }

    private void OnAnyStateChanged(object? sender, Component component)
    {
        if (_disposed)
        {
            return;
        }

        if (!ComponentLifecycle.IsInSubtree(component.Id, _rootId))
        {
            return;
        }

        _invalidations.Enqueue(component);
        _requestFrame();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Component.AnyStateChanged -= OnAnyStateChanged;
        _invalidations.Clear();
        ComponentLifecycle.DisposeSubtree(_rootId);
        _disposed = true;
    }
}
