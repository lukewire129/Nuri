# Lifecycle 규칙

Nuri lifecycle은 virtual tree render, diff, patch, commit, effect flush 순서로 진행됩니다. Identity와 key의 상세 계약은 [RUNTIME_IDENTITY.md](RUNTIME_IDENTITY.md)를 참고합니다.

## Mount

- Root render는 `IElement` tree를 만들고 `VirtualEntry` tree로 변환합니다.
- 플랫폼 renderer가 native root를 materialize합니다.
- 대기 중인 `useEffect` callback은 native tree가 연결된 이후 실행됩니다.

## Update

- `useState`와 `useReducer`는 새 값이 `EqualityComparer<T>.Default` 기준으로 다를 때만 state를 갱신합니다.
- State 변경은 component를 dirty로 표시하고 플랫폼 `IUiScheduler`로 예약합니다.
- Dirty root는 전체 rebuild를 수행합니다.
- 기존 virtual subtree를 찾을 수 없거나 subtree commit이 실패하면 전체 rebuild로 fallback합니다.
- 부모와 자식이 모두 dirty이면 부모 rebuild가 자식을 포함합니다.
- Renderer가 요청한 root replacement는 기존 root identity를 유지합니다. Partial replacement는 stable component identity의 hook/effect state를 보존하고, reset replacement는 새 root를 render하기 전에 이전 root state를 dispose합니다. Duxel은 다음 frame 경계에서 replacement를 적용하고 replacement projection이 commit된 뒤에만 새 pending effect를 실행합니다.

## Effect

- dependency가 없는 `useEffect`는 해당 component의 commit된 render마다 실행됩니다.
- dependency가 있는 effect는 mount와 dependency 변경 시 실행됩니다.
- 변경된 effect가 다시 실행되기 전에 이전 cleanup을 실행합니다.
- 초기 mount, 전체 rebuild 및 성공한 subtree rebuild 이후 pending effect를 실행합니다.
- Route 전환 순서는 `useState`, `useEffect`, neutral transition을 조합한 application component가 소유합니다. `Router`는 animation 정책을 정하지 않고 keyed route content만 선택합니다.

## Unmount

- child 제거와 entry 교체는 해당 subtree의 hook state를 정리합니다.
- hook state dispose 시 effect cleanup을 실행합니다.
- WPF subtree를 제거하거나 교체하기 전에 commit된 native event handler를 재귀적으로 해제합니다. WPF root dispose에도 같은 규칙을 적용하므로 unmount된 native control 참조를 보관해도 이전 virtual callback을 호출할 수 없습니다.
- WPF application root를 dispose하면 대기 중인 component invalidation을 비웁니다. 이미 Dispatcher에 게시된 callback도 dispose 이후 render하거나 effect를 다시 mount해서는 안 됩니다.
- `NuriApplication.Run<TComponent>`는 `[STAThread]`가 없는 entry point에서도 호출할 수 있습니다. WPF application이 없고 호출자가 STA가 아니면 `Run`은 전용 foreground STA thread를 만들고 그곳에서 WPF application과 Dispatcher를 실행하며, 종료될 때까지 호출자를 대기시키고 동기 startup 실패를 호출자에게 다시 throw합니다. WPF application이 다른 thread에 이미 있으면 해당 application의 Dispatcher로 전달합니다. STA가 아닌 thread에서 이미 생성된 application은 복구할 수 없습니다. `Show<TComponent>`와 `Attach`는 thread-affine WPF 객체를 반환하므로 호출자가 여전히 소유 WPF STA thread를 사용해야 합니다.
- `NuriApplication.Run<TComponent>`는 WPF에 `ShutdownMode.OnMainWindowClose`를 설정합니다. 해당 main window를 닫으면 application이 종료되고 남아 있는 모든 window가 닫히며, 각 등록 root는 `Closed` handler를 통해 dispose됩니다. `Show<TComponent>`만 호출하는 경우에는 application의 shutdown policy를 변경하지 않습니다.
- 비동기 작업이 보관한 `useState` setter와 `useReducer` dispatcher는 소유 runtime node가 dispose된 이후 no-op이 됩니다. 나중에 동일한 문자열 ID를 재사용하는 replacement component를 invalidate해서는 안 됩니다.
- subtree membership은 ID 문자열 파싱이 아니라 메모리 runtime ancestry를 사용합니다.
- component key 변경은 이전 논리 component의 unmount와 replacement mount입니다.

## 중복 Key

- 중복 sibling component key는 `RuntimeLogKind.DuplicateKey`를 기록합니다.
- 중복 key component는 state/effect 공유를 방지하기 위해 위치 기반 hook identity를 사용합니다.
- 중복 key reorder의 state 보존은 보장하지 않습니다.

## Hook Trimming

- 각 component는 render 전에 hook index를 초기화합니다.
- render 이후 사용된 hook 수를 초과하는 hook state를 제거합니다.
- 제거되는 effect hook은 즉시 cleanup을 실행합니다.

## Effect 성능 측정

Effect update 측정을 2026-07-12에 추가했습니다. 100개 hook 시나리오는 기존 public `useEffect(..., params object?[] dependencies)` 형태를 사용하며 cleanup/result 수가 정확한지 검증합니다.

| 시나리오 | 개선 전 할당 KB | 예상 할당 KB | 개선 후 할당 KB | 개선 전/후 중앙값 ms | 필수 결과 |
|---|---:|---:|---:|---:|---:|
| 동일 dependency update | 15.38 | <=15.38 | 15.38 | 0.0100 / 0.0078 | hook 100개, 재실행 없음 |
| 변경 dependency update | 38.66 | <=30.00 | 31.63 | 0.0235 / 0.0487 | cleanup 및 재실행 100회 |

단일 dependency는 내부 dependency 배열 없이 보관하며 cleanup 후 재실행 순서를 유지합니다. 변경 dependency 할당 목표에 도달하지 못한 이유는 public `params` 호출부에서 인자 배열이 여전히 생성되기 때문입니다. 이는 측정된 부분 성공으로 판단하며 breaking overload 변경은 하지 않습니다.
