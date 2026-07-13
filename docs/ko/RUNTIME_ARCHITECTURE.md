# Runtime 아키텍처 방향

이 문서는 Nuri 컴포넌트 runtime의 설계 방향과 측정된 성능 기준을 기록합니다. scheduling, hook, reconciliation 또는 lifecycle을 변경하기 전에 확인해야 합니다.

## 결정 사항

Nuri는 React와 유사한 선언형 UI 및 lifecycle 의미론을 따르지만 React Fiber 구현을 복제하지 않습니다.

Nuri의 방향은 virtual UI diff를 사용하는 .NET 기반의 가벼운 retained runtime입니다.

```text
state 변경
  -> 플랫폼 scheduler batching
  -> dirty component subtree render
  -> virtual tree diff
  -> 최소 renderer patch
  -> commit 이후 effect 실행
```

유지할 React 계열 계약:

- 부모, 컴포넌트 type, key를 기준으로 하는 논리적 identity
- 순서가 안정적인 hook slot과 논리적 rerender 사이의 state 보존
- keyed reconciliation과 이동 중 state 보존
- key/type 교체 및 unmount 시 cleanup
- commit 이후 effect 실행

측정된 Nuri 요구가 없다면 추가하지 않을 구현:

- alternate/current Fiber 이중 트리
- lane 및 concurrent scheduling
- 중단하거나 재시작할 수 있는 rendering
- 브라우저 DOM event delegation
- Suspense 전용 runtime 구조

WPF Dispatcher와 향후 renderer scheduler는 renderer가 소유합니다. Core는 중립적인 runtime identity, hook, lifecycle, diff 및 patch 설명을 소유합니다.

## 현재 Runtime 구조

Runtime tree node는 메모리에 유지되는 identity입니다. state, reducer, ref, memo, effect, store hook 데이터는 runtime node reference를 소유권 key로 사용합니다.

현재 `ComponentBase` 객체는 할당된 runtime node를 직접 캐시합니다. Render 경계에서 node를 한 번 resolve하고, 각 hook은 `Component.Id`로 전역 registry를 반복 검색하지 않고 이 reference를 바로 사용합니다.

`Component.Id`와 `VirtualEntry.Id` 같은 문자열은 호환성, diagnostics, virtual tree 검색 및 renderer patch target을 위해 유지합니다. 문자열을 파싱해서 tree ancestry 또는 hook 소유권을 판단하면 안 됩니다.

key, lifecycle, 중복 key 및 cleanup 불변식은 [RUNTIME_IDENTITY.md](RUNTIME_IDENTITY.md)를 참고합니다.

## 성능 기준

2026-07-11 Release 빌드에서 측정했습니다. 이 값은 로컬 비교 기준이며 모든 환경에 적용되는 성능 예산은 아닙니다. 같은 장비와 workload에서 전후 결과를 비교하고, 정확성 카운터는 반드시 유지해야 합니다.

Core runtime, warmup 10회 이후 100회 측정:

| 시나리오 | 크기 | 평균 ms | 할당 KB | 필수 결과 |
|---|---:|---:|---:|---:|
| Keyed reorder diff | 1,000 | 1.5134 | 548.55 | patch 1개 |
| State hook 안정 렌더 | 1 | 0.0006 | 0.25 | hook 1개 |
| State hook 안정 렌더 | 10 | 0.0018 | 1.48 | hook 10개 |
| State hook 안정 렌더 | 50 | 0.0102 | 6.95 | hook 50개 |
| Keyed component state mount/dispose | 1,000 | 3.1037 | 1596.31 | state 1,000개 정리 |
| 부모/자식 invalidation 병합 | 자식 1,000개 | 0.3749 | 226.81 | 부모 invalidation 1개 |
| Effect mount/unmount | 1,000 | 2.1434 | 1895.83 | cleanup 1,000개 |

WPF renderer, warmup 10회 이후 100회 측정:

| 시나리오 | 크기 | 평균 ms | 할당 KB | 필수 patch 수 |
|---|---:|---:|---:|---:|
| 초기 native build | 1,000 | 10.0946 | 1393.99 | 0 |
| Keyed native reorder | 1,000 | 6.1023 | 1603.84 | 1 |

Invalidation 병합은 O(1) 중복 enqueue 검사와 runtime parent 순회를 사용합니다. 자식 1,000개 결과는 부모 invalidation 1개를 유지하면서 `3.9679 ms / 673.73 KB`에서 `0.3749 ms / 226.81 KB`로 개선됐습니다.

자식 10,000개 stress 비교는 최적화 전 10회, 최적화 후 확인 측정 30회를 사용했습니다.

| 버전 | 평균 ms | 할당 KB | 유지된 invalidation |
|---|---:|---:|---:|
| 최적화 전 | 232.4499 | 6820.32 | 1 |
| 최적화 후 | 3.0302 | 2224.42 | 1 |

확인 측정에서 시간은 약 76배 개선됐고 할당량은 약 67% 감소했습니다. 결과 수 `1`은 정확성 불변식이며 시간은 실행 환경에 따라 달라질 수 있습니다.

Runtime node 캐시는 안정 렌더 100,000회로 별도 측정했습니다. 집중 측정 결과는 state hook 1개 `0.0005 ms`, 10개 `0.0022 ms`, 50개 `0.0046 ms`였습니다. Hook 렌더 할당량은 각각 `0.25 KB`, `1.48 KB`, `6.95 KB`로 유지됐습니다. 캐시는 hook 값 할당이 아니라 registry 검색을 제거하기 때문입니다. Cached reference 때문에 임시 component 객체 하나당 약 8 bytes가 추가되며, component 1,000개 mount 시나리오에서는 약 `7.8 KB`가 증가했습니다. 짧은 표준 측정은 노이즈 영향을 받으므로 고정된 성능 향상 비율을 주장하지 않고, hook별 registry lock 제거와 소유 구조 단순화를 변경 근거로 삼습니다.

Diagnostics 비활성 상태의 hook formatting은 2026-07-12에 최적화했습니다. 개선 전과 개선 후 값은 각각 warmup 10회와 측정 100회를 수행한 독립 Release process 7개의 중앙값입니다. 이제 value와 dependency summary는 `NuriDiagnostics.IsEnabled`가 `true`일 때만 formatting하며, diagnostics 활성 상태에서는 기존 hook kind, display type, summary를 그대로 유지합니다.

| 시나리오 | 개선 전 할당 KB | 예상 할당 KB | 개선 후 할당 KB | 필수 결과 |
|---|---:|---:|---:|---:|
| State hook 안정 렌더 | 1: 0.25 | 0.18-0.22 | 0.23 | hook 1개 |
| State hook 안정 렌더 | 10: 1.48 | 0.95-1.15 | 1.24 | hook 10개 |
| State hook 안정 렌더 | 50: 6.95 | 4.40-5.40 | 5.78 | hook 50개 |

Hook 50개 할당량은 약 16.8% 감소했지만 예상 상한인 5.40 KB에는 도달하지 못했습니다. 이 결과는 비활성 summary formatting에서 hook당 약 24 bytes가 제거됐고 setter delegate, closure 및 다른 hook 비용은 남아 있음을 보여 줍니다. 더 광범위한 hook store 재작성 근거가 아니라 측정된 부분 성공으로 취급합니다.

State setter 재사용은 2026-07-12에 동일한 독립 process 7개, warmup 10회, 측정 100회 방식으로 측정했습니다. Release IL에서 기존 `useState<T>` 경로가 매 render의 각 hook마다 display class 1개와 setter delegate 1개를 생성하는 것을 확인했습니다. 이제 state slot은 logical runtime node와 hook index에 setter 1개를 유지하고, 이후 render에서 현재 CLR component owner를 갱신합니다.

| 시나리오 | 개선 전 할당 KB | 예상 할당 KB | 개선 후 할당 KB | 개선 전/후 중앙값 ms | 필수 결과 |
|---|---:|---:|---:|---:|---:|
| State hook 안정 렌더 | 1: 0.23 | 0.10-0.15 | 0.12 | 0.0009 / 0.0013 | hook 1개 |
| State hook 안정 렌더 | 10: 1.24 | 0.10-0.20 | 0.15 | 0.0018 / 0.0022 | hook 10개 |
| State hook 안정 렌더 | 50: 5.78 | 0.10-0.30 | 0.31 | 0.0063 / 0.0077 | hook 50개 |

Hook 50개 안정 렌더 할당량은 약 94.6% 감소했습니다. 사전에 정한 예상 상한을 0.01 KB 초과했고 짧은 100회 지연 시간이 증가했으므로 사전 기준에 따른 판정은 부분 성공입니다. Keyed component 1,000개 mount 할당량도 1580.69 KB에서 1557.25 KB로 감소했으며 dispose된 state 수는 정확히 1,000개를 유지했습니다. 개선 후 시간 중앙값은 6.6388 ms이고 범위는 5.9019-7.7229 ms였습니다.

짧은 실행은 GC 비용을 반영할 만큼 충분한 할당 압력을 만들지 않습니다. 별도의 지속 비교에서는 commit된 개선 전 runtime과 working runtime을 분리 worktree에서 실행했습니다. 독립 Release process 7개가 각각 warmup 10,000회 이후 state hook 50개 렌더를 100,000회 측정했습니다.

| 버전 | 총시간 중앙값 ms | Renders/sec | 렌더당 할당 KB | 총 할당 MB | Gen0 | 필수 결과 |
|---|---:|---:|---:|---:|---:|---:|
| 개선 전 | 626.04 | 159734 | 5.77 | 563.83 | 70 | hook 50개 |
| 개선 후 | 197.44 | 506491 | 0.30 | 29.76 | 3 | hook 50개 |

지속 실행 총시간은 약 68.5% 감소했고 처리량은 약 3.17배 증가했습니다. 총 할당량은 약 94.7%, Gen0 횟수는 약 95.7% 감소했습니다. 따라서 문서에 기록한 짧은 지연 tradeoff를 유지하면서도 hook이 많은 지속 workload에서는 setter 재사용을 지지하는 결과입니다.

Application 형태 WPF와 invalidation 검증을 2026-07-12에 추가했습니다. WPF 시나리오는 `Nuri.TodoValidationSample`과 같은 header/input/keyed-list virtual 구조를 사용하며, 의도적으로 sample assembly를 직접 참조하지 않는 renderer harness입니다. 값은 동일한 Release harness에서 commit된 개선 전 runtime과 working runtime을 실행한 중앙값입니다.

| 시나리오 | 개선 전 | 개선 후 | 개선 전/후 할당 KB | 필수 결과 |
|---|---:|---:|---:|---:|
| Todo 형태 initial build, item 1,000개 | 6.1149 ms | 6.2102 ms | 1398.58 / 1398.58 | patch 0개 |
| Todo 형태 keyed reorder, item 1,000개 | 6.4502 ms | 6.2962 ms | 1608.87 / 1608.87 | patch 1개 |
| Invalidation enqueue, child 1,000개 | 0.1941 ms | 0.1862 ms | 147.33 / 147.33 | pending 결과 1 |
| Parent/child coalescing, child 1,000개 | 0.3843 ms | 0.3189 ms | 226.81 / 226.81 | retained 결과 1 |

차이는 실행 환경 노이즈 범위이며 queue 또는 renderer 복잡도를 추가할 근거가 없습니다. 신규 시나리오는 회귀 및 측정 coverage로 유지하고, 이번 slice에서는 추가 runtime 최적화를 하지 않았습니다.

확장한 harness는 개선 전/예상/개선 후 판정에 사용하지 않는 향후 기준선도 제공합니다. Hook 100개 안정 렌더는 0.50 KB와 0.0178 ms였습니다. Hook 1, 10, 50, 100개 first mount 결과는 1.33/3.10/11.21/22.34 KB이고 시간 중앙값은 0.0052/0.0053/0.0136/0.0257 ms였습니다. Setter identity, latest-owner invalidation, functional update, hook slot 격리, trimming, dispose는 회귀 테스트로 검증합니다.

Stale setter 보호를 2026-07-13에 추가했습니다. `useState` setter와 `useReducer` dispatcher는 소유 runtime node reference를 유지하며, replacement가 나중에 동일한 문자열 ID를 재사용하더라도 정확히 그 node가 더 이상 등록되어 있지 않으면 no-op이 됩니다. 100회 sanity run에서 hook 1/10/50/100개의 안정 렌더 할당량은 0.12/0.15/0.31/0.50 KB를 유지했고 keyed reorder 필수 결과도 patch 1개였습니다. 추가 runtime node reference 때문에 first mount 할당량은 1.34/3.18/11.57/23.11 KB로 hook당 약 8 bytes 증가했으며 안정 렌더 할당량은 증가하지 않습니다.

## 최적화 순서

Runtime 복잡성을 늘리기 전에 측정합니다.

1. 할당량 측정 결과가 필요성을 보여줄 때 hook 종류별 dictionary를 순서 기반의 작은 hook slot으로 변경합니다.
2. profiling에서 GC 압력이 확인된 뒤에만 임시 component 또는 closure 할당을 줄입니다.
3. renderer batching과 subtree rendering으로 부족하다는 측정 결과가 있을 때만 scheduling을 복잡하게 만듭니다.

작은 시간 개선을 위해 patch 수, 결정적인 cleanup, keyed state 보존 또는 플랫폼 중립성을 포기하면 안 됩니다.

## 검증 명령

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```
