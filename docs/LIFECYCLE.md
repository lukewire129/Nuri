# Lifecycle Rules

Nuri lifecycle work is driven by virtual tree render, diff, patch, commit, then effect flush.

Runtime identity, key, and required regression-test contracts are defined in [RUNTIME_IDENTITY.md](RUNTIME_IDENTITY.md). Lifecycle changes must preserve those contracts.

## Mount

- A root render builds an `IElement` tree and converts it to a `VirtualEntry` tree.
- The platform renderer materializes the native root from the virtual tree.
- Pending `useEffect` callbacks run only after the native tree is attached.

## Update

- `useState` and `useReducer` update stored hook state only when the new value differs by `EqualityComparer<T>.Default`.
- State updates mark the component dirty and are scheduled through the platform `IUiScheduler`.
- If the dirty component is the root, a full rebuild is performed.
- If a dirty component's old virtual subtree cannot be found, the runtime falls back to a full rebuild.
- If replacing the committed subtree fails after patching, the runtime falls back to a full rebuild.
- If both a parent and child are dirty, the parent rebuild covers the child.

## Effects

- `useEffect` without dependencies runs after every committed render of that component.
- `useEffect` with dependencies runs after mount and whenever any dependency changes by `Equals`.
- Before a changed effect runs again, the previous cleanup runs.
- `FlushPendingEffects` is called after initial mount, full rebuilds, and successful subtree rebuilds.

## Unmount

- Removing a child subtree disposes hook state for that subtree.
- Replacing an entry disposes hook state for the replaced entry subtree.
- Effect cleanups are invoked when hook state is disposed.
- Before a WPF subtree is removed or replaced, its committed native event handlers are detached recursively. WPF root disposal applies the same rule, so a retained reference to an unmounted native control cannot invoke the previous virtual callback.
- Disposing a WPF application root clears queued component invalidations. Dispatcher callbacks that were already posted must not render or remount effects after disposal.
- `NuriApplication.Run<TComponent>` may be called from an entry point without `[STAThread]`. If no WPF application exists and the caller is not STA, `Run` creates a dedicated foreground STA thread, runs the WPF application and Dispatcher there, blocks the caller until shutdown, and rethrows a synchronous startup failure on the caller. If a WPF application already exists on another thread, `Run` dispatches to that application's Dispatcher. An existing application created on a non-STA thread cannot be repaired. `Show<TComponent>` and `Attach` still require the caller to use the owning WPF STA thread because they return thread-affine WPF objects.
- `NuriApplication.Run<TComponent>` configures WPF with `ShutdownMode.OnMainWindowClose`. Closing that main window shuts down the application, closes every remaining window, and disposes each registered root through its `Closed` handler. Calling `Show<TComponent>` by itself does not change an application's shutdown policy.
- `useState` setters and `useReducer` dispatchers retained by asynchronous work become no-ops after their owning runtime node is disposed. They must not invalidate a replacement component that later reuses the same string ID.
- `DisposeHookState` removes effect, memo, pending effect, and state entries for the component subtree.
- Subtree membership comes from the in-memory runtime ancestry registry, not from parsing component ID strings.
- Changing a component key is an unmount of the previous logical component followed by a mount of the replacement.

## Duplicate Keys

- Duplicate sibling component keys emit `RuntimeLogKind.DuplicateKey`.
- Duplicate keyed components use position-based hook identity so they cannot share state or effects.
- State preservation during duplicate-key reorder is intentionally not guaranteed.

## Hook Trimming

- Each component resets its hook index before render.
- After render, hook state beyond the number of hooks used in that render is trimmed.
- Trimmed effect hooks run their cleanup immediately.

## Effect Performance Measurement

Effect update measurements were added on 2026-07-12. The 100-hook scenarios use the existing public `useEffect(..., params object?[] dependencies)` shape and verify that cleanup/result counts remain correct.

| Scenario | Before alloc KB | Expected alloc KB | After alloc KB | Before/after median ms | Required result |
|---|---:|---:|---:|---:|---:|
| Same dependency update | 15.38 | <=15.38 | 15.38 | 0.0100 / 0.0078 | 100 hooks, no rerun |
| Changed dependency update | 38.66 | <=30.00 | 31.63 | 0.0235 / 0.0487 | 100 cleanups and reruns |

Single dependencies are retained without an internal dependency array, preserving cleanup-before-rerun behavior. The changed-dependency allocation target was not reached because the public `params` call still creates its argument array; this is a measured partial success and does not justify a breaking overload change.
