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

## 최적화 순서

Runtime 복잡성을 늘리기 전에 측정합니다.

1. 현재 component 객체에 할당된 runtime node reference를 직접 보관해 registry 재검색을 줄입니다.
2. 할당량 측정 결과가 필요성을 보여줄 때 hook 종류별 dictionary를 순서 기반의 작은 hook slot으로 변경합니다.
3. 부모가 자식을 포함하는 규칙을 유지하면서 invalidation queue 검색과 할당을 줄입니다.
4. profiling에서 GC 압력이 확인된 뒤에만 임시 component 또는 closure 할당을 줄입니다.
5. renderer batching과 subtree rendering으로 부족하다는 측정 결과가 있을 때만 scheduling을 복잡하게 만듭니다.

작은 시간 개선을 위해 patch 수, 결정적인 cleanup, keyed state 보존 또는 플랫폼 중립성을 포기하면 안 됩니다.

## 검증 명령

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```
