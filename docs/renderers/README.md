# Renderer Contracts

Nuri Core owns platform-neutral virtual UI, diffing, runtime state, lifecycle, value models, and neutral event and animation descriptions. Renderer projects own framework materialization and scheduling. Native framework types must not leak into Core.

## Shared Contract

- Component `Render()` methods produce virtual UI descriptions.
- Renderers schedule invalidations, apply or project the committed virtual tree, then flush effects after commit.
- Renderer differences must not change runtime identity, keyed reconciliation, deterministic cleanup, or patch semantics.
- Compatibility overloads remain renderer-owned where existing applications require native delegates or host types.
- Parity means equivalent user-visible semantics, not identical native implementation mechanics.

## Native Islands

`Native<TNative>(mount: ..., render: ...)` is a renderer-owned escape hatch for adopting an existing native control without making Core reference that framework. Core carries only the native CLR type, factory, mount cleanup, and render callback in a neutral descriptor. WPF accepts `FrameworkElement` types and Avalonia accepts `Control` types; another renderer must reject an incompatible type explicitly.

`mount` runs once for a retained native instance and may return an unmount cleanup. `render` runs after initial mount and after a retained Nuri render commits, allowing Nuri state to project into the native control without `INotifyPropertyChanged`. Native islands are leaves: Nuri does not reconcile their internal native tree, map their internal events, or make them portable across renderers. Use a stable `.Key(...)` when the native control can move among siblings.

Nuri's WPF preview host is used by both the Visual Studio and VS Code preview extensions for WPF projects, so `Native<FrameworkElement>` works in either editor preview without an editor-specific integration. Duxel remains immediate-mode and does not materialize native islands.

## Renderer Roles

| Renderer | Role |
|---|---|
| WPF | Retained-control reference adapter. Owns WPF control creation, property/event mapping, native patching, Dispatcher integration, animation materialization, and WPF window hosting. |
| Avalonia | Existing retained-control adapter and regression baseline. Preserve its supported behavior, but do not use Avalonia types to shape Core contracts. `Nuri.Avalonia.WindowExtensions` provides fluent helpers for transparency, client-area title bars, chrome hints, decorations, resizing, taskbar visibility, window state, and size limits. |
| Duxel | Priority immediate-mode adapter. Projects the latest committed virtual tree each frame; its Windows project owns native window, input, frame-loop, theme, and modeless-window integration. |

The detailed current implementation, known gaps, measurements, and renderer-specific behavior are maintained in [Runtime Architecture](../architecture/RUNTIME_ARCHITECTURE.md). Add separate renderer documents only when a renderer's durable materialization contract can no longer be reviewed clearly there.
