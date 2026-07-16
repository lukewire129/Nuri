# Nuri Session Handoff

This file is intentionally short. Use it only when resuming work after the local session/context is gone.

## Current Goal

Maintain Nuri as a platform-neutral Core virtual UI/runtime/diff model, with WPF, Avalonia, and Duxel attached through Core contracts. Split Duxel into a platform-neutral immediate-mode renderer project and a Windows host project under one physical and solution folder, using Duxel `0.2.8-preview`. Direct the next UI backend implementation work to Duxel while preserving Avalonia as an existing regression baseline.

## Renderer Priority Decision

- As of 2026-07-15, Duxel is the next UI backend development priority instead of Avalonia.
- Preserve the existing Avalonia adapter, samples, and regression coverage, but do not direct the next parity, materialization, or sample-expansion slices to Avalonia unless the project is explicitly reprioritized.
- Drive Duxel work from focused samples. Keep immediate-mode projection in `src/Nuri.Duxel/Nuri.Duxel` and Windows host integration in `src/Nuri.Duxel/Nuri.Duxel.Windows`, outside Core.

## Current Status

- The Core project at `src/Nuri` is named `Nuri` and exposes `Nuri.*` namespaces. It owns platform-neutral virtual entries, patch operations, diffing, runtime, state store, UI element abstractions, and value models.
- `Nuri.WPF` references the Core project and acts as the WPF renderer/materializer adapter.
- WPF controls are now created by `WpfControlRegistry`/`WpfVirtualEntryRenderer`, not by component render methods directly.
- Dirty component subtree render/diff/patch and Dispatcher batching are implemented.
- WPF and Avalonia roots share `RenderCoordinator` for native patch, commit, and post-commit effect flushing, and both use `ComponentInvalidationQueue` with `RuntimeTreeIdentity` coverage.
- Core DSL state changes are batched by each renderer root; non-root component hooks render/diff/patch only that component subtree when the current virtual entry can be found, with root rebuild as fallback.
- Keyed reconciliation is implemented with `MoveChildPatch` and WPF move support.
- A simple perf harness exists under `perf/` for basic before/after measurements.
- The 2026-07-14 Release validation, including `Nuri.RendererTests`, completed with zero warnings and zero errors while the preview host was not running. If `Nuri.WPF.PreviewHost.dll` is locked by a live preview process, close it before rebuilding.

## Recent Feature Additions

- Explicit `.Key("...")` API added.
- `Name` still works as a key fallback for compatibility.
- Duplicate virtual keys are diagnosed and fall back to index diff. Duplicate component keys also emit `RuntimeLogKind.DuplicateKey` and use position-based hook identity so state/effects cannot collide.
- Keyed component lifecycle uses explicit component keys. Key changes clean up the old logical component and mount the replacement.
- Newly added keys receive key-derived virtual-entry IDs instead of reusing removed-key patch IDs. Reuse could dispose a newly remounted effect when a key sequence returned from `a` to `b` to `a`.
- Runtime subtree cleanup, diagnostics cleanup, and dirty-component coalescing use the in-memory `RuntimeTreeIdentity` ancestry registry instead of parsing ID delimiters.
- `ComponentBase` caches its assigned runtime node, so hook access does not repeat global registry lookup; detached nodes are refreshed before reuse.
- `useState` setters and `useReducer` dispatchers retain their owning runtime-node identity and become no-ops after unmount, so stale async completions cannot invalidate a replacement that reuses the same string ID.
- `ComponentInvalidationQueue` uses hash-based enqueue deduplication and runtime-parent traversal; the documented 10,000-child stress case retains one parent invalidation at about 3.03 ms versus 232.45 ms before optimization.
- The authoritative identity/key/hook invariants and regression checklist are in `docs/RUNTIME_IDENTITY.md`.
- The runtime architecture decision, non-goals, performance scenarios, and measured baseline are in `docs/RUNTIME_ARCHITECTURE.md`.
- Core-neutral event foundation added:
  - `VirtualEvent`
  - `VirtualEventKind`
  - neutral `.OnClick(Action)`
  - neutral `OnTextChanged(Action<string>)`
  - neutral `OnContentChanged(Action<object>)`
  - neutral `OnCheckChanged(Action<bool>)`
  - neutral hover, mouse down/up, keyboard down/up, focus-change, and loaded/unloaded compatibility events
- Existing WPF delegate overloads are still kept for compatibility.
- WPF and Avalonia adapters materialize supported Core `VirtualEvent` descriptions into native events. Duxel materializes its supported baseline event subset during immediate-mode frame projection.
- Component mount/unmount behavior belongs to `useEffect(..., [])` and its cleanup. Loaded/unloaded remain element-level compatibility events and are not a target for further component lifecycle expansion.
- Core-neutral DSL foundation added under `src/Nuri/UI/Dsl` with public namespace `Nuri.UI.Dsl`.
  - New code can use Core `Component`, `IElement`, `Div`, `Text`, `Input`, and neutral extension methods without referencing WPF Controls.
  - `ApplicationRoot.Initialize(Nuri.UI.IElement, Window)` can render Core DSL trees through the WPF renderer.
  - Core DSL now includes neutral layout/content alignment, grid placement, border, padding, font, cursor, and image source descriptions.
  - WPF maps Core alignment values and string image sources during renderer/property materialization.
  - Core `ContentControl`, `VirtualContentControl`, and `VirtualElement` wrapper layers were removed; content is represented as virtual properties/children, not a WPF-style inheritance branch.
  - Core DSL now includes neutral hover, image scaling, font weight helpers, and transition descriptions.
  - `GridTest`, `RouterSample`, `BorderTest`, `ContentControlChildrenTest`, and template component files were migrated to Core DSL component/render types.
  - `DiffingEngineTest` was migrated after Core nested component expansion was added.
  - `src/Nuri.WPF/Controls` was removed. WPF now provides renderer/adapter/materialization, not a framework-specific DSL/control layer.
  - Component instances remain in the Core virtual element tree until adapter conversion; `VirtualEntryAdapter` renders component boundaries using their final assigned IDs so nested component subtree updates can target the correct virtual entry.
- Public Core namespaces and project paths use `Nuri`.
- Sample/template entry points now use WPF `App.xaml` + `App.xaml.cs`, calling `NuriApplication.Run<TComponent>("Title", width, height)`.
- `NuriApplication` owns native WPF window creation, root creation/registration, application-level hot reload, and component state-change routing across all registered roots, including subwindows.
- `WindowView` remains the internal virtual root wrapper for window title/size/content, but samples do not expose it directly.
- Each native window gets its own `ApplicationRoot` instance and unique tree prefix, so separate windows build separate virtual trees instead of sharing ids.
- Component cleanup is stronger now: removed and replaced component subtrees are disposed.
- `src/Nuri.Avalonia` contains the Avalonia renderer and references Avalonia desktop packages.
- The Avalonia adapter includes application-root scheduling, virtual-entry rendering, control/property/event mapping, hot reload support, and a smoke-test sample. It is no longer a renderer skeleton.
- The Duxel physical and solution folder is `Nuri.Duxel`. `src/Nuri.Duxel/Nuri.Duxel` is the renderer package, targets `net9.0`, and references `Duxel.App` `0.2.8-preview`. `src/Nuri.Duxel/Nuri.Duxel.Windows` is the Windows host package, targets `net9.0`, references the renderer plus `Duxel.Windows.App` `0.2.8-preview`, and owns `NuriApplication`/`DuxelAppSession` execution.
- `NuriDuxelScreen` projects root content directly into the Duxel viewport work area, so Nuri content is not represented by a draggable nested Duxel window. Direct hosts use `WorkPos`/`WorkSize`; the Windows host overrides the size with logical client dimensions measured from `GetClientRect` and actual `WM_SIZE` messages. The native Windows title bar is already excluded; do not subtract a title-bar inset again. The Windows host must run the same `DuxelAppSession` whose `RequestFrame` callback is supplied to the screen, otherwise idle-frame skipping can delay state and animation updates.
- Duxel Grid projection uses nested-safe renderer-owned tracks rather than legacy `UiImmediateContext.Columns`. Pixel/Auto/Star rows and columns, tallest-cell Auto row advancement, Auto-to-Star reallocation, zero default track spacing, one WPF-compatible implicit track, and column spans are materialized; row spans remain unsupported and diagnostic.
- Duxel vertical layout passes width through but consumes height in document order. An implicit-height Scroll region or Grid with a Star row receives the remaining height. The shared Animated Dashboard root and Explorer detail panel use Scroll so their natural content stays reachable at smaller window sizes.
- `Nuri.Duxel.Windows/WindowsInputEventBridge.cs` uses the Windows subclass API after Duxel creates the HWND. It records ordered pointer, wheel, key, text, focus, and resize events without replacing Duxel's keyboard/text/IME path. Only wheel and left-pointer interactions over a published Nuri Scroll hit region are consumed by Nuri.
- The Windows bridge owns mouse resize grips after native `WM_NCLBUTTONDOWN`. It captures the pointer and applies each edge/corner movement with `SetWindowPos` instead of entering Duxel 0.2.8's modal predictive `WM_SIZING` loop. Every resulting actual `WM_SIZE` reaches Duxel's normal snapshot and swapchain path, then Nuri reflows against the new client work area. Pixel/Auto content and native title-bar metrics remain fixed while Star tracks consume the changed space, matching WPF-style layout in both grow and shrink directions.
- `DuxelInputEventQueue` preserves semantic transition and wheel ordering plus event-time positions; consecutive pointer-move and resize samples coalesce. Frame requests remain wake signals and are not queued as historical frames.
- With that queue attached, `DuxelVirtualEntryRenderer` owns Scroll offset, clipping, directional boundary routing, scrollbar rendering, and handle dragging. The Windows boundary captures vertical wheel samples by event-time pointer position and overflow only; the renderer evaluates direction and current offset while applying the ordered batch, so rapid reversals do not depend on a stale frame snapshot. Wheel samples add per-region velocity impulses; repeated same-direction input accelerates up to a cap, opposite input decelerates or reverses existing motion, Duxel `GetTime()` frame deltas advance offsets, exponential friction settles motion, and bounds remove outward velocity. One consecutive direction run is presented per frame, and the screen requests continuation frames for deferred input or active Scroll motion. Direct screen hosts without the queue retain Duxel `BeginChild` scrolling.
- `tests/Nuri.DuxelRendererTests` covers the title-bar-excluded work area at the Basic (720x580), Explorer (1120x720), and Dashboard (900x640) sample sizes, plus Explorer 800x500 and Dashboard 700x480 resize cases.
- `src/Nuri.Duxel/Nuri.Duxel.Windows/HotReloadService.cs` registers the .NET metadata update handler. An update calls `NuriDuxelScreen.RequestFullRebuild()`, requests the owning Duxel frame, and rebuilds the root while retaining stable logical component state.
- The 2026-07-16 Duxel headless performance sanity run after the ordered-input/Nuri Scroll slice measured 1.3290 ms / 468.55 KB for initial 1,000-entry projection and 2.6399 ms / 622.78 KB with exactly 1 patch for keyed reorder. The Todo-shaped initial/reorder results were 3.1722/2.2249 ms and retained 0/1 patches. Treat timings as local sanity data; patch counts remained stable.
- `samples/Duxel/Nuri.DuxelSample` validates Nuri hook state, neutral click/text/check events, Grid, Scroll, size, padding/spacing, and scoped text styling through the Windows host. Duxel `0.2.8-preview` supports `net9.0` and `net10.0`; the current Nuri renderer targets `net9.0`, and Windows samples target `net9.0-windows`.
- `tests/Nuri.RendererTests` validates post-commit effects, subtree and key-replacement cleanup, repeated keyed native moves, and idempotent root disposal without external test packages. WPF coverage also runs 50-cycle mount/unmount, key replacement, and move stress cases.
- WPF root disposal clears pending component invalidations and ignores Dispatcher callbacks that were posted before disposal, preventing effects from remounting on a closed root.
- WPF input-event coverage raises click, text/check, hover, mouse, keyboard, and focus events on native controls, replaces handlers through 50 rebuilds without duplication, and verifies recursive handler detachment on subtree removal and root disposal.
- `samples/WPF/Nuri.ExplorerTreeSample` exercises recursive keyed components, expand/collapse cleanup, selection, rename, and add/delete flows through the WPF renderer.
- `samples/WPF/GridTest` demonstrates `.Key(...)`, neutral `.OnClick(Action)`, and the preferred `Grid(...).Rows(...).Columns(...)` layout style.
- Legacy `Div(Rows(...), Columns(...), children...)` overloads remain for compatibility, but new code should prefer fluent layout definitions so row/column definitions do not look like child controls.
- Core DSL now exposes WPF-familiar factory aliases such as `Button`, `TextBox`, `CheckBox`, `RadioButton`, `ToggleButton`, and `PasswordBox`. These are semantic aliases over Nuri virtual input descriptions, not WPF types in Core.
- Animation DSL supports `.Transition(duration, easing)` for configured supported properties. Existing `.Transitions("Property", ...)` remains for explicit compatibility.
- Core DSL exposes `.Opacity(...)`; WPF and Avalonia now materialize opacity transitions, replace repeated transitions without duplicate native registrations, and remove native transitions when the virtual animation is removed.
- `Nuri.AnimatedDashboardSample` runs the same Core-neutral dashboard component through WPF by default or Avalonia with `--avalonia` and exercises opacity interruption/replacement.
- `Nuri.WPFAnimatedDashboardSample` exercises WPF Margin, background, foreground, Rotate, Translate, and Scale transition replacement. WPF composes Scale, Rotate, and Translate in one transform group and preserves each latest base value through animation replacement and removal.
- `Nuri.RendererTests` covers cross-renderer opacity transition add, replacement, and removal behavior.
- `Nuri.RendererTests` also covers WPF Margin, background, foreground, Rotate, Translate, and Scale native animation replacement, base values, transition removal, and property reset.
- Runtime diagnostics track component render counts, duplicate keys, root patch batches grouped by `PatchOperationType`, and virtualized item/realized-row counts. WPF also records deduplicated `UnsupportedProperty` entries after every supported property fallback fails and `UnsupportedEvent` entries when event conversion or native event lookup fails.
- `Nuri.LargeListSample` is now the WPF 10,000-row stress screen for update, swap, reverse, filter, add, remove, replace, reset, and selection operations. It displays the previous committed patch batch, cumulative patches, component renders, and realized native rows.
- WPF virtualized reconciliation keeps small keyed edits incremental and switches to one retained-handle collection reset when adds, removes, and LIS-derived moves exceed 256, avoiding quadratic large-reorder behavior.
- `Nuri.MultiWindowSample` exercises per-window hook-state isolation, shared `Store` updates, root registration, and window cleanup. Renderer coverage verifies that closing one WPF window unregisters only its root, disposes its effect and Store subscription, and makes its stale setter inert while remaining roots continue updating.
- WPF `NuriApplication.Run<TComponent>` uses `ShutdownMode.OnMainWindowClose`, so closing its MainWindow closes all remaining windows and disposes every registered root. `Show<TComponent>` alone preserves the caller's existing shutdown policy.

## Important Files

- `src/Nuri/VirtualDom/VirtualEntry.cs`
- `src/Nuri/VirtualDom/VirtualTreeDiff.cs`
- `src/Nuri/VirtualDom/PatchOperation.cs`
- `src/Nuri/UI/IElement.cs`
- `src/Nuri/UI/Element.cs`
- `src/Nuri/UI/ComponentBase.cs`
- `src/Nuri/Runtime/RuntimeTreeIdentity.cs`
- `docs/RUNTIME_IDENTITY.md`
- `docs/RUNTIME_ARCHITECTURE.md`
- `src/Nuri/UI/Dsl/IElement.cs`
- `src/Nuri/UI/Dsl/Component.cs`
- `src/Nuri/UI/Dsl/SemanticElements.cs`
- `src/Nuri/UI/Dsl/ElementExtensions.cs`
- `src/Nuri/UI/Values/AlignmentValue.cs`
- `src/Nuri/UI/Values/ImageScalingModeValue.cs`
- `src/Nuri/UI/Events/VirtualEvent.cs`
- `src/Nuri.WPF/ApplicationRoot.cs`
- `src/Nuri.WPF/VirtualDom/Bridge/VirtualEntryAdapter.cs`
- `src/Nuri.WPF/VirtualDom/Rendering/WpfVirtualEntryRenderer.cs`
- `src/Nuri.WPF/VirtualDom/Rendering/WpfControlRegistry.cs`
- `src/Nuri.Avalonia/AvaloniaApplicationRoot.cs`
- `src/Nuri.Avalonia/AvaloniaVirtualEntryRenderer.cs`
- `src/Nuri.Avalonia/AvaloniaControlRegistry.cs`
- `src/Nuri.Avalonia/AvaloniaPropertyMapper.cs`
- `src/Nuri.Avalonia/AvaloniaEventMapper.cs`
- `src/Nuri.Duxel/Nuri.Duxel/NuriDuxelScreen.cs`
- `src/Nuri.Duxel/Nuri.Duxel/DuxelVirtualEntryRenderer.cs`
- `src/Nuri.Duxel/Nuri.Duxel.Windows/NuriApplication.cs`
- `src/Nuri.Duxel/Nuri.Duxel.Windows/HotReloadService.cs`
- `perf/Nuri.Performance/Program.cs`
- `perf/Nuri.WPFPerformance/Program.cs`

## Validation Commands

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet run --project "tests\Nuri.RendererTests\Nuri.RendererTests.csproj" -c Release
dotnet run --project "tests\Nuri.DuxelRendererTests\Nuri.DuxelRendererTests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.DuxelPerformance\Nuri.DuxelPerformance.csproj" -c Release -- --label after
```

Expected baseline sanity for keyed reorder:

- Patch count should be `1`.
- Allocations and timings can vary by machine/load.

## Next Feature Priorities

1. Validate and refine current neutral event semantics across WPF and Duxel instead of adding baseline event kinds.
   - Click, value changes, hover, mouse down/up, keyboard down/up, focus changes, and loaded/unloaded compatibility events already exist.
   - Use `useEffect(..., [])` plus cleanup for component mount/unmount.
   - Let samples justify richer pointer and keyboard payloads such as modifiers, repeat state, handled state, coordinates, or device information.
   - Keep Avalonia event behavior as a regression baseline rather than the next expansion target.

2. Complete platform-neutral animation behavior.
   - Keep WPF `AnimationTimeline` out of Core.
   - Expand `AnimationValue` and supported properties only for demonstrated transitions.
   - Opacity materialization in WPF and Avalonia, plus WPF Margin, color, and transform replacement/removal, remain regression baselines.
   - Add Duxel animation materialization from focused sample needs while keeping frame-specific behavior in `src/Nuri.Duxel`.
   - Add actionable unsupported-animation diagnostics.

3. Strengthen renderer-level lifecycle/effect coverage.
   - Verify effects flush after WPF native commit and Duxel frame projection.
   - Cover subtree removal, key replacement, repeated keyed moves, frame invalidation, and root/window disposal in the applicable renderer model.

4. Improve WPF/Duxel semantic parity.
   - Audit property, event, animation, lifecycle, frame invalidation, and host behavior.
   - Compare user-visible semantics without forcing retained native-control mechanics onto Duxel.
   - Keep all native materialization outside Core.

5. Add diagnostics where they solve observed debugging problems.
   - Render count, duplicate-key, patch-count, and virtualized-row diagnostics already exist.
   - WPF unsupported property and event warnings are implemented; use sample findings to prioritize actual mapping gaps.

6. Drive the next Core refinements from focused samples.
   - Start with `Nuri.DuxelSample`, then add Duxel-focused Explorer Tree coverage for recursive keyed subtrees and lifecycle cleanup.
   - Use a Duxel Animated Dashboard slice for transition coverage.
   - Use a Duxel Stress slice for reorder/filter/replacement diagnostics and frame invalidation.
   - Add Duxel Multi-Window coverage when host capabilities support root registration and window lifecycle boundaries.

## Documentation Policy

- Do not create broad documentation for every detail yet.
- Keep one handoff file for session recovery.
- Add focused docs only when they prevent repeated rediscovery.
- Prefer examples in samples over long explanations.
- If a future task needs context, read this file first, then read only the relevant source files listed above.

## Cautions

- Do not put WPF types in the Core project at `src/Nuri`.
- Do not remove existing WPF delegate overloads unless compatibility is intentionally broken.
- Do not replace `Name` fallback immediately; persisted/sample DSL may rely on it.
- Keep Avalonia stable as an existing regression baseline; do not spend the next backend expansion slices there unless the project is reprioritized.
- Keep `Nuri.Duxel` on `Duxel.App` and `Nuri.Duxel.Windows` on `Duxel.Windows.App`; both are pinned to `0.2.8-preview` in this slice. Do not collapse the Windows host back into the renderer package.
- Keep Duxel Grid nesting independent from Duxel's single active Table state. Do not replace the renderer-owned track layout with nested `BeginTable` calls unless Duxel adds a stack-safe Table scope.
- Do not trust perf timings from one run; use patch count plus repeated measurements.
