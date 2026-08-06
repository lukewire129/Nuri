# Agent Instructions

These instructions apply to AI agents working in this repository, including OpenCode and Codex.

This file defines how to work safely in Nuri. It is not an implementation inventory, changelog, or session handoff. Use `docs/README.md` as the map to durable project documentation and inspect the current source before making claims about implementation status.

## Required Reading Before Work

- Read `docs/README.md` to select the reference documents relevant to the change.
- English reference documents under `docs/` are the implementation source of truth.
- Before changing runtime, hooks, keys, lifecycle, reconciliation, invalidation, or performance code, read:
  1. `docs/architecture/RUNTIME_ARCHITECTURE.md`
  2. `docs/architecture/RUNTIME_IDENTITY.md`
  3. `docs/architecture/LIFECYCLE.md`
- Read `docs/renderers/README.md` before changing renderer ownership, materialization, scheduling, or parity behavior.
- `docs/operations/SESSION_HANDOFF.md` is optional recovery context, not required reading or a source of truth. Verify it against current source and reference documents before relying on it.
- Korean translations for the project owner are under `docs/ko/`. Do not use a translation to override or reinterpret the English source of truth.

## Project Direction

- `Nuri` must stay platform-neutral.
- Do not add WPF, Avalonia, Duxel, Uno, OpenSilver, MAUI, or other UI framework types to `Nuri`.
- `Nuri.WPF` is the WPF renderer adapter and retains compatibility overloads where needed.
- `Nuri.Duxel` is the next UI backend development priority. Keep its immediate-mode materialization and Duxel package types outside Core.
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
- Keep Duxel materialization in `src/Nuri.Duxel`:
  - Duxel application/frame integration
  - immediate-mode virtual-entry projection
  - Duxel property, event, and animation materialization
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
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WpfPerformance.csproj" -c Release -- --label after
```

## Documentation Policy

- Do not create broad documentation unless it prevents repeated rediscovery.
- Use `docs/README.md` to keep document roles, locations, and reading paths discoverable.
- Prefer focused samples over long explanatory docs.
- Before finishing each meaningful implementation session, review every related reference document for stale or missing statements; do not update only the first document that mentions the feature.
- Durable English reference documents must have a matching Korean translation under `docs/ko/`.
- Keep the English and Korean documents in matching subdirectories and update both in the same change. Keep code symbols, paths, commands, API names, package versions, and measured numbers identical.
- `docs/operations/` contains optional operational notes and does not require a full Korean mirror. Move durable decisions into paired reference documents.
- Do not duplicate implementation inventories or long current-status lists in `AGENTS.md`; keep those facts in the appropriate reference document.

## Editing Rules

- Make small vertical slices that build.
- Do not rewrite large areas without a concrete reason.
- Do not revert user changes unless explicitly asked.
- Avoid introducing new external packages without confirming package direction first.
- Keep changes ASCII unless the edited file already uses non-ASCII or there is a clear reason.

## Change Workflow

1. Inspect the current worktree and the relevant source before assuming the handoff or reference documentation is current.
2. Read the references selected through `docs/README.md` and identify the contracts the change must preserve.
3. Make the smallest vertical change that keeps Core platform-neutral and renderer ownership explicit.
4. Run focused tests, then the required Release solution build after meaningful changes.
5. Review related English and Korean documentation for behavior, capability, command, package-version, and measurement drift.
