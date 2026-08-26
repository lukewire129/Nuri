# Hook Reference

Nuri hooks are protected methods on `Component`. Call ordered hooks in the same order on every render of a logical component. Do not call `useState`, `useReducer`, `useRef`, `useLatest`, `useStore`, `useMemo`, `useEffect`, or `useNavigation` conditionally or inside a loop whose count can change.

`useService<T>()` is a read-only provider lookup and does not consume a hook slot.

## Local State

### `useState<T>(initialValue)`

Stores local state and returns the current value with a functional setter.

```csharp
var (count, setCount) = useState(0);
setCount(current => current + 1);
setCount(_ => 0);
```

### `useReducer<TState, TAction>(reducer, initialState)`

Uses a reducer when several actions transform the same state.

```csharp
var (state, dispatch) = useReducer<int, int>(
    (current, amount) => current + amount,
    0);
```

## Stable Values

### `useRef<T>(initialValue)` and `useLatest<T>(value)`

`useRef` retains a mutable object across renders without invalidating the component when `Current` changes. `useLatest` retains the same ref while updating `Current` to the render's latest value.

```csharp
var latestQuery = useLatest(query);
```

### `useMemo<T>(factory, dependencies)`

Caches a computed value until a dependency changes by `Equals`.

```csharp
var visibleItems = useMemo(
    () => items.Where(item => item.IsVisible).ToArray(),
    items);
```

Use it for expensive, stable derived values. Do not use it to hide a state ownership problem.

## Effects and External State

### `useEffect(effect, dependencies)`

Effects run after the virtual tree is committed. The previous cleanup runs before a changed effect reruns and on unmount.

```csharp
useEffect(() =>
{
    subscription.Changed += OnChanged;
    return () => subscription.Changed -= OnChanged;
}, [subscription]);
```

Pass `[]` to run once for a stable logical component. Omit dependencies to run after every committed render. Effects are for subscriptions, timers, and external work; they are not a DI lifetime mechanism.

### `useStore<T>(store)` and `useStore<TState, TResult>(store, selector)`

Reads a `Store<T>` and re-renders the component only when its selected value changes.

```csharp
var displayName = useStore(sessionStore, state => state.DisplayName);
```

Store subscriptions are cleaned up when the hook is removed or the component unmounts.

### `useService<T>()`

Resolves `T` from the process-wide `IServiceProvider` configured through `NuriServices.UseServiceProvider(...)`.

```csharp
var todos = useService<ITodoService>();
```

Nuri does not register, construct, scope, or dispose services. Let the host DI container own those responsibilities. A service changing internally does not re-render a component by itself; pair it with `Store<T>` or an effect-based subscription.

## Navigation

### `useNavigation(initialRoute)`

Owns local route state and returns `(NavigationState, Navigator)`.

```csharp
var (navigation, navigator) = useNavigation("overview");
```

Render its route content with `Router(...)`. Give route content stable keys when replacing pages so hook state cannot leak between pages.

## Component Design and Render Scope

Nuri schedules dirty component subtrees, so component boundaries affect the amount of work after a state change.

- Keep fast-changing state close to the smallest subtree that displays it: timers, text input, selection, and streaming status are common examples.
- Split a component when a child has independent state, a different update frequency, or a reusable behavioral boundary.
- Keep state in the parent when siblings must coordinate on one value; moving it down merely to reduce renders can make ownership unclear.
- Use stable `.Key(...)` values for stateful children in dynamic lists.
- Avoid component instances and hooks inside `VirtualizedItems` templates; those templates are lazy and stateless.
- Measure before adding memoization or extra boundaries. Preserve keyed reconciliation and minimal patch counts rather than optimizing a single isolated timing number.

```csharp
// Avoid: a clock invalidates unrelated dashboard content.
public override IElement Render()
{
    var (seconds, setSeconds) = useState(0);
    return Div(ExpensiveSummary(), Text(seconds.ToString()));
}

// Prefer: ClockComponent owns the frequently changing state.
public override IElement Render()
{
    return Div(ExpensiveSummary(), new ClockComponent());
}
```

See [Getting Started](GETTING_STARTED.md) for a complete first component and DI setup.
