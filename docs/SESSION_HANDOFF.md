# Nuri Session Handoff

This file is intentionally short. Use it only when resuming work after the local session/context is gone.

## Current Goal

Move Nuri from WPF-direct UI creation toward a platform-neutral Core virtual UI/runtime/diff model, with WPF as one renderer adapter and future renderers such as Avalonia/Uno/OpenSilver/MAUI attachable later.

## Current Status

- The Core project at `src/Nuri` is named `Nuri` and exposes `Nuri.*` namespaces. It owns platform-neutral virtual entries, patch operations, diffing, runtime, state store, UI element abstractions, and value models.
- `Nuri.WPF` references the Core project and acts as the WPF renderer/materializer adapter.
- WPF controls are now created by `WpfControlRegistry`/`WpfVirtualEntryRenderer`, not by component render methods directly.
- Dirty component subtree render/diff/patch and Dispatcher batching are implemented.
- Core DSL state changes are batched by `ApplicationRoot`; non-root component hooks render/diff/patch only that component subtree when the current virtual entry can be found, with root rebuild as fallback.
- Keyed reconciliation is implemented with `MoveChildPatch` and WPF move support.
- A simple perf harness exists under `perf/` for basic before/after measurements.
- Release build currently passes with 0 errors. Existing Visual Studio preview threading analyzer warnings may still be reported.

## Recent Feature Additions

- Explicit `.Key("...")` API added.
- `Name` still works as a key fallback for compatibility.
- Duplicate virtual keys are diagnosed and fall back to index diff. Duplicate component keys also emit `RuntimeLogKind.DuplicateKey` and use position-based hook identity so state/effects cannot collide.
- Keyed component lifecycle uses explicit component keys. Key changes clean up the old logical component and mount the replacement.
- Runtime subtree cleanup, diagnostics cleanup, and dirty-component coalescing use the in-memory `RuntimeTreeIdentity` ancestry registry instead of parsing ID delimiters.
- The authoritative identity/key/hook invariants and regression checklist are in `docs/RUNTIME_IDENTITY.md`.
- The runtime architecture decision, non-goals, performance scenarios, and measured baseline are in `docs/RUNTIME_ARCHITECTURE.md`.
- Core-neutral event foundation added:
  - `VirtualEvent`
  - `VirtualEventKind`
  - neutral `.OnClick(Action)`
  - neutral `OnTextChanged(Action<string>)`
  - neutral `OnContentChanged(Action<object>)`
  - neutral `OnCheckChanged(Action<bool>)`
- Existing WPF delegate overloads are still kept for compatibility.
- WPF adapter materializes Core `VirtualEvent` into WPF native delegates.
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
- `samples/GridTest` demonstrates `.Key(...)`, neutral `.OnClick(Action)`, and the preferred `Grid(...).Rows(...).Columns(...)` layout style.
- Legacy `Div(Rows(...), Columns(...), children...)` overloads remain for compatibility, but new code should prefer fluent layout definitions so row/column definitions do not look like child controls.
- Core DSL now exposes WPF-familiar factory aliases such as `Button`, `TextBox`, `CheckBox`, `RadioButton`, `ToggleButton`, and `PasswordBox`. These are semantic aliases over Nuri virtual input descriptions, not WPF types in Core.
- Animation DSL now supports `.Transition(duration, easing)` immediately after a property setter, using the last changed property. Existing `.Transitions("Property", ...)` remains for explicit compatibility.

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
- `src/Nuri.Avalonia/AvaloniaRendererSkeleton.cs`
- `perf/Nuri.Performance/Program.cs`
- `perf/Nuri.WPFPerformance/Program.cs`

## Validation Commands

```powershell
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```

Expected baseline sanity for keyed reorder:

- Patch count should be `1`.
- Allocations and timings can vary by machine/load.

## Next Feature Priorities

1. Continue refining Core DSL semantics.
   - Keep the DSL C#/.NET UI-shaped, not HTML-shaped.
   - Add only renderer-neutral concepts to Core.
   - Keep framework-specific overloads out of Core and prefer renderer materialization.

2. Expand Core-neutral events.
   - Mouse enter/leave
   - Pointer/mouse down/up
   - Keyboard events
   - Focus/blur
   - Loaded/unloaded if needed

3. Rework animation as a platform-neutral feature.
   - Keep WPF `AnimationTimeline` out of Core.
   - Extend `AnimationValue` for common transitions.
   - Add renderer-specific animation materialization in WPF.
   - Later map the same Core animation description to Avalonia.

4. Improve lifecycle/effects.
   - Move more effect lifecycle behavior into Core.
   - Ensure mount/update/unmount cleanup is deterministic.

5. Continue fleshing out the Avalonia renderer while keeping materialization outside Core.

6. Add diagnostics only where useful.
   - Patch count
   - render count per component
   - duplicate key warning
   - unsupported property/event warning

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
- Do not add Avalonia NuGet until renderer/package direction is decided.
- Do not trust perf timings from one run; use patch count plus repeated measurements.
