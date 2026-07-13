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

WPF and Avalonia application roots share `RenderCoordinator` for native build, patch, commit, and post-commit effect flushing. Both roots also use `ComponentInvalidationQueue`, so dirty-subtree coverage follows `RuntimeTreeIdentity` instead of renderer-specific string ancestry checks.

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

Disabled-diagnostics hook formatting was optimized on 2026-07-12. The before and after values are medians from 7 independent Release processes, each using 10 warmups and 100 measured iterations. Value and dependency summaries are now formatted only when `NuriDiagnostics.IsEnabled` is `true`; enabled diagnostics retain the same hook kind, display type, and summary.

| Scenario | Before alloc KB | Expected alloc KB | After alloc KB | Required result |
|---|---:|---:|---:|---:|
| Stable render with state hooks | 1: 0.25 | 0.18-0.22 | 0.23 | 1 hook |
| Stable render with state hooks | 10: 1.48 | 0.95-1.15 | 1.24 | 10 hooks |
| Stable render with state hooks | 50: 6.95 | 4.40-5.40 | 5.78 | 50 hooks |

The 50-hook allocation fell by about 16.8%, but did not reach the expected upper bound of 5.40 KB. The result indicates that the change removed about 24 bytes per hook from disabled summary formatting while setter delegates, closures, and other hook costs remain. Treat this as a measured partial success rather than evidence for a broader hook-store rewrite.

State setter reuse was measured on 2026-07-12 with the same 7-process, 10-warmup, 100-iteration method. Release IL confirmed that the previous `useState<T>` path created one display class and one setter delegate per hook on every render. State slots now retain one setter for the logical runtime node and hook index, and update its current CLR component owner on subsequent renders.

| Scenario | Before alloc KB | Expected alloc KB | After alloc KB | Before/after median ms | Required result |
|---|---:|---:|---:|---:|---:|
| Stable render with state hooks | 1: 0.23 | 0.10-0.15 | 0.12 | 0.0009 / 0.0013 | 1 hook |
| Stable render with state hooks | 10: 1.24 | 0.10-0.20 | 0.15 | 0.0018 / 0.0022 | 10 hooks |
| Stable render with state hooks | 50: 5.78 | 0.10-0.30 | 0.31 | 0.0063 / 0.0077 | 50 hooks |

The 50-hook stable allocation fell by about 94.6%. It missed the expected upper bound by 0.01 KB, and short 100-iteration latency increased, so this remains a partial success under the predeclared thresholds. The 1,000-component keyed mount allocation also fell from 1580.69 KB to 1557.25 KB and retained exactly 1,000 disposed states. Its after-time median was 6.6388 ms with a 5.9019-7.7229 ms range.

The short run does not include enough allocation pressure to represent GC cost. A separate sustained comparison used the committed pre-change runtime and the working runtime in isolated worktrees. Each of 7 independent Release processes performed 10,000 warmups and 100,000 measured renders with 50 state hooks:

| Version | Median total ms | Renders/sec | Alloc/render KB | Total alloc MB | Gen0 | Required result |
|---|---:|---:|---:|---:|---:|---:|
| Before | 626.04 | 159734 | 5.77 | 563.83 | 70 | 50 hooks |
| After | 197.44 | 506491 | 0.30 | 29.76 | 3 | 50 hooks |

Sustained elapsed time fell by about 68.5%, throughput increased by about 3.17x, total allocation fell by about 94.7%, and Gen0 collections fell by about 95.7%. This supports the setter reuse for sustained hook-heavy workloads while retaining the documented short-latency tradeoff.

Application-shaped WPF and invalidation validation was added on 2026-07-12. The WPF scenario uses the same header/input/keyed-list virtual structure as `Nuri.TodoValidationSample`; it is intentionally a renderer harness, not a direct sample assembly reference. Values are medians from the committed before runtime and the working runtime under the same Release harness.

| Scenario | Before | After | Alloc before/after KB | Required result |
|---|---:|---:|---:|---:|
| Todo-shaped initial build, 1,000 items | 6.1149 ms | 6.2102 ms | 1398.58 / 1398.58 | 0 patches |
| Todo-shaped keyed reorder, 1,000 items | 6.4502 ms | 6.2962 ms | 1608.87 / 1608.87 | 1 patch |
| Invalidation enqueue, 1,000 children | 0.1941 ms | 0.1862 ms | 147.33 / 147.33 | pending result 1 |
| Parent/child coalescing, 1,000 children | 0.3843 ms | 0.3189 ms | 226.81 / 226.81 | retained result 1 |

The differences are within environment noise and do not justify additional queue or renderer complexity. The new scenarios remain as regression and measurement coverage; no further runtime optimization was made in this slice.

The expanded harness also establishes future baselines not used in the before/expected/after verdict: stable 100-hook render was 0.50 KB and 0.0178 ms; first mount results for 1, 10, 50, and 100 hooks were 1.33/3.10/11.21/22.34 KB with median times of 0.0052/0.0053/0.0136/0.0257 ms. Setter identity, latest-owner invalidation, functional updates, hook-slot isolation, trimming, and disposal are covered by regression tests.

Stale setter protection was added on 2026-07-13. `useState` setters and `useReducer` dispatchers retain their owning runtime-node reference and become no-ops when that exact node is no longer registered, including when a replacement later reuses the same string ID. A 100-iteration sanity run retained 0.12/0.15/0.31/0.50 KB stable-render allocation for 1/10/50/100 hooks and the required keyed reorder result of 1 patch. The extra runtime-node reference increased first-mount allocation to 1.34/3.18/11.57/23.11 KB, approximately 8 bytes per state hook; it does not add stable-render allocation.

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
dotnet run --project "tests\Nuri.RendererTests\Nuri.RendererTests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```

For performance changes, retain the TSV output in the review or handoff notes and compare before/after results from the same environment.
