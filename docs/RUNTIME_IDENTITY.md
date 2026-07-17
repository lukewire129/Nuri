# Runtime Identity and Reconciliation

This document defines the runtime contracts that must remain stable when changing components, hooks, keys, diffing, or lifecycle behavior.

The broader implementation direction and measured performance baseline are defined in [RUNTIME_ARCHITECTURE.md](RUNTIME_ARCHITECTURE.md).

## Identity Layers

Nuri has three related but distinct identities:

- Runtime ancestry records the in-memory parent relationship used by subtree cleanup, diagnostics, and dirty-component coalescing.
- `Component.Id` identifies hook state and component invalidation. It remains public for compatibility, but code must not parse it to discover ancestry.
- `VirtualEntry.Id` identifies renderer patch targets. Keyed reconciliation may retain or rewrite this ID independently of a newly created component object.

Do not merge these responsibilities. Renderer patch identity must not decide hook ownership, and lifecycle code must not infer parents from ID delimiters.

## Component and Key Rules

- A newly allocated `Component` object may represent an existing logical component.
- Logical identity is stable when the parent, component type, and explicit key are stable.
- A key change represents replacement: clean up the previous component, then mount the new component.
- A newly added key receives its own key-derived virtual-entry identity. It must not reuse the removed key's patch identity because that identity may also name the removed component's hook subtree.
- Keyed moves preserve hook state and should produce `MoveChildPatch` instead of remove/add patches where possible.
- `Name` remains a virtual-entry key fallback for compatibility. New component and list code should use `.Key("...")`.
- Keys are scoped to siblings. The same key may be reused under different parents.
- Routers keep a stable unkeyed virtual root and place route content behind a component host keyed by the route key. Replacing a route must not reuse the previous page's hook identity when different page components occupy the same position.
- Duplicate component keys never share hook identity. They emit `RuntimeLogKind.DuplicateKey` and fall back to position-based hook identity.
- Duplicate keys have no state-preservation guarantee across reorder. They should be fixed by the caller.

Component keys are copied to the rendered virtual root when that root has no explicit key. All renderer adapters, including WPF, Avalonia, and Duxel, must apply the same rule.

## Runtime Ancestry

`RuntimeTreeIdentity` records element and component parent relationships in memory.

It is the source of truth for:

- determining whether an invalidated child is covered by an invalidated parent;
- disposing hook and effect state for a subtree;
- removing diagnostic component and store-subscription records.

Do not restore checks based on `StartsWith`, `_`, `#key:`, ID length, or other string formatting. Public and diagnostic IDs may change without changing ancestry semantics.

Renderer adapters that receive a global component invalidation use `ComponentLifecycle.IsInSubtree(componentId, rootComponentId)` to test root membership through this registry. They must not filter roots by parsing or prefix-matching component IDs.

Ancestry entries must be registered when node numbers are assigned and removed when their subtree is disposed.

## Hooks and Effects

- Hook slots are ordered and must be called consistently between renders.
- `useState` uses a functional setter: `setState(current => next)`.
- Use the supplied `current` value when the update depends on previous state. Use `setState(_ => value)` for replacement.
- `useEffect(..., [])` mounts once for a stable logical component, even if a new CLR component object is allocated during a parent render.
- Replacing a type or key runs the old cleanup and mounts the new effect after commit.
- Removing a parent cleans keyed and unkeyed descendants in the subtree.
- Effect callbacks run after commit; cleanup runs before a changed effect, on hook trimming, and on unmount.
- A `useState` setter or `useReducer` dispatcher retained after unmount must become a no-op. Runtime-node object identity, not a reusable string ID, determines whether its owner is still mounted.

Hook storage is keyed by persistent in-memory runtime nodes. `Component.Id` remains the diagnostic and compatibility identifier associated with the node; it is not the hook store's ownership key.

The current component object caches its assigned runtime node. The cache is refreshed at the render boundary, so a component ID change or reuse after disposal resolves a currently registered node before hooks run.

## Change Checklist

When changing identity, hooks, lifecycle, or diffing, cover these cases:

- unkeyed state survives an ordinary rerender;
- a unique keyed component preserves state across reorder;
- changing a key produces old cleanup followed by new mount;
- two navigation hooks in one component remain independent;
- nested navigation state remains isolated;
- keyed route replacement gives the new page independent hook state;
- consecutive functional state updates use the latest state;
- stale state setters and reducer dispatchers cannot invalidate a replacement that reuses the same string ID;
- parent disposal cleans keyed descendants;
- simultaneous parent and keyed-child invalidations coalesce to the parent;
- duplicate keys receive independent hook slots and emit diagnostics;
- keyed reorder preserves patch target identity and patch count.

Run:

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
```

For reconciliation or performance changes, also run both projects under `perf/` and compare patch count as well as elapsed time.
