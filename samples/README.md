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
- `Nuri.TodoValidationSample`: controlled form input, validation, filtering, editing, removal, and keyed rows
- `Nuri.TodoNotesSample`: productivity flow with add/edit/delete/filter, keyed reorder, and inline editing
- `Nuri.SettingsPreferencesSample`: settings inputs, validation, disabled state, save/reset behavior
- `Nuri.DataEntrySample`: form-oriented data entry
- `Nuri.MasterDetailSample`: selection and detail-pane updates
- `Nuri.ModalDialogSample`: overlay composition and effect cleanup
- `Nuri.TabsNavigationSample`: keyed navigation and lifecycle behavior
- `Nuri.StoreSample`: shared store subscriptions and updates
- `Nuri.AsyncLoadingSample`: effect-driven asynchronous loading
- `Nuri.LargeListSample`: larger keyed-list rendering
- `Nuri.VirtualExplorerTreeSample`: WPF fixed-extent virtualization over 10,101 flattened tree rows
- `Nuri.RouterTransitionSample`: application-owned route transition sequencing with neutral opacity transitions
- `Nuri.WPFEditorStressComparison`: side-by-side WPF executables sharing one 1,000-line eager keyed editor component; one references NuGet Nuri.WPF 0.2.0 and the other references the current source project
- `Nuri.WPFDiagnosticsSample`: WPF runtime diagnostics, component highlighting, hooks, stores, patches, and console capture through `Nuri.WPF.Diagnostics`
- `Nuri.ExplorerTreeSample`: recursive keyed folders/files with expand, selection, rename, add/delete, and lifecycle cleanup
- `Nuri.AnimatedDashboardSample`: shared WPF/Avalonia/Duxel opacity transition and interruption baseline
- `Nuri.WPFAnimatedDashboardSample`: WPF Margin, background, foreground, Rotate, Translate, and Scale transition replacement coverage
- `Nuri.AvaloniaHotReloadSample`: minimal Avalonia renderer and C# Hot Reload smoke test
- `Nuri.DuxelSample`: Duxel immediate-mode hook state, Hot Reload, neutral events, nested Grid tracks, Scroll, sizing, spacing, and text-style smoke test
- `Nuri.DuxelDiagnosticsSample`: Duxel runtime diagnostics, hooks, keyed lifecycle, stores, patches, and console capture through `Nuri.Duxel.Diagnostics`
- `Nuri.DuxelThemeGallerySample`: runtime Duxel theme switching across the currently materialized text, button, input, selection, Grid, and Scroll controls
- `Nuri.DuxelExplorerTreeSample`: the WPF Explorer component sources projected through the Duxel host, including keyed subtree state, effect cleanup, work-area sizing, ordered wheel routing, and independent Nuri-owned tree/detail Scroll regions
- `Nuri.DuxelVirtualExplorerTreeSample`: Duxel fixed-extent projection of 10,101 virtualized tree rows
- `Nuri.DuxelNavigationSample`: `Navigate`, `Replace`, `GoBack`, route-local state, and keyed route replacement
- `Nuri.DuxelRouterSample`: compact immediate route-replacement baseline
- `Nuri.DuxelRouterTransitionSample`: neutral route-transition composition projected through Duxel
- `Nuri.DuxelEditorStressSample`: a 100,000-line editor-shaped Duxel workload with virtualized projection, single-line edits, adjacent keyed swaps, filtering, patch counts, and projected-row diagnostics
- `Nuri.DuxelAnimatedDashboardSample`: the shared scrollable Animated Dashboard projected through Duxel `AnimateFloat` opacity tracks and constrained to the viewport work area
- `Nuri.MultiWindowSample`: WPF root registration, local state isolation, shared Store updates, and per-window lifecycle cleanup

## Remaining Sample-Driven Gaps

Explorer Tree, Virtual Explorer Tree, Animated Dashboard, and Editor Stress are regression samples rather than future roadmap items. Use them to expose the next concrete gaps instead of recreating those scenarios.

Current priorities:

- materialize Duxel neutral events beyond click, text change, and check change
- close demonstrated Duxel gaps in `FontFamily`, `FontWeight`, `Grid.RowSpan`, and transform properties
- extend Duxel animation parity beyond opacity to color, margin, and transforms
- add actionable Duxel `UnsupportedEvent` diagnostics; unsupported animations currently use deduplicated `UnsupportedProperty` entries
- add Duxel multi-window and root-lifecycle coverage when the host supports it
- continue property, layout, theme, navigation, diagnostics, and lifecycle parity through existing samples

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
