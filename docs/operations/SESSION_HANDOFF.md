# Nuri Session Handoff

This is an optional operational recovery note, not an architecture source of truth or required starting document. Keep it short and move durable decisions into the paired English and Korean reference documents.

## Current Direction

- Keep Core platform-neutral and materialize native behavior only in renderer projects.
- Prioritize WPF/Duxel semantic parity while retaining Avalonia as a regression baseline.
- Preserve dirty-subtree rendering, keyed reconciliation, deterministic lifecycle cleanup, and patch-count invariants.
- Drive new event, animation, property, diagnostics, and host behavior from concrete samples.

## Current Areas

- Core and WPF expose rich neutral pointer events, including move, wheel, coordinates, buttons, modifiers, routing, capture, and handled propagation. Avalonia has partial support; Duxel pointer materialization remains parity work.
- WPF materializes `Absolute` and `Viewport`; `Nuri.WorkflowSample` exercises positioned children, pointer capture, pan, and zoom. Avalonia and Duxel Viewport support remain future work.
- Duxel supports independent modeless windows through `ShowModeless`; shared SimplyShare hosts exercise WPF and Duxel multi-window behavior. Automated Duxel modeless-window isolation coverage remains useful.
- The current runtime does not independently detect a CLR component-type replacement at the same parent/key or position when both components render compatible roots. See `docs/architecture/RUNTIME_ARCHITECTURE.md`.

## Resume Work

- Read `docs/README.md` for the documentation map.
- Read only the reference documents relevant to the change.
- Inspect the current worktree and source before relying on this note, because it may lag active work.

## Validation

```powershell
dotnet build "Nuri.sln" -c Release
```

Run the focused test and performance projects listed in the relevant reference document when changing runtime, renderer, formatter, lifecycle, reconciliation, or performance behavior.
