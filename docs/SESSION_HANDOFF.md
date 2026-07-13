# Nuri Session Handoff

This file is intentionally short. Use it only when resuming work after the local session/context is gone.

## Current Goal

Maintain Nuri as a platform-neutral Core virtual UI/runtime/diff model, with WPF and Avalonia as renderer adapters and additional renderers attachable through Core contracts later.

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
- WPF and Avalonia adapters materialize supported Core `VirtualEvent` descriptions into native events.
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
- `tests/Nuri.RendererTests` validates WPF/Avalonia post-commit effects, subtree and key-replacement cleanup, repeated keyed native moves, and idempotent root disposal without external test packages.
- `samples/WPF/Nuri.ExplorerTreeSample` exercises recursive keyed components, expand/collapse cleanup, selection, rename, and add/delete flows through the WPF renderer.
- `samples/WPF/GridTest` demonstrates `.Key(...)`, neutral `.OnClick(Action)`, and the preferred `Grid(...).Rows(...).Columns(...)` layout style.
- Legacy `Div(Rows(...), Columns(...), children...)` overloads remain for compatibility, but new code should prefer fluent layout definitions so row/column definitions do not look like child controls.
- Core DSL now exposes WPF-familiar factory aliases such as `Button`, `TextBox`, `CheckBox`, `RadioButton`, `ToggleButton`, and `PasswordBox`. These are semantic aliases over Nuri virtual input descriptions, not WPF types in Core.
- Animation DSL supports `.Transition(duration, easing)` for configured supported properties. Existing `.Transitions("Property", ...)` remains for explicit compatibility.
- Core DSL exposes `.Opacity(...)`; WPF and Avalonia now materialize opacity transitions, replace repeated transitions without duplicate native registrations, and remove native transitions when the virtual animation is removed.
- `Nuri.AnimatedDashboardSample` runs the same Core-neutral dashboard component through WPF by default or Avalonia with `--avalonia` and exercises opacity interruption/replacement.
- `Nuri.RendererTests` covers cross-renderer opacity transition add, replacement, and removal behavior.
- Runtime diagnostics already track component render counts and duplicate keys. Patch-count and unsupported property/event diagnostics remain candidates when a concrete sample needs them.

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
- `perf/Nuri.Performance/Program.cs`
- `perf/Nuri.WPFPerformance/Program.cs`

## Validation Commands

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet run --project "tests\Nuri.RendererTests\Nuri.RendererTests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```

Expected baseline sanity for keyed reorder:

- Patch count should be `1`.
- Allocations and timings can vary by machine/load.

## Next Feature Priorities

1. Validate and refine current neutral event semantics instead of adding baseline event kinds.
   - Click, value changes, hover, mouse down/up, keyboard down/up, focus changes, and loaded/unloaded compatibility events already exist.
   - Use `useEffect(..., [])` plus cleanup for component mount/unmount.
   - Let samples justify richer pointer and keyboard payloads such as modifiers, repeat state, handled state, coordinates, or device information.

2. Complete platform-neutral animation behavior.
   - Keep WPF `AnimationTimeline` out of Core.
   - Expand `AnimationValue` and supported properties only for demonstrated transitions.
   - Opacity materialization is covered in WPF and Avalonia; use the Animated Dashboard to drive margin, color, and rotation parity.
   - Add actionable unsupported-animation diagnostics.

3. Strengthen renderer-level lifecycle/effect coverage.
   - Verify effects flush after native commit in WPF and Avalonia.
   - Cover subtree removal, key replacement, repeated keyed moves, and root/window disposal.

4. Improve WPF/Avalonia parity.
   - Audit property, event, animation, hot reload, and semantic-control behavior.
   - Keep all native materialization outside Core.

5. Add diagnostics where they solve observed debugging problems.
   - Render count and duplicate-key diagnostics already exist.
   - Patch count and unsupported property/event warnings are the main remaining candidates.

6. Drive the next Core refinements from focused samples.
   - Explorer Tree for recursive keyed subtrees and lifecycle cleanup.
   - Animated Dashboard for transition coverage.
   - Stress Sample for reorder/filter/replacement diagnostics.
   - Multi-Window for root registration and window lifecycle boundaries.

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
- Keep the existing Avalonia package direction consistent; do not change renderer packages or versions without confirming the package direction.
- Do not trust perf timings from one run; use patch count plus repeated measurements.
