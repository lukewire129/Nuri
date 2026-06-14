# Agent Instructions

These instructions apply to AI agents working in this repository, including OpenCode and Codex.

## Project Direction

- `Nuri` must stay platform-neutral.
- Do not add WPF, Avalonia, Uno, OpenSilver, MAUI, or other UI framework types to `Nuri`.
- `Nuri.WPF` is a compatibility DSL plus renderer adapter.
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

## Editing Rules

- Make small vertical slices that build.
- Do not rewrite large areas without a concrete reason.
- Do not revert user changes unless explicitly asked.
- Avoid introducing new external packages without confirming package direction first.
- Keep changes ASCII unless the edited file already uses non-ASCII or there is a clear reason.

## Current Feature Priorities

1. Expand Core-neutral events: hover, pointer/mouse, keyboard, focus, loaded/unloaded if needed.
2. Improve platform-neutral animation descriptions and renderer materialization.
3. Move lifecycle/effect semantics further into Core.
4. Flesh out the Avalonia renderer after package direction is decided.
5. Add diagnostics only where useful: patch count, render count, duplicate key, unsupported property/event.
