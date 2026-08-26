---
name: nuri
description: Build and maintain Nuri applications using platform-neutral components, ordered hooks, keyed reconciliation, and renderer-owned materialization.
---

# Nuri Application Skill

Read [Getting Started](../../docs/guides/GETTING_STARTED.md) before creating a new Nuri application and [Hook Reference](../../docs/guides/HOOKS.md) before changing component state, effects, stores, navigation, or services.

## Non-Negotiable Rules

- `Render()` returns platform-neutral `IElement` descriptions. Do not create WPF, Avalonia, or Duxel controls in Core components.
- Call ordered hooks consistently on every render. Do not put state, reducer, ref, latest, store, memo, effect, or navigation hooks behind conditional control flow.
- Use `.Key(...)` for stateful dynamic-list and route children.
- Put fast-changing state in the smallest component that displays it. Preserve parent ownership when siblings coordinate on the same state.
- Treat `useService<T>()` as an `IServiceProvider` lookup. Nuri does not own service registration, lifetime, or disposal; use `Store<T>` or `useEffect` for observable service state.
- Keep renderer-specific APIs in renderer projects. Core must remain platform-neutral.

## Required References

- Runtime changes: [Runtime Architecture](../../docs/architecture/RUNTIME_ARCHITECTURE.md), [Runtime Identity](../../docs/architecture/RUNTIME_IDENTITY.md), and [Lifecycle](../../docs/architecture/LIFECYCLE.md).
- Renderer changes: [Renderer Contracts](../../docs/renderers/README.md).
- Full repository instructions: [AGENTS.md](../../AGENTS.md).
