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
- `Nuri.AvaloniaHotReloadSample`: minimal Avalonia renderer and C# Hot Reload smoke test

## Next Sample Roadmap

### 1. Todo Notes Sample

Goal:
Show an everyday productivity flow with add, edit, complete, delete, filter, and inline state updates.

What it should pressure:

- controlled input ergonomics
- keyed list updates and reorder safety
- local state shape and update patterns
- inline editing and focus retention
- small reusable component composition

Core questions it should answer:

- Is the current state API too verbose for common app flows?
- Do we need better focus or element reference hooks?
- Does inline edit expose caret or selection gaps?
- Are keyed list patterns natural enough in the DSL?

### 2. Settings Form Sample

Goal:
Show a settings window with text inputs, checkboxes, radio buttons, toggles, validation, disabled states, and save/reset actions.

What it should pressure:

- multiple input types bound to one state model
- validation rendering and error message flow
- focus movement between fields
- conditional enable/disable UI
- semantic control aliases in the DSL

Core questions it should answer:

- Are input events complete enough for real forms?
- Do focus and blur need first-class neutral events?
- Is form state composition awkward without new helpers?
- Do we need better diagnostics for unsupported input behavior?

### 3. Explorer Tree Sample

Goal:
Show nested folders/items with expand/collapse, selection, rename, and active detail pane updates.

What it should pressure:

- recursive component rendering
- keyed nested subtree updates
- subtree-only rerender behavior
- selection state propagation
- mount/unmount cleanup around expanding and collapsing branches

Core questions it should answer:

- Do lifecycle hooks need clearer mount/update/unmount semantics?
- Are nested keyed subtrees stable under repeated edits?
- Do we need better diagnostics for render count or patch count?
- Does the current component model make recursive UI pleasant to write?

## Follow-Up Samples

- `AnimatedDashboardSample`: pressure transition semantics, supported animation surface, and unsupported animation diagnostics
- `StressSample`: combine reorder, filtering, subtree replacement, and diagnostics in one manual regression sample
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
