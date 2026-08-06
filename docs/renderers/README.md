# Renderer Contracts

Nuri Core owns platform-neutral virtual UI, diffing, runtime state, lifecycle, value models, and neutral event and animation descriptions. Renderer projects own framework materialization and scheduling. Native framework types must not leak into Core.

## Shared Contract

- Component `Render()` methods produce virtual UI descriptions.
- Renderers schedule invalidations, apply or project the committed virtual tree, then flush effects after commit.
- Renderer differences must not change runtime identity, keyed reconciliation, deterministic cleanup, or patch semantics.
- Compatibility overloads remain renderer-owned where existing applications require native delegates or host types.
- Parity means equivalent user-visible semantics, not identical native implementation mechanics.

## Renderer Roles

| Renderer | Role |
|---|---|
| WPF | Retained-control reference adapter. Owns WPF control creation, property/event mapping, native patching, Dispatcher integration, animation materialization, and WPF window hosting. |
| Avalonia | Existing retained-control adapter and regression baseline. Preserve its supported behavior, but do not use Avalonia types to shape Core contracts. |
| Duxel | Priority immediate-mode adapter. Projects the latest committed virtual tree each frame; its Windows project owns native window, input, frame-loop, theme, and modeless-window integration. |

The detailed current implementation, known gaps, measurements, and renderer-specific behavior are maintained in [Runtime Architecture](../architecture/RUNTIME_ARCHITECTURE.md). Add separate renderer documents only when a renderer's durable materialization contract can no longer be reviewed clearly there.
