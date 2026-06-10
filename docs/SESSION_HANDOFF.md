# Delta Session Handoff

This file is intentionally short. Use it only when resuming work after the local session/context is gone.

## Current Goal

Move Delta from WPF-direct UI creation toward a platform-neutral Core virtual UI/runtime/diff model, with WPF as one renderer adapter and future renderers such as Avalonia/Uno/OpenSilver/MAUI attachable later.

## Current Status

- `DeltaUI.Core` exists and owns platform-neutral virtual entries, patch operations, diffing, runtime, state store, UI element abstractions, and value models.
- `DeltaUI.WPF` references `DeltaUI.Core` and acts as the compatibility DSL plus WPF renderer/materializer.
- WPF controls are now created by `WpfControlRegistry`/`WpfVirtualEntryRenderer`, not by component render methods directly.
- Dirty component subtree render/diff/patch and Dispatcher batching are implemented.
- Keyed reconciliation is implemented with `MoveChildPatch` and WPF move support.
- A simple perf harness exists under `perf/` for basic before/after measurements.
- Release build currently passes with 0 warnings and 0 errors.

## Recent Feature Additions

- Explicit `.Key("...")` API added.
- `Name` still works as a key fallback for compatibility.
- Duplicate virtual keys are diagnosed with `Debug.WriteLine` and fall back to index diff.
- Core-neutral event foundation added:
  - `VirtualEvent`
  - `VirtualEventKind`
  - neutral `.OnClick(Action)`
  - neutral `OnTextChanged(Action<string>)`
  - neutral `OnContentChanged(Action<object>)`
  - neutral `OnCheckChanged(Action<bool>)`
- Existing WPF delegate overloads are still kept for compatibility.
- WPF adapter materializes Core `VirtualEvent` into WPF native delegates.
- Component cleanup is stronger now: removed and replaced component subtrees are disposed.
- `src/DeltaUI.Avalonia` scaffold exists without external Avalonia NuGet dependency yet.
- `samples/GridTest` demonstrates `.Key(...)` and neutral `.OnClick(Action)`.

## Important Files

- `src/DeltaUI.Core/VirtualDom/VirtualEntry.cs`
- `src/DeltaUI.Core/VirtualDom/VirtualTreeDiff.cs`
- `src/DeltaUI.Core/VirtualDom/PatchOperation.cs`
- `src/DeltaUI.Core/UI/IElement.cs`
- `src/DeltaUI.Core/UI/Element.cs`
- `src/DeltaUI.Core/UI/ComponentBase.cs`
- `src/DeltaUI.Core/UI/Events/VirtualEvent.cs`
- `src/DeltaUI.WPF/ApplicationRoot.cs`
- `src/DeltaUI.WPF/VirtualDom/Bridge/VirtualEntryAdapter.cs`
- `src/DeltaUI.WPF/VirtualDom/Rendering/WpfVirtualEntryRenderer.cs`
- `src/DeltaUI.WPF/VirtualDom/Rendering/WpfControlRegistry.cs`
- `src/DeltaUI.WPF/Controls/VisualExtentions_Event.cs`
- `src/DeltaUI.WPF/Controls/VisualExtentions_1.cs`
- `src/DeltaUI.Avalonia/AvaloniaRendererSkeleton.cs`
- `perf/DeltaUI.Performance/Program.cs`
- `perf/DeltaUI.WPFPerformance/Program.cs`

## Validation Commands

```powershell
dotnet build "Delta.sln" -c Release
dotnet run --project "perf\DeltaUI.Performance\DeltaUI.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\DeltaUI.WpfPerformance\DeltaUI.WpfPerformance.csproj" -c Release -- --label after
```

Expected baseline sanity for keyed reorder:

- Patch count should be `1`.
- Allocations and timings can vary by machine/load.

## Next Feature Priorities

1. Expand Core-neutral events.
   - Mouse enter/leave
   - Pointer/mouse down/up
   - Keyboard events
   - Focus/blur
   - Loaded/unloaded if needed

2. Rework animation as a platform-neutral feature.
   - Keep WPF `AnimationTimeline` out of Core.
   - Extend `AnimationValue` for common transitions.
   - Add renderer-specific animation materialization in WPF.
   - Later map the same Core animation description to Avalonia.

3. Improve lifecycle/effects.
   - Move more effect lifecycle behavior into Core.
   - Ensure mount/update/unmount cleanup is deterministic.

4. Flesh out Avalonia renderer.
   - Add actual Avalonia package only when package policy is decided.
   - Start with `Text`, `Button`, `StackPanel`, `Border`, `TextBox`.

5. Add diagnostics only where useful.
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

- Do not put WPF types in `DeltaUI.Core`.
- Do not remove existing WPF delegate overloads unless compatibility is intentionally broken.
- Do not replace `Name` fallback immediately; persisted/sample DSL may rely on it.
- Do not add Avalonia NuGet until renderer/package direction is decided.
- Do not trust perf timings from one run; use patch count plus repeated measurements.
