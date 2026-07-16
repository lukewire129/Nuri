using System.Threading;
using Duxel.Core;
using Nuri.Runtime;
using Nuri.Runtime.Diagnostics;
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
    private readonly DuxelVirtualEntryRenderer _renderer;
    private readonly Func<UiVector2?>? _viewportSizeProvider;
    private readonly Action<UiTheme>? _themeObserver;
    private readonly object _themeGate = new();
    private readonly ComponentInvalidationQueue _invalidations = new();
    private readonly string _rootId;
    private bool _initialized;
    private bool _diagnosticsRegistered;
    private bool _disposed;
    private int _fullRebuildRequested;
    private UiTheme _currentTheme;
    private bool _hasCurrentTheme;
    private ThemeBox? _pendingTheme;

    public NuriDuxelScreen(
        IElement rootElement,
        Action requestFrame,
        string surfaceTitle = "Nuri",
        DuxelInputEventQueue? inputEvents = null,
        Func<UiVector2?>? viewportSizeProvider = null,
        Action<UiTheme>? themeObserver = null)
    {
        ArgumentNullException.ThrowIfNull(rootElement);
        _requestFrame = requestFrame ?? throw new ArgumentNullException(nameof(requestFrame));
        _renderer = new DuxelVirtualEntryRenderer(inputEvents);
        _viewportSizeProvider = viewportSizeProvider;
        _themeObserver = themeObserver;

        var treePrefix = $"duxel{Interlocked.Increment(ref _nextTreeIndex)}";
        rootElement.LoadNodeNumber(treePrefix, 0);
        Nuri.UI.ElementTree<IElement, AnimationValue>.AssignDescendantIds(rootElement.Id, rootElement);
        _rootId = rootElement.Id;

        _runtime = new ApplicationRuntime<IElement>(() => rootElement, element => element.ToVirtualEntry());
        Component.AnyStateChanged += OnAnyStateChanged;
    }

    public bool HasActiveAnimations => _renderer.HasActiveAnimations;

    public bool HasActiveScrollMotion => _renderer.HasActiveScrollMotion;

    public bool HasPendingLayout => _renderer.HasPendingLayout;

    public UiTheme? CurrentTheme
    {
        get
        {
            lock (_themeGate)
            {
                return _hasCurrentTheme ? _currentTheme : null;
            }
        }
    }

    public override void Render(UiImmediateContext ui)
    {
        if (_disposed)
        {
            return;
        }

        var currentTheme = CaptureTheme(ui);
        lock (_themeGate)
        {
            _currentTheme = currentTheme;
            _hasCurrentTheme = true;
        }
        _themeObserver?.Invoke(currentTheme);

        var pendingTheme = Interlocked.Exchange(ref _pendingTheme, null);
        if (pendingTheme is not null)
        {
            ui.RequestTheme(pendingTheme.Theme);
        }

        ApplicationRenderResult<IElement>? pendingCommit = null;
        var shouldFlushEffects = false;
        if (!_initialized)
        {
            Interlocked.Exchange(ref _fullRebuildRequested, 0);
            pendingCommit = _runtime.Initialize();
            _initialized = true;
            shouldFlushEffects = true;
        }
        else if (Interlocked.Exchange(ref _fullRebuildRequested, 0) != 0)
        {
            _invalidations.Clear();
            pendingCommit = _runtime.CreateRebuild();
            ComponentLifecycle.CleanupRemovedComponentState(pendingCommit.Operations);
            shouldFlushEffects = true;
        }
        else if (_invalidations.HasPending)
        {
            pendingCommit = RebuildInvalidatedComponents();
            shouldFlushEffects = true;
        }

        var entry = pendingCommit?.VirtualEntry ?? _runtime.CurrentVirtualEntry;
        ui.EnableRootViewportContentLayout(contentPadding: 0f);
        var viewport = ui.GetMainViewport();
        var measuredSize = _viewportSizeProvider?.Invoke();
        var viewportWidth = measuredSize is { X: > 0f }
            ? measuredSize.Value.X
            : viewport.WorkSize.X;
        var viewportHeight = measuredSize is { Y: > 0f }
            ? measuredSize.Value.Y
            : viewport.WorkSize.Y;
        _renderer.Render(
            ui,
            entry,
            new UiRect(
                viewport.WorkPos.X,
                viewport.WorkPos.Y,
                viewportWidth,
                viewportHeight));

        if (pendingCommit is not null)
        {
            _runtime.Commit(pendingCommit);
            NuriDiagnostics.RecordPatchBatch(_rootId, pendingCommit.Operations);
        }

        if (!_diagnosticsRegistered)
        {
            NuriDiagnostics.RegisterRoot(_rootId, "Duxel", () => _runtime.CurrentVirtualEntry);
            _diagnosticsRegistered = true;
        }

        if (shouldFlushEffects)
        {
            ComponentLifecycle.FlushPendingEffects();
        }

        if (_invalidations.HasPending
            || Volatile.Read(ref _fullRebuildRequested) != 0
            || _renderer.HasActiveAnimations
            || _renderer.HasActiveScrollMotion
            || _renderer.HasPendingLayout
            || _renderer.HasPendingInput)
        {
            _requestFrame();
        }
    }

    private static UiTheme CaptureTheme(UiImmediateContext ui)
    {
        var theme = new UiTheme();
        for (var index = 0; index < UiThemeColors.StyleColorCount; index++)
        {
            var color = (UiStyleColor)index;
            theme[color] = ui.GetColorU32(color);
        }

        return theme;
    }

    public void RequestFullRebuild()
    {
        if (_disposed)
        {
            return;
        }

        Interlocked.Exchange(ref _fullRebuildRequested, 1);
        _requestFrame();
    }

    public void RequestTheme(UiTheme theme)
    {
        if (_disposed)
        {
            return;
        }

        Volatile.Write(ref _pendingTheme, new ThemeBox(theme));
        _requestFrame();
    }

    private ApplicationRenderResult<IElement>? RebuildInvalidatedComponents()
    {
        ApplicationRenderResult<IElement>? rootRebuild = null;
        foreach (var invalidation in _invalidations.DrainCoveredByParents())
        {
            NuriDiagnostics.Log(
                RuntimeLogKind.SubtreeRebuild,
                _rootId,
                invalidation.ComponentId,
                "Subtree rebuild scheduled.");
            if (string.Equals(invalidation.ComponentId, _runtime.CurrentVirtualEntry.Id, StringComparison.Ordinal))
            {
                NuriDiagnostics.Log(
                    RuntimeLogKind.FullRebuild,
                    _rootId,
                    invalidation.ComponentId,
                    "Dirty root requested full rebuild.");
                rootRebuild = _runtime.CreateRebuild();
                ComponentLifecycle.CleanupRemovedComponentState(rootRebuild.Operations);
                break;
            }

            if (!TryRebuildSubtree(invalidation.Component, invalidation.ComponentId))
            {
                NuriDiagnostics.Log(
                    RuntimeLogKind.FullRebuild,
                    _rootId,
                    invalidation.ComponentId,
                    "Dirty subtree was not found or replacement failed.");
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
        NuriDiagnostics.RecordPatchBatch(_rootId, operations);
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
        NuriDiagnostics.Log(
            RuntimeLogKind.ComponentInvalidated,
            _rootId,
            component.Id,
            "Component scheduled for Duxel frame projection.");
        _requestFrame();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Component.AnyStateChanged -= OnAnyStateChanged;
        Interlocked.Exchange(ref _fullRebuildRequested, 0);
        Interlocked.Exchange(ref _pendingTheme, null);
        _invalidations.Clear();
        _renderer.Dispose();
        ComponentLifecycle.DisposeSubtree(_rootId);
        if (_diagnosticsRegistered)
        {
            NuriDiagnostics.UnregisterRoot(_rootId);
        }

        _disposed = true;
    }

    private sealed record ThemeBox(UiTheme Theme);
}
