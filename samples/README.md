# Nuri Samples

Samples in this folder should do two jobs at the same time:

- show users what building with Nuri feels like
- pressure the Core with realistic app flows before adding new abstractions

## Direction

Build samples from large user-visible scenarios down into small Core refinements.

Do not start by adding hooks speculatively.
Add Core features only after a sample makes the need obvious and repeatable.

Duxel is the next UI backend development priority. Keep existing Avalonia samples as regression baselines, and direct new cross-renderer sample slices to WPF and Duxel first.

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
- `Nuri.AnimatedDashboardSample`: shared WPF/Avalonia/Duxel opacity transition and interruption baseline
- `Nuri.WPFAnimatedDashboardSample`: WPF Margin, background, foreground, and Rotate transition replacement coverage
- `Nuri.AvaloniaHotReloadSample`: minimal Avalonia renderer and C# Hot Reload smoke test
- `Nuri.DuxelSample`: Duxel immediate-mode hook state, Hot Reload, neutral events, nested Grid tracks, Scroll, sizing, spacing, and text-style smoke test
- `Nuri.DuxelThemeGallerySample`: runtime Duxel theme switching across the currently materialized text, button, input, selection, Grid, and Scroll controls
- `Nuri.DuxelExplorerTreeSample`: the WPF Explorer component sources projected through the Duxel host, including keyed subtree state, effect cleanup, work-area sizing, ordered wheel routing, and independent Nuri-owned tree/detail Scroll regions
- `Nuri.DuxelAnimatedDashboardSample`: the shared scrollable Animated Dashboard projected through Duxel `AnimateFloat` opacity tracks and constrained to the viewport work area
- `Nuri.MultiWindowSample`: WPF root registration, local state isolation, shared Store updates, and per-window lifecycle cleanup

## Next Sample Roadmap

Todo Notes, Settings Preferences, and Explorer Tree now cover the first three roadmap scenarios. The next samples should target gaps that are not already represented.

### 1. Animated Dashboard Sample

Goal:
Pressure the existing neutral transition model across WPF and Duxel.

What it should pressure:

- supported animated properties and easing values
- interruption and replacement of active transitions
- WPF/Duxel semantic parity
- unsupported-animation diagnostics

Shared WPF/Avalonia/Duxel opacity transitions and interruption/replacement are now regression baselines. The next slice should close Duxel background, foreground, Margin, transform, and remaining easing gaps without moving Duxel types into Core.

### 2. Stress Sample

Goal:
Combine reorder, filtering, subtree replacement, and diagnostics in one manual regression flow.

What it should pressure:

- patch count and render count visibility
- keyed identity under repeated mixed updates
- cleanup and invalidation coalescing
- unsupported property/event reporting
- Duxel frame invalidation and immediate-mode projection under repeated mixed updates

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
