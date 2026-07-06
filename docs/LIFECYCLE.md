# Lifecycle Rules

Nuri lifecycle work is driven by virtual tree render, diff, patch, commit, then effect flush.

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
- `DisposeHookState` removes effect, memo, pending effect, and state entries for the component subtree.

## Hook Trimming

- Each component resets its hook index before render.
- After render, hook state beyond the number of hooks used in that render is trimmed.
- Trimmed effect hooks run their cleanup immediately.
