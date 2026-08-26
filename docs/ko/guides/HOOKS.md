# Hook Reference

Nuri hook은 `Component`의 protected method입니다. 논리 component의 모든 render에서 ordered hook은 같은 순서로 호출해야 합니다. `useState`, `useReducer`, `useRef`, `useLatest`, `useStore`, `useMemo`, `useEffect`, `useNavigation`을 조건부로 호출하거나 count가 달라질 수 있는 loop 안에서 호출하면 안 됩니다.

`useService<T>()`는 read-only provider lookup이며 hook slot을 소비하지 않습니다.

## Local State

### `useState<T>(initialValue)`

Local state를 저장하고 현재 값과 함수형 setter를 반환합니다.

```csharp
var (count, setCount) = useState(0);
setCount(current => current + 1);
setCount(_ => 0);
```

### `useReducer<TState, TAction>(reducer, initialState)`

여러 action이 같은 state를 바꿀 때 reducer를 사용합니다.

```csharp
var (state, dispatch) = useReducer<int, int>(
    (current, amount) => current + amount,
    0);
```

## Stable Value

### `useRef<T>(initialValue)`와 `useLatest<T>(value)`

`useRef`는 `Current`가 바뀌어도 component를 invalidate하지 않으며 render 사이에 mutable object를 유지합니다. `useLatest`는 같은 ref를 유지하면서 `Current`를 render의 최신 값으로 갱신합니다.

```csharp
var latestQuery = useLatest(query);
```

### `useMemo<T>(factory, dependencies)`

Dependency가 `Equals` 기준으로 바뀔 때까지 계산된 값을 cache합니다.

```csharp
var visibleItems = useMemo(
    () => items.Where(item => item.IsVisible).ToArray(),
    items);
```

비용이 큰 안정 derived value에 사용합니다. State ownership 문제를 숨기기 위해 사용하지 마세요.

## Effect와 External State

### `useEffect(effect, dependencies)`

Effect는 virtual tree가 commit된 뒤 실행됩니다. 이전 cleanup은 변경된 effect가 다시 실행되기 전과 unmount 시 실행됩니다.

```csharp
useEffect(() =>
{
    subscription.Changed += OnChanged;
    return () => subscription.Changed -= OnChanged;
}, [subscription]);
```

안정적인 논리 component에서 한 번 실행하려면 `[]`를 전달합니다. 매 commit render 뒤 실행하려면 dependency를 생략합니다. Effect는 subscription, timer, external 작업을 위한 것이며 DI lifetime mechanism이 아닙니다.

### `useStore<T>(store)`와 `useStore<TState, TResult>(store, selector)`

`Store<T>`를 읽고 선택한 값이 바뀔 때만 component를 re-render합니다.

```csharp
var displayName = useStore(sessionStore, state => state.DisplayName);
```

Store subscription은 hook이 제거되거나 component가 unmount될 때 cleanup됩니다.

### `useService<T>()`

`NuriServices.UseServiceProvider(...)`로 구성한 process-wide `IServiceProvider`에서 `T`를 resolve합니다.

```csharp
var todos = useService<ITodoService>();
```

Nuri는 service를 등록, 생성, scope 또는 dispose하지 않습니다. Host DI container가 그 책임을 소유하게 합니다. Service 내부 변경만으로 component가 re-render되지는 않으므로 `Store<T>` 또는 effect 기반 subscription과 조합해야 합니다.

## Navigation

### `useNavigation(initialRoute)`

Local route state를 소유하고 `(NavigationState, Navigator)`를 반환합니다.

```csharp
var (navigation, navigator) = useNavigation("overview");
```

Route content는 `Router(...)`로 render합니다. Page 교체 시 hook state가 새지 않도록 route content에 안정적인 key를 부여합니다.

## Component Design과 Render Scope

Nuri는 dirty component subtree를 schedule하므로 component boundary가 state change 이후 작업량에 영향을 줍니다.

- Timer, text input, selection, streaming status처럼 자주 바뀌는 state는 표시하는 가장 작은 subtree 가까이에 둡니다.
- Child가 독립 state, 다른 update frequency 또는 재사용 가능한 행동 경계를 가지면 component를 분리합니다.
- Sibling이 하나의 값을 함께 조정해야 하면 state를 parent에 둡니다. Render를 줄이려고 아래로 옮기면 ownership만 불명확해질 수 있습니다.
- Dynamic list의 stateful child에는 안정적인 `.Key(...)` 값을 사용합니다.
- `VirtualizedItems` template 안에는 component instance나 hook을 두지 않습니다. Template은 lazy하며 stateless입니다.
- Memoization이나 추가 boundary를 넣기 전에 측정합니다. 단일 timing 수치 최적화보다 keyed reconciliation과 최소 patch count 보존을 우선합니다.

```csharp
// 피할 것: clock이 관련 없는 dashboard content를 invalidate합니다.
public override IElement Render()
{
    var (seconds, setSeconds) = useState(0);
    return Div(ExpensiveSummary(), Text(seconds.ToString()));
}

// 권장: ClockComponent가 자주 변경되는 state를 소유합니다.
public override IElement Render()
{
    return Div(ExpensiveSummary(), new ClockComponent());
}
```

완전한 첫 component와 DI 설정은 [시작하기](GETTING_STARTED.md)를 참고하세요.
