# Runtime Identity와 Reconciliation

이 문서는 component, hook, key, diff 또는 lifecycle 변경 시 유지해야 하는 runtime 계약을 정의합니다. 더 넓은 설계 방향과 성능 기준은 [RUNTIME_ARCHITECTURE.md](RUNTIME_ARCHITECTURE.md)를 참고합니다.

## Identity 계층

Nuri에는 서로 관련되어 있지만 역할이 다른 세 가지 identity가 있습니다.

- Runtime ancestry: subtree cleanup, diagnostics 및 dirty component 병합에 사용하는 메모리상의 부모 관계
- `Component.Id`: hook state 및 component invalidation을 식별합니다. 호환성을 위해 공개 상태를 유지하지만 ancestry를 찾기 위해 파싱하면 안 됩니다.
- `VirtualEntry.Id`: renderer patch target을 식별합니다. Keyed reconciliation은 새로 생성된 component 객체와 독립적으로 이 ID를 유지하거나 다시 쓸 수 있습니다.

Renderer patch identity가 hook 소유권을 결정하거나 lifecycle 코드가 ID 구분자를 파싱해 부모를 추론하면 안 됩니다.

## Component 및 Key 규칙

- 새로 생성된 `Component` 객체가 기존 논리적 component를 나타낼 수 있습니다.
- 부모, component type 및 명시적 key가 안정적이면 논리적 identity도 유지됩니다.
- key가 바뀌면 이전 component를 cleanup하고 새 component를 mount합니다.
- 새로 추가된 key는 자체 key 기반 virtual-entry identity를 받습니다. 제거된 key의 patch identity는 제거된 component의 hook subtree 이름으로도 사용될 수 있으므로 재사용하면 안 됩니다.
- keyed move는 hook state를 유지하며 가능하면 remove/add 대신 `MoveChildPatch`를 생성해야 합니다.
- `Name`은 호환성을 위한 virtual-entry key fallback으로 유지합니다. 새 코드는 `.Key("...")`를 사용합니다.
- key는 sibling 범위에서만 유일하면 됩니다. 같은 key는 서로 다른 부모 아래에서 다시 사용할 수 있습니다.
- Router는 안정적인 unkeyed virtual root를 유지하고 route key로 keyed된 component host 뒤에 route content를 배치합니다. 서로 다른 page component가 같은 위치를 차지하더라도 route 교체 시 이전 page의 hook identity를 재사용하면 안 됩니다.
- 중복 component key는 hook identity를 공유하지 않습니다. `RuntimeLogKind.DuplicateKey`를 기록하고 위치 기반 hook identity로 fallback합니다.
- 중복 key reorder에서는 state 보존을 보장하지 않으며 caller가 중복 key를 수정해야 합니다.

Component key는 렌더 결과 root에 별도 key가 없을 때 virtual root로 전달됩니다. WPF, Avalonia 및 Duxel을 포함한 모든 renderer adapter는 동일한 규칙을 적용해야 합니다.

## Runtime Ancestry

`RuntimeTreeIdentity`가 element 및 component의 부모 관계를 메모리에 기록합니다.

다음 동작의 기준입니다.

- dirty child가 dirty parent에 포함되는지 판단
- subtree의 hook 및 effect state 정리
- diagnostics component와 store subscription 기록 정리

`StartsWith`, `_`, `#key:`, ID 길이 등 문자열 형식을 이용한 부모 판정을 다시 도입하면 안 됩니다. Public 및 diagnostics ID 형식은 ancestry semantics를 바꾸지 않고 변경될 수 있습니다. Ancestry는 node number 할당 시 등록하고 subtree dispose 시 제거해야 합니다.

전역 component invalidation을 받는 renderer adapter는 이 registry를 통해 root 포함 관계를 검사하는 `ComponentLifecycle.IsInSubtree(componentId, rootComponentId)`를 사용합니다. Component ID를 파싱하거나 prefix 비교로 root를 filtering하면 안 됩니다.

## Hook 및 Effect

- Hook slot은 순서 기반이며 render 사이에 일관되게 호출해야 합니다.
- `useState`는 함수형 setter인 `setState(current => next)`를 사용합니다.
- 이전 state가 필요한 변경은 전달된 `current`를 사용하고, 값 교체는 `setState(_ => value)`를 사용합니다.
- `useEffect(..., [])`는 부모 render에서 새 CLR 객체가 생성되더라도 안정적인 논리 component에서는 한 번만 mount됩니다.
- type 또는 key 교체 시 이전 cleanup 이후 새 effect가 mount됩니다.
- 부모 제거는 keyed/unkeyed 자손을 모두 정리합니다.
- effect는 commit 이후 실행되며 dependency 변경, hook trimming 및 unmount 시 cleanup됩니다.
- unmount 이후 보관된 `useState` setter 또는 `useReducer` dispatcher는 no-op이 되어야 합니다. 재사용 가능한 문자열 ID가 아니라 runtime node 객체 identity로 소유자가 아직 mount 상태인지 판단합니다.

Hook 저장소는 메모리에 유지되는 runtime node를 소유권 key로 사용합니다. `Component.Id`는 node에 연결된 diagnostics 및 호환성 식별자이며 hook store의 ownership key가 아닙니다.

현재 component 객체는 할당된 runtime node를 캐시합니다. Render 경계에서 cache를 갱신하므로 Component ID가 바뀌거나 dispose 이후 객체를 다시 사용해도 hook 실행 전에 현재 등록된 node를 resolve합니다.

## 변경 체크리스트

- 일반 rerender에서 unkeyed state 보존
- reorder에서 unique keyed state 보존
- key 변경 시 이전 cleanup 후 새 mount
- 한 component 안의 navigation hook 격리
- nested navigation state 격리
- keyed route 교체 시 새 page가 독립적인 hook state 사용
- 연속 함수형 state 변경이 최신 값 사용
- stale state setter와 reducer dispatcher가 동일한 문자열 ID를 재사용한 replacement를 invalidate하지 않음
- 부모 dispose 시 keyed 자손 cleanup
- 부모와 keyed child 동시 invalidation이 부모 하나로 병합
- 중복 key hook 격리와 diagnostics 기록
- keyed reorder patch identity 및 patch count 유지

다음을 실행합니다.

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
```

Reconciliation 또는 performance 변경에는 관련 harness를 명시적으로 실행합니다. Core는 `Nuri.Performance`, WPF는 `Nuri.WpfPerformance`, Duxel은 `Nuri.DuxelPerformance` 또는 `Nuri.DuxelWindowsPerformance`를 사용하고 elapsed time 및 allocation과 함께 patch count를 비교합니다.
