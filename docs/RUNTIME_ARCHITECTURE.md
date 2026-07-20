# Runtime Architecture Direction

This document records the architectural direction and measured baseline for Nuri's component runtime. It should be reviewed before introducing scheduling, hook, reconciliation, or lifecycle changes.

## Decision

Nuri follows React-like declarative and lifecycle semantics, but it does not copy React Fiber as an implementation.

Nuri is a .NET-native lightweight retained runtime with virtual UI diffing:

```text
state update
  -> platform scheduler batching
  -> dirty component subtree render
  -> virtual tree diff
  -> minimal renderer patches
  -> effect flush after commit
```

Keep these React-like contracts:

- logical component identity based on parent, component type, and key;
- ordered hook slots and stable state across logical rerenders;
- keyed reconciliation and state preservation across moves;
- cleanup on key/type replacement and unmount;
- effects executed after commit.

Do not add React implementation machinery without a measured Nuri requirement:

- alternate/current Fiber trees;
- lanes and concurrent scheduling;
- interruptible or restartable rendering;
- browser DOM event delegation;
- Suspense-specific runtime machinery.

WPF Dispatcher, Duxel frame scheduling, and future renderer schedulers remain renderer-owned. Core owns neutral runtime identity, hooks, lifecycle, diffing, and patch descriptions.

## Renderer Priority

As of 2026-07-15, Duxel is the next UI backend development priority. Avalonia remains an existing adapter and regression baseline, but new backend parity, materialization, and sample expansion should target Duxel first unless the project is explicitly reprioritized.

Duxel development must preserve Core neutrality. The physical and solution folder `Nuri.Duxel` contains two projects: `src/Nuri.Duxel/Nuri.Duxel` owns immediate-mode frame projection and Duxel-specific property, event, and animation materialization, while `src/Nuri.Duxel/Nuri.Duxel.Windows` owns Windows application and frame-loop integration. Neither project may reshape Core around retained native-control assumptions.

## Current Runtime Shape

Runtime tree nodes are persistent in-memory identities. State, reducer, ref, memo, effect, and store hook data use runtime node references as ownership keys.

Each current `ComponentBase` object caches its assigned runtime node. The node is resolved once at the render boundary, and hook calls use that reference directly instead of locking and searching the global registry by `Component.Id`.

Strings such as `Component.Id` and `VirtualEntry.Id` remain for compatibility, diagnostics, virtual-tree lookup, and renderer patch targets. Tree ancestry and hook ownership must not be inferred by parsing those strings.

WPF and Avalonia application roots share `RenderCoordinator` for native build, patch, commit, and post-commit effect flushing. Both roots also use `ComponentInvalidationQueue`, so dirty-subtree coverage follows `RuntimeTreeIdentity` instead of renderer-specific string ancestry checks.

`Nuri.Duxel` is an immediate-mode renderer adapter and intentionally does not retain or patch a native control tree. A Nuri state invalidation requests a Duxel frame and renders/diffs only the covered dirty component virtual subtrees; every Duxel frame projects the latest committed `VirtualEntry` tree into `UiImmediateContext` commands. Effects flush after that projection is committed, and removed component state is still cleaned from the Nuri diff. The current projection covers linear layouts, nested Grid placement with Pixel/Auto/Star row and column tracks, synchronized row advancement, column spans, Scroll child regions, scoped margin, padding, and spacing, draw-list-backed Div background, border, and corner radius, explicit widget size, font size, solid foreground color, text and button horizontal alignment, input frame padding, and the baseline text/input/button/check event path. Opacity transitions use a stable virtual entry ID with Duxel `AnimateFloat`; linear/default and `CubicOut` easing, interruption from the current projected value, inherited subtree alpha, and continuation frame requests are supported. `Nuri.DuxelAnimatedDashboardSample` compiles the same platform-neutral `AnimatedDashboardComponent` as the WPF/Avalonia host. `Nuri.DuxelExplorerTreeSample` similarly shares the WPF Explorer component source files and exercises recursive keyed state and effect cleanup through Duxel frame projection. Duxel roots, invalidations, rebuilds, and patch batches are registered with `NuriDiagnostics`; known unsupported properties, animations, and opacity easing modes emit deduplicated diagnostics when diagnostics are enabled. Grid row spans remain future renderer work. The renderer project depends on `Duxel.App` `0.2.8-preview` and targets `net9.0`; the Windows host project depends on `Duxel.Windows.App` `0.2.8-preview` and Windows host samples target `net9.0-windows`. Duxel types remain outside Core.

`NuriApplication.Run(..., theme: ...)` accepts Duxel `UiTheme` presets while an omitted theme continues to follow the Windows app color scheme. `useDuxelTitleBar` and `integrateSystemChrome` are window-creation options and both default to `false`: the default Windows host keeps system-owned native chrome, enabling `integrateSystemChrome` lets Duxel supply native DWM caption/text/border colors, and enabling `useDuxelTitleBar` renders Duxel's title bar in the client area. Direct screen hosts can queue runtime palette changes with `NuriDuxelScreen.RequestTheme(...)`; Nuri components hosted by `NuriApplication` can receive a host-scoped `DuxelThemeController` and request the same change without global state. `NuriDuxelScreen.CurrentTheme` and `DuxelThemeController.CurrentTheme` are nullable until the first frame and then expose the palette observed from the frame Duxel actually rendered, rather than a request that is still pending. `DuxelThemeController.CurrentThemeChanged` fires only when that applied palette changes, so a component can synchronize hook state without causing a render loop. Duxel theme `UiColor` values can use the neutral color DSL directly, such as `.Background(theme.WindowBg)`, or be converted explicitly with `ToColorValue()`; the adapter preserves RGBA without adding Duxel types to Core. Duxel applies the requested palette at the next frame boundary while the mounted Nuri tree and hook state remain intact.

The Duxel host runs the same `DuxelAppSession` that receives Nuri frame requests, so idle-frame skipping wakes immediately for state and animation invalidations. Root content projects directly into the Duxel viewport work area instead of creating a movable nested Duxel window. `NuriDuxelScreen` snapshots `GetMainViewport().WorkPos` and `WorkSize` once per frame and passes that logical-coordinate rectangle as the explicit root bounds. With the default native title bar, Win32 client coordinates already exclude non-client chrome and the Nuri renderer does not subtract a title-bar inset. With the Duxel title bar, the host subtracts the configured Duxel title-bar height from its measured client height while Duxel supplies the matching viewport work-position inset. A Scroll Div is a viewport with at most one content child; multiple elements must be wrapped in a Column, Row, or Grid, and spacing belongs to that content layout rather than Scroll. Vertical containers propagate the available width but consume height in document order; an implicit-height Scroll region or Grid with a Star row receives the remaining height instead of every child inheriting the full parent height. Natural content that exceeds those bounds must opt into Scroll, as the shared Animated Dashboard root and Explorer detail panel do.

The Windows host records ordered pointer, wheel, key, text, focus, and actual `WM_SIZE` resize events in `DuxelInputEventQueue`; consecutive pointer-move and resize samples may coalesce, but semantic transitions and wheel samples retain their order and event-time position. Duxel 0.2.8's modal predictive `WM_SIZING` path projects proposed dimensions against the old swapchain before Win32 commits the rectangle, which visually reverses continuous grip movement. The Windows bridge therefore owns mouse edge and corner grips after native `WM_NCLBUTTONDOWN`, captures the pointer, and applies each movement with `SetWindowPos`. The message pump remains non-modal, so every resulting actual `WM_SIZE` reaches Duxel's normal snapshot and swapchain recreation path before Nuri projects the next frame. Pixel/Auto content, text, and native title-bar metrics remain fixed while Star tracks consume the changed client space, matching WPF-style layout in both grow and shrink directions.

Nuri animation, inertial Scroll motion, and pending input are composed into Duxel `IsAnimationActiveProvider`. Frame requests remain coalesced wake signals rather than a frame queue. Wheel capture at the Windows boundary uses only the event-time pointer position and whether a Nuri Scroll region has overflow; direction and the current offset are evaluated later while the renderer applies the ordered batch, preventing rapid direction reversals from being classified against a stale frame snapshot. The renderer converts wheel samples into per-region velocity impulses and advances offsets from Duxel `GetTime()` frame deltas. Repeated input in the current direction accelerates within a capped speed, opposite input decelerates or reverses the existing velocity, exponential friction settles motion, and bounds remove outward velocity. One consecutive wheel-direction run is presented per frame so equal rapid down/up runs do not cancel before presentation; same-direction samples remain batched. `NuriDuxelScreen` requests continuation frames while either deferred input or inertial Scroll motion remains. When this input queue is attached, the Duxel renderer owns Scroll hit regions, offsets, clipping, directional boundary consumption, and scrollbar drag so wheel input is applied before the affected content is projected in the same frame. Windows messages outside a Nuri-owned Scroll interaction continue through Duxel, preserving its existing keyboard, text, and IME behavior. Direct/non-Windows screen hosts without the queue retain the Duxel `BeginChild` Scroll path. A .NET metadata update requests a full root rebuild on the Duxel frame loop while retaining logical runtime state for stable component identities.

Grid tracks have no implicit gap. Missing definitions represent one implicit track; a constrained implicit row fills its arranged height, while an unconstrained implicit row measures its content. When an Auto row's projected height differs from its estimate, the remaining Star rows are recalculated inside the original arranged height instead of pushing later rows outside the client area.

For a direct/non-Windows host, `GetMainViewport().WorkSize` remains the default root size. The Windows host overrides it with logical client dimensions measured from `GetClientRect` and actual `WM_SIZE` messages. This makes the native title bar exclusion explicit and prevents stale or host-specific viewport work sizes from moving the bottom layout boundary.

See [RUNTIME_IDENTITY.md](RUNTIME_IDENTITY.md) for key, lifecycle, duplicate-key, and cleanup invariants.

### Duxel Theme Selection

The fixed-theme `NuriApplication.Run(theme, rootFactory, ...)` overload passes one selected `UiTheme` to both the root factory and the Duxel host. Use this overload when a component needs palette colors but does not need to change the host theme at runtime. The existing `NuriApplication.Run(..., theme: ...)` parameter remains the concise path when the root does not need the palette value, while `Func<DuxelThemeController, IElement>` remains the opt-in path for runtime switching and applied-theme observation.

## Flat Virtualized Items

Large renderer-owned lists use the platform-neutral `VirtualizedItems<T>(items, itemTemplate, ...)` contract. Fixed sizing remains the default fast path: `itemExtent` defaults to `36`, and the item buffer defaults to `5` before and after the viewport. One `buffer` value applies symmetrically; the `bufferBefore, bufferAfter` overload allows asymmetric values. Variable sizing is opt-in through `VirtualizedItems(items, itemTemplate, estimatedItemExtent: ..., bufferPixels: ...)`. The estimate positions unseen rows until a renderer measures them, and its buffer is a scroll length in pixels rather than an item count. The optional `itemKey` supplies stable identity across insertions, removals, and moves and also owns a measured extent. When it is omitted, identity is positional and row-local state or measured extents are not expected to follow a reordered item. The previous `VirtualizedItems<T>(items, keySelector, itemExtent, itemTemplate, comparer)` overload remains available for compatibility. Core copies the supplied items into an immutable render snapshot and emits `UpdateVirtualizedItemsPatch` with keyed add, remove, move, and update changes without invoking `itemTemplate`.

`itemTemplate` is lazy and stateless: it may return normal `IElement` trees, but it must not contain `Component` instances or hooks. Keys must be stable and unique. Duplicate keys produce `DuplicateKey` diagnostics and index-qualified fallback identities.

The WPF adapter materializes this contract with a recycling `VirtualizingStackPanel` and viewport-driven row preparation. Fixed sizing forces the container height and maps before/after buffers to an item-unit `VirtualizationCacheLength`; measured sizing leaves container height natural and maps `bufferPixels` to a pixel-unit cache length. Same-key source updates and visible keyed moves preserve the native item container, while filtering, empty/full replacement, duplicate-key fallback rows, repeated scrolling, and unload/reload restoration retain bounded realized work.

Duxel keeps the fixed path as fixed-extent immediate-mode row clipping. Its measured path retains extents by item identity, uses the estimate for unseen rows, and maintains cumulative scroll length in a Fenwick index so extent updates and offset-to-index lookup remain `O(log n)`. Each frame invokes `itemTemplate` only for the viewport plus the configured pixel buffer, learns realized row heights, and requests a stabilization frame when a measurement changes. The first visible item remains the scroll anchor while measurements before it change, preventing the viewport from jumping. Both the Windows/Nuri renderer-owned Scroll path and direct Duxel `BeginChild` path support measured extents; the fixed direct path continues to use `CalcListClipping`. Materialized row entries receive item-identity-derived widget ids, and `VirtualizedItemsSnapshot` reports the bounded per-frame projected count. `Nuri.DuxelVirtualExplorerTreeSample` exercises 10,101 fixed rows through this contract. Regular `Items(...)`, `ItemsTypes.Tree`, and eager child reconciliation remain unchanged. Avalonia does not materialize `ItemsTypes.Virtualized` yet.

WPF reconciliation keeps small structural edits incremental. It estimates the retained-key move count from the longest increasing subsequence of previous indexes; when adds, removes, and required moves exceed 256, it reuses retained item handles in one collection reset instead of issuing a quadratic sequence of native moves. The WPF Large List sample exercises 10,000-row update, swap, reverse, filter, add, remove, replace, reset, and selection paths.

The warmed `--explorer-comparison` WPF harness measured the same two-button row UI with 10,101 visible rows in a 700px viewport on 2026-07-14. Eager materialization took 4414.35 ms, allocated 543.17 MB, and created 10,101 native rows. Fixed-extent virtualization took 269.10 ms, allocated 3.27 MB, and created 19 native rows: 16.4x less materialization time and 166.2x less allocation in this local workload.

## Runtime Diagnostics

When `NuriDiagnostics` is enabled, each registered application root records applied diff-batch count, cumulative patch count, the last batch size, and its counts grouped by `PatchOperationType`. These counters cover `RenderCoordinator` rebuild batches after initial native materialization; renderer-internal realized-row diffs are not counted as root patch batches.

Application-root presence is registered independently of `NuriDiagnostics.IsEnabled`, while component, hook, log, and patch detail collection remains gated by it. This lets DevTools enabled after an application root has mounted discover that live root immediately. Root disposal always unregisters the presence record, including when diagnostics have been disabled, so late enablement cannot reveal stale roots.

Renderer-owned virtualized hosts may also publish neutral `VirtualizedItemsSnapshot` entries containing host id, virtual item count, and realized or projected row count. WPF and Duxel root disposal remove these entries deterministically. The WPF Large List stress screen displays the previous committed patch batch, cumulative patches, component render count, and realized row count so interactive operations expose full rebuilds or unbounded materialization.

When diagnostics are enabled, the WPF property path records `RuntimeLogKind.UnsupportedProperty` only after mapper, writable CLR property, and attached-property fallbacks all fail. Host-only window properties remain intentionally excluded. Messages identify the property and native control type and are deduplicated by native control CLR type plus property name until `NuriDiagnostics.ClearLogs()` is called.

The WPF event add path similarly records `RuntimeLogKind.UnsupportedEvent` when a neutral event cannot be converted or the mapped native event is absent on the target control. Native delegate compatibility remains unchanged, event removal does not emit warnings, and messages are deduplicated by native control CLR type plus source event name until `NuriDiagnostics.ClearLogs()` is called.

`Nuri.DevTools` is the canonical Duxel-backed diagnostics UI. It reads the platform-neutral `RuntimeSnapshot`, renders component tree, detail, hook, store, runtime-log, and console views through the Core DSL and `Nuri.Duxel`, and runs its Duxel window on a separate thread from an inspected WPF application. `NuriDiagnostics.Changed` requests a DevTools full rebuild directly instead of mutating a hook from the inspected renderer thread. Renderer-specific integration remains thin through Core's `INuriDebugHost`: WPF owns function-key routing and dispatches both snapshot capture and selected component highlighting to its Dispatcher, so the Duxel thread never traverses or highlights WPF-owned state directly.

The `NuriDevTools.OpenInspector(...)` and `RunInspector(...)` APIs accept an optional `snapshotProvider`. Their names distinguish opening the renderer-neutral inspector from creating an application window; the Duxel host remains an implementation detail. The former `Show(...)` and `Run(...)` names remain obsolete compatibility aliases. A retained renderer that owns a mutable virtual tree must supply a provider scheduled on its UI thread; the default direct `NuriDiagnostics.GetSnapshot` provider is appropriate only when the caller already guarantees safe snapshot access.

`NuriApplication.Create<TComponent>(...)` returns a lazy WPF application builder that does not mount a root until `Show()` or `Run()`. The `Nuri.DevTools` extensions `UseDebug()` and `UseDebug(DebugKey)` configure `F12` by default or any explicit `F1` through `F12` key without exposing WPF `Key` in the shared contract. Calling `UseDebug()` before startup enables diagnostics before the first render. A late call remains functional but records one `RuntimeLogKind.Diagnostics` warning and `Debug.WriteLine` message because earlier component, hook, store, and patch details cannot be reconstructed. Reconfiguration replaces the shortcut, and a closed host rejects configuration.

A DevTools `NuriDuxelScreen` uses `includeInDiagnostics: false`. Core diagnostics exclude that registered runtime root and its descendants from root, component, hook, invalidation, patch, virtualization, and supported renderer warning records, then release the exclusion when the screen is disposed. This prevents the inspector from observing its own render and producing a diagnostics-change/rebuild loop while leaving the inspected application diagnostics enabled. The current host shares diagnostics in-process; a later out-of-process transport may carry the same snapshot and highlight commands without changing the DevTools UI model.

The former native WPF DevTools window and `Nuri.WPF.DevTools` package have been removed. Renderer adapters may retain narrow integration helpers, such as `Nuri.WPF.Diagnostics.WpfElementHighlighter`, but the diagnostics UI itself belongs only to `Nuri.DevTools`.

## Neutral Transform Animation

Core exposes renderer-neutral `.Translate(x, y)`, `.TranslateX(...)`, `.TranslateY(...)`, `.Scale(value)`, `.Scale(x, y)`, `.ScaleX(...)`, and `.ScaleY(...)` DSL properties. Their `TranslateX`, `TranslateY`, `ScaleX`, and `ScaleY` values are scalar doubles and participate in `.Transition(duration, easing)` beside `Rotate`.

WPF materializes the transform properties in one centered `TransformGroup` ordered as Scale, Rotate, then Translate. Each axis owns an independent `DoubleAnimation`, so active animations can be replaced or removed without replacing the native control. The latest property value remains the animation base value; removing the properties restores Scale to `1`, Rotate to `0`, and Translate to `0`. `Nuri.WPFAnimatedDashboardSample` demonstrates the combined transform transition.

## Performance Baseline

Measured in Release on 2026-07-11. These values are a local baseline, not universal budgets. Compare future results on the same machine and workload; correctness counters are hard invariants.

Core runtime, 100 measured iterations after 10 warmups:

| Scenario | Size | Mean ms | Alloc KB | Required result |
|---|---:|---:|---:|---:|
| Keyed reorder diff | 1,000 | 1.5134 | 548.55 | 1 patch |
| Stable render with state hooks | 1 | 0.0006 | 0.25 | 1 hook |
| Stable render with state hooks | 10 | 0.0018 | 1.48 | 10 hooks |
| Stable render with state hooks | 50 | 0.0102 | 6.95 | 50 hooks |
| Keyed component state mount/dispose | 1,000 | 3.1037 | 1596.31 | 1,000 disposed states |
| Parent/child invalidation coalescing | 1,000 children | 0.3749 | 226.81 | 1 retained parent invalidation |
| Effect mount/unmount | 1,000 | 2.1434 | 1895.83 | 1,000 cleanups |

An editor-shaped focused run on 2026-07-19 exposed allocation in unchanged-order keyed reconciliation. An aligned-key fast path now retains existing virtual IDs and diffs children directly, while reordered, added, removed, and duplicate-key cases keep the general reconciliation path.

The WPF phase comparison compiles the same benchmark source once against published Nuri.WPF 0.2.0 and once against the current source. Values below are medians from 5 independent Release processes, each with 30 warmups and 300 measured iterations over 1,000 eager keyed lines. Gen0 is the collection count for all 300 measured iterations.

| Phase | Package 0.2.0 ms / KB / Gen0 | Current source ms / KB / Gen0 | Required result |
|---|---:|---:|---:|
| Virtual tree creation | 0.7947 / 695.74 / 25 | 0.7697 / 695.74 / 25 | 1,000 entries |
| VirtualTreeDiff | 1.3372 / 523.76 / 19 | 0.8431 / 180.98 / 6 | 1 patch |
| WPF initial build | 6.3666 / 1393.81 / 51 | 6.2872 / 1393.81 / 51 | 1 root |
| WPF property patch | 0.0003 / 0.04 / 0 | 0.0003 / 0.04 / 0 | 1 patch |
| Full sequential update | 0.8217 / 1219.54 / 44 | 0.5815 / 876.77 / 32 | 1 patch |

The isolated diff was about 36.9% faster, allocated about 65.4% less, and triggered 68.4% fewer Gen0 collections. The full virtual-tree creation, diff, and WPF patch path was about 29.2% faster, allocated about 28.1% less, and triggered 27.3% fewer Gen0 collections. This supports retaining the fast path. A slower interactive sample result must be investigated above these phases in hook, Dispatcher, effect, layout, or measurement behavior rather than attributed to the aligned-key loop from that observation alone. Nuri.DuxelEditorStressSample keeps the related workload interactive with 100,000 virtual lines.

WPF renderer, 100 measured iterations after 10 warmups:

| Scenario | Size | Mean ms | Alloc KB | Required patch count |
|---|---:|---:|---:|---:|
| Initial native build | 1,000 | 10.0946 | 1393.99 | 0 |
| Keyed native reorder | 1,000 | 6.1023 | 1603.84 | 1 |

Invalidation coalescing uses constant-time enqueue deduplication and runtime-parent traversal. The 1,000-child result improved from 3.9679 ms / 673.73 KB to 0.3749 ms / 226.81 KB while retaining exactly one parent invalidation.

The 10,000-child stress comparison used 10 iterations before optimization and 30 confirmation iterations after optimization:

| Version | Mean ms | Alloc KB | Retained invalidations |
|---|---:|---:|---:|
| Before | 232.4499 | 6820.32 | 1 |
| After | 3.0302 | 2224.42 | 1 |

Timing improved by about 76x in the confirmation run and allocation fell by about 67%. Treat result count `1` as the correctness invariant; timing remains environment-dependent.

Runtime-node caching was measured separately with 100,000 stable render iterations. Current focused results were 0.0005 ms for 1 state hook, 0.0022 ms for 10 hooks, and 0.0046 ms for 50 hooks. Hook-render allocation stayed at 0.25 KB, 1.48 KB, and 6.95 KB because caching removes registry lookup rather than hook-value allocation. The cached reference adds about 8 bytes per transient component object; the 1,000-component mount scenario increased by about 7.8 KB. Short standard runs remain noise-sensitive, so this change is justified primarily by removing per-hook registry locking and simplifying ownership, not by claiming a fixed speedup ratio.

Disabled-diagnostics hook formatting was optimized on 2026-07-12. The before and after values are medians from 7 independent Release processes, each using 10 warmups and 100 measured iterations. Value and dependency summaries are now formatted only when `NuriDiagnostics.IsEnabled` is `true`; enabled diagnostics retain the same hook kind, display type, and summary.

| Scenario | Before alloc KB | Expected alloc KB | After alloc KB | Required result |
|---|---:|---:|---:|---:|
| Stable render with state hooks | 1: 0.25 | 0.18-0.22 | 0.23 | 1 hook |
| Stable render with state hooks | 10: 1.48 | 0.95-1.15 | 1.24 | 10 hooks |
| Stable render with state hooks | 50: 6.95 | 4.40-5.40 | 5.78 | 50 hooks |

The 50-hook allocation fell by about 16.8%, but did not reach the expected upper bound of 5.40 KB. The result indicates that the change removed about 24 bytes per hook from disabled summary formatting while setter delegates, closures, and other hook costs remain. Treat this as a measured partial success rather than evidence for a broader hook-store rewrite.

State setter reuse was measured on 2026-07-12 with the same 7-process, 10-warmup, 100-iteration method. Release IL confirmed that the previous `useState<T>` path created one display class and one setter delegate per hook on every render. State slots now retain one setter for the logical runtime node and hook index, and update its current CLR component owner on subsequent renders.

| Scenario | Before alloc KB | Expected alloc KB | After alloc KB | Before/after median ms | Required result |
|---|---:|---:|---:|---:|---:|
| Stable render with state hooks | 1: 0.23 | 0.10-0.15 | 0.12 | 0.0009 / 0.0013 | 1 hook |
| Stable render with state hooks | 10: 1.24 | 0.10-0.20 | 0.15 | 0.0018 / 0.0022 | 10 hooks |
| Stable render with state hooks | 50: 5.78 | 0.10-0.30 | 0.31 | 0.0063 / 0.0077 | 50 hooks |

The 50-hook stable allocation fell by about 94.6%. It missed the expected upper bound by 0.01 KB, and short 100-iteration latency increased, so this remains a partial success under the predeclared thresholds. The 1,000-component keyed mount allocation also fell from 1580.69 KB to 1557.25 KB and retained exactly 1,000 disposed states. Its after-time median was 6.6388 ms with a 5.9019-7.7229 ms range.

The short run does not include enough allocation pressure to represent GC cost. A separate sustained comparison used the committed pre-change runtime and the working runtime in isolated worktrees. Each of 7 independent Release processes performed 10,000 warmups and 100,000 measured renders with 50 state hooks:

| Version | Median total ms | Renders/sec | Alloc/render KB | Total alloc MB | Gen0 | Required result |
|---|---:|---:|---:|---:|---:|---:|
| Before | 626.04 | 159734 | 5.77 | 563.83 | 70 | 50 hooks |
| After | 197.44 | 506491 | 0.30 | 29.76 | 3 | 50 hooks |

Sustained elapsed time fell by about 68.5%, throughput increased by about 3.17x, total allocation fell by about 94.7%, and Gen0 collections fell by about 95.7%. This supports the setter reuse for sustained hook-heavy workloads while retaining the documented short-latency tradeoff.

Application-shaped WPF and invalidation validation was added on 2026-07-12. The WPF scenario uses the same header/input/keyed-list virtual structure as `Nuri.TodoValidationSample`; it is intentionally a renderer harness, not a direct sample assembly reference. Values are medians from the committed before runtime and the working runtime under the same Release harness.

| Scenario | Before | After | Alloc before/after KB | Required result |
|---|---:|---:|---:|---:|
| Todo-shaped initial build, 1,000 items | 6.1149 ms | 6.2102 ms | 1398.58 / 1398.58 | 0 patches |
| Todo-shaped keyed reorder, 1,000 items | 6.4502 ms | 6.2962 ms | 1608.87 / 1608.87 | 1 patch |
| Invalidation enqueue, 1,000 children | 0.1941 ms | 0.1862 ms | 147.33 / 147.33 | pending result 1 |
| Parent/child coalescing, 1,000 children | 0.3843 ms | 0.3189 ms | 226.81 / 226.81 | retained result 1 |

The differences are within environment noise and do not justify additional queue or renderer complexity. The new scenarios remain as regression and measurement coverage; no further runtime optimization was made in this slice.

The expanded harness also establishes future baselines not used in the before/expected/after verdict: stable 100-hook render was 0.50 KB and 0.0178 ms; first mount results for 1, 10, 50, and 100 hooks were 1.33/3.10/11.21/22.34 KB with median times of 0.0052/0.0053/0.0136/0.0257 ms. Setter identity, latest-owner invalidation, functional updates, hook-slot isolation, trimming, and disposal are covered by regression tests.

Stale setter protection was added on 2026-07-13. `useState` setters and `useReducer` dispatchers retain their owning runtime-node reference and become no-ops when that exact node is no longer registered, including when a replacement later reuses the same string ID. A 100-iteration sanity run retained 0.12/0.15/0.31/0.50 KB stable-render allocation for 1/10/50/100 hooks and the required keyed reorder result of 1 patch. The extra runtime-node reference increased first-mount allocation to 1.34/3.18/11.57/23.11 KB, approximately 8 bytes per state hook; it does not add stable-render allocation.

## Measurement Scenarios

The Core performance harness covers:

- stable component renders with 1, 10, and 50 state hooks;
- 1,000 keyed components mounting state and disposing their runtime nodes;
- a parent and 1,000 dirty children coalescing into one subtree rebuild request;
- 1,000 effects mounting and cleaning up;
- a 1,000-entry keyed reorder retaining a single patch.

The test suite separately verifies:

- keyed state and effect identity;
- key replacement cleanup and mount order;
- nested and consecutive navigation updates;
- duplicate-key isolation and diagnostics;
- runtime ancestry registry cleanup after root disposal.

## Optimization Order

Use measurements before adding runtime complexity. If hook-heavy scenarios regress materially, optimize in this order:

1. Replace per-kind hook dictionaries with compact ordered hook slots where allocation data justifies it.
2. Reduce transient component or closure allocation only after profiling shows meaningful GC pressure.
3. Consider scheduling sophistication only when renderer batching and subtree rendering are insufficient.

Do not trade patch count, deterministic cleanup, keyed state preservation, or platform neutrality for small isolated timing gains.

## Validation

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet run --project "tests\Nuri.RendererTests\Nuri.RendererTests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```

For performance changes, retain the TSV output in the review or handoff notes and compare before/after results from the same environment.
