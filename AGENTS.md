# Agent Instructions

These instructions apply to AI agents working in this repository, including OpenCode and Codex.

## Required Reading Before Work

- English documents under `docs/` are the implementation source of truth for agents.
- Before changing runtime, hooks, keys, lifecycle, reconciliation, invalidation, or performance code, read:
  1. `docs/RUNTIME_ARCHITECTURE.md`
  2. `docs/RUNTIME_IDENTITY.md`
  3. `docs/LIFECYCLE.md`
- When resuming work without reliable context, read `docs/SESSION_HANDOFF.md` first, then the relevant reference documents above.
- Korean translations for the project owner are under `docs/ko/`. Do not use a translation to override or reinterpret the English source of truth.

## Project Direction

- `Nuri` must stay platform-neutral.
- Do not add WPF, Avalonia, Uno, OpenSilver, MAUI, or other UI framework types to `Nuri`.
- `Nuri.WPF` is the WPF renderer adapter and retains compatibility overloads where needed.
- Component `Render()` methods should produce virtual UI descriptions, not native WPF controls.
- Native WPF controls should be created only by the WPF renderer/registry path.
- Preserve existing user-facing DSL compatibility unless the user explicitly approves a breaking change.

## Architecture Rules

- Keep platform-neutral concepts in `src/Nuri`:
  - virtual entries
  - patch operations
  - diffing
  - runtime/state
  - element abstractions
  - value models
  - neutral event/animation descriptions
- Keep WPF materialization in `src/Nuri.WPF`:
  - WPF control factories
  - WPF property mapping
  - WPF event delegate materialization
  - WPF animation materialization
- Future renderers should attach through Core contracts instead of depending on WPF code.

## Compatibility Rules

- Keep existing overloads such as WPF delegate event handlers unless removal is explicitly requested.
- Prefer adding neutral overloads beside existing WPF-specific overloads.
- `Name` currently remains a key fallback for compatibility.
- Prefer explicit `.Key("...")` for new keyed list examples.

## Performance Rules

- Preserve dirty component subtree render/diff/patch behavior.
- Preserve Dispatcher batching for state changes.
- Preserve keyed reconciliation and `MoveChildPatch` support.
- Use the `perf/` harness for before/after comparisons when optimizing.
- Treat patch count as an important metric, not just elapsed time.

## Validation

Run this after meaningful changes:

```powershell
dotnet build "Nuri.sln" -c Release
```

For performance sanity checks:

```powershell
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```

## Documentation Policy

- Do not create broad documentation unless it prevents repeated rediscovery.
- Use `docs/SESSION_HANDOFF.md` only to recover lost session context.
- Prefer focused samples over long explanatory docs.
- When resuming without context, read `docs/SESSION_HANDOFF.md` first, then inspect only relevant source files.
- Durable English reference documents must have a matching Korean translation under `docs/ko/`.
- Update the English source and its Korean translation in the same change. Keep code symbols, paths, commands, API names, and measured numbers identical.
- `docs/SESSION_HANDOFF.md` is an operational recovery file and does not require a full Korean mirror; summarize durable decisions in the paired reference documents instead.

## Editing Rules

- Make small vertical slices that build.
- Do not rewrite large areas without a concrete reason.
- Do not revert user changes unless explicitly asked.
- Avoid introducing new external packages without confirming package direction first.
- Keep changes ASCII unless the edited file already uses non-ASCII or there is a clear reason.

## Current Feature Priorities

1. Validate and refine existing Core-neutral event semantics across WPF and Avalonia.
   - The baseline already includes click, text/content/check changes, hover, mouse down/up, keyboard down/up, focus changes, and loaded/unloaded compatibility events.
   - Prefer `useEffect(..., [])` and its cleanup for component mount/unmount behavior; do not expand loaded/unloaded as component lifecycle APIs.
   - Add richer pointer or keyboard payloads only when samples require modifiers, repeat state, handled semantics, coordinates, or device information.
2. Complete platform-neutral animation support.
   - Keep `AnimationValue` renderer-neutral.
   - Expand supported properties and Avalonia materialization based on sample needs.
   - Diagnose unsupported animation properties where actionable.
3. Strengthen renderer-level lifecycle/effect validation.
   - Verify effect flush after commit and deterministic cleanup across subtree removal, key replacement, and repeated moves in both renderers.
4. Improve WPF/Avalonia renderer parity.
   - Close property, event, animation, hot reload, and control-behavior gaps without moving framework types into Core.
5. Add diagnostics only where useful.
   - Render count and duplicate-key diagnostics already exist.
   - Prioritize patch count and unsupported property/event warnings when they help real samples.
6. Use the next samples to expose concrete gaps.
   - Prioritize Explorer Tree, Animated Dashboard, Stress, and Multi-Window scenarios.
