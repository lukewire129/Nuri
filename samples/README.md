# Nuri Samples

Samples in this folder should do two jobs at the same time:

- show users what building with Nuri feels like
- pressure the Core with realistic app flows before adding new abstractions

## Direction

Build samples from large user-visible scenarios down into small Core refinements.

Do not start by adding hooks speculatively.
Add Core features only after a sample makes the need obvious and repeatable.

## Current Samples

- `RouterSample`: component composition and nested router sample
- `GridTest`: layout and click basics
- `BorderTest`: border and styling basics
- `ContentControlChildrenTest`: content/child handling
- `DiffingEngineTest`: diffing behavior checks
- `Nuri.CommandPaletteSample`: keyboard-driven interaction, controlled input, keyed list rendering
- `Nuri.TodoNotesSample`: productivity flow with add/edit/delete/filter, keyed reorder, and inline editing
- `Nuri.SettingsPreferencesSample`: settings inputs, validation, disabled state, save/reset behavior
- `Nuri.DataEntrySample`: form-oriented data entry
- `Nuri.MasterDetailSample`: selection and detail-pane updates
- `Nuri.ModalDialogSample`: overlay composition and effect cleanup
- `Nuri.TabsNavigationSample`: keyed navigation and lifecycle behavior
- `Nuri.StoreSample`: shared store subscriptions and updates
- `Nuri.AsyncLoadingSample`: effect-driven asynchronous loading
- `Nuri.LargeListSample`: larger keyed-list rendering
- `Nuri.DevToolsSample`: runtime diagnostics and developer tooling
- `Nuri.ExplorerTreeSample`: recursive keyed folders/files with expand, selection, rename, add/delete, and lifecycle cleanup
- `Nuri.AnimatedDashboardSample`: shared WPF/Avalonia opacity transition and interruption baseline
- `Nuri.AvaloniaHotReloadSample`: minimal Avalonia renderer and C# Hot Reload smoke test

## Next Sample Roadmap

Todo Notes, Settings Preferences, and Explorer Tree now cover the first three roadmap scenarios. The next samples should target gaps that are not already represented.

### 1. Animated Dashboard Sample

Goal:
Pressure the existing neutral transition model across WPF and Avalonia.

What it should pressure:

- supported animated properties and easing values
- interruption and replacement of active transitions
- renderer parity
- unsupported-animation diagnostics

The first slice covers shared WPF/Avalonia opacity transitions. Margin, color, rotation, and diagnostics remain follow-up slices driven by this sample.

### 2. Stress Sample

Goal:
Combine reorder, filtering, subtree replacement, and diagnostics in one manual regression flow.

What it should pressure:

- patch count and render count visibility
- keyed identity under repeated mixed updates
- cleanup and invalidation coalescing
- unsupported property/event reporting

## Follow-Up Sample

- `MultiWindowSample`: pressure root registration, shared vs isolated state, and window lifecycle boundaries

## Promotion Rule For Core Features

Promote a sample pain point into Core only when at least one of these is true:

- the same workaround appears in two or more samples
- the workaround leaks renderer-specific behavior into sample code
- the workaround makes normal app code hard to read
- the workaround hides correctness or lifecycle bugs

## How To Use This Roadmap

For each new sample:

1. build the user-facing flow first
2. keep notes on friction in state, events, lifecycle, refs, animation, and diagnostics
3. extract only the smallest Core improvement that removes repeated friction
4. add tests after the Core rule becomes clear
