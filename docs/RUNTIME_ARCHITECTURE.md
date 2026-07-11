# Runtime Architecture Direction

This document records the architectural direction and measured baseline for Nuri's component runtime. It should be reviewed before introducing scheduling, hook, reconciliation, or lifecycle changes.

## Decision

Nuri follows React-like declarative and lifecycle semantics, but it does not copy React Fiber as an implementation.

Nuri is a .NET-native lightweight retained runtime with virtual UI diffing:

```text
state update
  -> platform scheduler batching
  -> dirty component subtree render
  -> virtual tree diff
  -> minimal renderer patches
  -> effect flush after commit
```

Keep these React-like contracts:

- logical component identity based on parent, component type, and key;
- ordered hook slots and stable state across logical rerenders;
- keyed reconciliation and state preservation across moves;
- cleanup on key/type replacement and unmount;
- effects executed after commit.

Do not add React implementation machinery without a measured Nuri requirement:

- alternate/current Fiber trees;
- lanes and concurrent scheduling;
- interruptible or restartable rendering;
- browser DOM event delegation;
- Suspense-specific runtime machinery.

WPF Dispatcher and future renderer schedulers remain renderer-owned. Core owns neutral runtime identity, hooks, lifecycle, diffing, and patch descriptions.

## Current Runtime Shape

Runtime tree nodes are persistent in-memory identities. State, reducer, ref, memo, effect, and store hook data use runtime node references as ownership keys.

Each current `ComponentBase` object caches its assigned runtime node. The node is resolved once at the render boundary, and hook calls use that reference directly instead of locking and searching the global registry by `Component.Id`.

Strings such as `Component.Id` and `VirtualEntry.Id` remain for compatibility, diagnostics, virtual-tree lookup, and renderer patch targets. Tree ancestry and hook ownership must not be inferred by parsing those strings.

See [RUNTIME_IDENTITY.md](RUNTIME_IDENTITY.md) for key, lifecycle, duplicate-key, and cleanup invariants.

## Performance Baseline

Measured in Release on 2026-07-11. These values are a local baseline, not universal budgets. Compare future results on the same machine and workload; correctness counters are hard invariants.

Core runtime, 100 measured iterations after 10 warmups:

| Scenario | Size | Mean ms | Alloc KB | Required result |
|---|---:|---:|---:|---:|
| Keyed reorder diff | 1,000 | 1.5134 | 548.55 | 1 patch |
| Stable render with state hooks | 1 | 0.0006 | 0.25 | 1 hook |
| Stable render with state hooks | 10 | 0.0018 | 1.48 | 10 hooks |
| Stable render with state hooks | 50 | 0.0102 | 6.95 | 50 hooks |
| Keyed component state mount/dispose | 1,000 | 3.1037 | 1596.31 | 1,000 disposed states |
| Parent/child invalidation coalescing | 1,000 children | 0.3749 | 226.81 | 1 retained parent invalidation |
| Effect mount/unmount | 1,000 | 2.1434 | 1895.83 | 1,000 cleanups |

WPF renderer, 100 measured iterations after 10 warmups:

| Scenario | Size | Mean ms | Alloc KB | Required patch count |
|---|---:|---:|---:|---:|
| Initial native build | 1,000 | 10.0946 | 1393.99 | 0 |
| Keyed native reorder | 1,000 | 6.1023 | 1603.84 | 1 |

Invalidation coalescing uses constant-time enqueue deduplication and runtime-parent traversal. The 1,000-child result improved from 3.9679 ms / 673.73 KB to 0.3749 ms / 226.81 KB while retaining exactly one parent invalidation.

The 10,000-child stress comparison used 10 iterations before optimization and 30 confirmation iterations after optimization:

| Version | Mean ms | Alloc KB | Retained invalidations |
|---|---:|---:|---:|
| Before | 232.4499 | 6820.32 | 1 |
| After | 3.0302 | 2224.42 | 1 |

Timing improved by about 76x in the confirmation run and allocation fell by about 67%. Treat result count `1` as the correctness invariant; timing remains environment-dependent.

Runtime-node caching was measured separately with 100,000 stable render iterations. Current focused results were 0.0005 ms for 1 state hook, 0.0022 ms for 10 hooks, and 0.0046 ms for 50 hooks. Hook-render allocation stayed at 0.25 KB, 1.48 KB, and 6.95 KB because caching removes registry lookup rather than hook-value allocation. The cached reference adds about 8 bytes per transient component object; the 1,000-component mount scenario increased by about 7.8 KB. Short standard runs remain noise-sensitive, so this change is justified primarily by removing per-hook registry locking and simplifying ownership, not by claiming a fixed speedup ratio.

## Measurement Scenarios

The Core performance harness covers:

- stable component renders with 1, 10, and 50 state hooks;
- 1,000 keyed components mounting state and disposing their runtime nodes;
- a parent and 1,000 dirty children coalescing into one subtree rebuild request;
- 1,000 effects mounting and cleaning up;
- a 1,000-entry keyed reorder retaining a single patch.

The test suite separately verifies:

- keyed state and effect identity;
- key replacement cleanup and mount order;
- nested and consecutive navigation updates;
- duplicate-key isolation and diagnostics;
- runtime ancestry registry cleanup after root disposal.

## Optimization Order

Use measurements before adding runtime complexity. If hook-heavy scenarios regress materially, optimize in this order:

1. Replace per-kind hook dictionaries with compact ordered hook slots where allocation data justifies it.
2. Reduce transient component or closure allocation only after profiling shows meaningful GC pressure.
3. Consider scheduling sophistication only when renderer batching and subtree rendering are insufficient.

Do not trade patch count, deterministic cleanup, keyed state preservation, or platform neutrality for small isolated timing gains.

## Validation

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```

For performance changes, retain the TSV output in the review or handoff notes and compare before/after results from the same environment.
