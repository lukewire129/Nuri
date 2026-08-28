---
name: nuri
description: Build and maintain Nuri applications using platform-neutral components, ordered hooks, keyed reconciliation, and renderer-owned materialization.
---

# Nuri Application Skill

Read [Getting Started](../../docs/guides/GETTING_STARTED.md) before creating a new Nuri application and [Hook Reference](../../docs/guides/HOOKS.md) before changing component state, effects, stores, navigation, or services.

## Non-Negotiable Rules

- `Render()` returns platform-neutral `IElement` descriptions. Do not create WPF, Avalonia, or Duxel controls in Core components.
- Call ordered hooks consistently on every render. Do not put state, reducer, ref, latest, store, memo, effect, or navigation hooks behind conditional control flow.
- Use `.Key(...)` for stateful dynamic-list and route children.
- Put fast-changing state in the smallest component that displays it. Preserve parent ownership when siblings coordinate on the same state.
- Treat `useService<T>()` as an `IServiceProvider` lookup. Nuri does not own service registration, lifetime, or disposal; use `Store<T>` or `useEffect` for observable service state.
- Keep renderer-specific APIs in renderer projects. Core must remain platform-neutral.

## Component Structure Template

Every component follows this layout. Keep the order exactly as shown.

```csharp
using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Sample.Components;

public sealed class MyComponent : Component
{
    private static readonly Item[] InitialItems = { ... };

    public override IElement Render()
    {
        // 1. Hooks (always in the same order)
        var (state, setState) = useState(new MyState(...));
        var stateRef = useLatest(state);
        var derived = useMemo(() => Compute(state), state);

        // 2. Blank line

        // 3. Local functions (state mutators)
        void Update(Func<MyState, MyState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        void DoSomething() { ... }

        // 4. Blank line

        // 5. Return the UI tree
        return
            Div(
                Text("Title")
                    .FontSize(22)
                    .FontWeight(FontWeightValue.Bold),
                Button("Action", DoSomething)
            )
            .Padding(24)
            .Background("#0f172a");
    }

    // 6. Static helper methods for UI decomposition
    private static IElement SubView(Item item) { ... }

    // 7. Static pure functions
    private static string[] Validate(MyState state) { ... }
}

// 8. Record types at the bottom of the file
internal sealed record MyState(string Draft, Item[] Items);
internal sealed record Item(string Id, string Text);
```

## Formatting Quick Reference

Full rules: [FORMATTING.md](../../docs/guides/FORMATTING.md)

### Good

```csharp
public override IElement Render()
{
    var (count, setCount) = useState(0);

    return
        Div(
            Text("Counter")
                .FontSize(22)
                .FontWeight(FontWeightValue.Bold),
            Grid(
                Button("-", () => setCount(c => c - 1)).Column(0),
                Text(count.ToString()).Column(1),
                Button("+", () => setCount(c => c + 1)).Column(2)
            )
            .Columns(Pixels(60), Star, Pixels(60))
        )
        .Padding(24)
        .Background("#0f172a");
}
```

### Bad

```csharp
public override IElement Render()
{
    var (count, setCount) = useState(0);
    return Div(Text("Counter").FontSize(22).FontWeight(FontWeightValue.Bold), Grid(Button("-", () => setCount(c => c - 1)).Column(0), Text(count.ToString()).Column(1), Button("+", () => setCount(c => c + 1)).Column(2)).Columns(Pixels(60), Star, Pixels(60))).Padding(24).Background("#0f172a");
}
```

### Key rules

- `return` on its own line; indent the expression by 4 spaces.
- Container children (`Div`, `Grid`, `Column`, `Row`, `Stack`, `Panel`, `Scroll`) one per line.
- Container fluent calls at the closing-paren indentation.
- Control fluent calls indented 4 spaces under their receiver.
- One blank line between hooks, local functions, and the return expression.

## State Management Patterns

### Local state: `useState`

For simple component-local values.

```csharp
var (count, setCount) = useState(0);
var (name, setName) = useState("world");
```

### Stale-closure-safe updates: `useLatest` + `Update`

When multiple state fields change together or when callbacks capture state, use `useLatest` with a local `Update` function.

```csharp
var (state, setState) = useState(new MyState(...));
var stateRef = useLatest(state);

void Update(Func<MyState, MyState> change)
{
    var next = change(stateRef.Current);
    stateRef.Current = next;
    setState(_ => next);
}

void AddItem()
{
    Update(current => current with
    {
        Items = current.Items.Append(newItem).ToArray()
    });
}
```

### Shared state: `Store<T>` + `useStore`

For state shared across components.

```csharp
internal static class UserStore
{
    public static readonly Store<UserState> State = Store.Create(new UserState("Guest"));
}

public override IElement Render()
{
    var user = useStore(UserStore.State, s => s);
    return Text(user.Name);
}
```

### Derived data: `useMemo`

For expensive computations that depend on state.

```csharp
var visibleItems = useMemo(
    () => state.Items.Where(i => i.Matches(state.Filter)).ToArray(),
    state.Items, state.Filter);
```

### Transient mutable state: `useRef`

For drag, pan, or other interaction state that should not trigger re-render.

```csharp
var dragRef = useRef<DragState?>(null);
```

### Side effects: `useEffect`

For async loading, subscriptions, timers. Always return a cleanup function when needed.

```csharp
useEffect(() =>
{
    var cts = new CancellationTokenSource();
    _ = LoadAsync(cts.Token);
    return () => cts.Cancel();
}, Array.Empty<object>());
```

## Component Decomposition

- Keep `Render()` under ~150 lines. Decompose into `private static IElement` helpers when it grows.
- Keep helper method parameters under 7. Use a record parameter object when more are needed.
- Extract repeated UI patterns into reusable static methods.

```csharp
public override IElement Render()
{
    var (state, setState) = useState(...);

    return
        Div(
            Header(state.ActiveCount),
            Composer(state.Draft, AddItem),
            NotesList(state.Items)
        )
        .Padding(32);
}

private static IElement Header(int activeCount)
{
    return Text($"{activeCount} active")
        .FontSize(20)
        .FontWeight(FontWeightValue.Bold);
}
```

## Visual Design Conventions

Use these color palettes for consistency across samples.

### Dark theme

| Role | Hex | Usage |
|------|-----|-------|
| Deep background | `#0B1120` / `#0F172A` | Root background |
| Surface | `#111827` | Card, panel |
| Surface alt | `#1E293B` | Secondary panel, sidebar |
| Border | `#334155` | Panel borders |
| Border subtle | `#475569` | Dividers |
| Primary text | `#F8FAFC` | Headings |
| Secondary text | `#94A3B8` | Muted text |
| Tertiary text | `#CBD5E1` | Labels |
| Muted text | `#64748B` | Hints, disabled |
| Accent blue | `#2563EB` | Active buttons |
| Accent blue dark | `#1D4ED8` | Button brush |
| Selected bg | `#DBEAFE` | Selected items |
| Success | `#047857` / `#86EFAC` | Positive state |
| Error | `#BE123C` | Error state |

### Light theme

| Role | Hex | Usage |
|------|-----|-------|
| Canvas | `#F3F4F6` / `#F8FAFC` | Page background |
| Surface | `#FFFFFF` | Card background |
| Primary text | `#111827` | Headings |
| Secondary text | `#6B7280` | Body text |
| Muted text | `#64748B` | Hints |
| Border | `#E5E7EB` | Card borders |
| Accent blue | `#2563EB` | Active/selected |
| Selected bg | `#DBEAFE` | Selected row |
| Error | `#BE123C` | Error state |

### Spacing scale

Use multiples of 4: `4, 8, 12, 14, 16, 18, 20, 24, 32`.

### Font sizes

`11, 12, 13, 14, 15, 18, 20, 22, 26, 30`

### Corner radius

`8, 10, 12, 14, 16, 18`

### Centralized palette (recommended for larger samples)

```csharp
internal static class Palette
{
    public const string Canvas = "#0F172A";
    public const string Surface = "#111827";
    public const string SurfaceAlt = "#1E293B";
    public const string Border = "#334155";
    public const string TextPrimary = "#F8FAFC";
    public const string TextSecondary = "#94A3B8";
    public const string TextMuted = "#64748B";
    public const string Accent = "#2563EB";
    public const string AccentDark = "#1D4ED8";
    public const string Error = "#BE123C";
}
```

## Sample Checklist

Before submitting a new sample, verify:

- [ ] `Render()` follows the component structure template (hooks, functions, return order).
- [ ] `return` is on its own line with 4-space indentation.
- [ ] Container children are one per line.
- [ ] Hooks are called unconditionally in the same order on every render.
- [ ] `.Key(...)` is used for dynamic list children.
- [ ] Fast-changing state is in the smallest component that needs it.
- [ ] `useLatest` + `Update` pattern is used for complex state mutations.
- [ ] Colors come from the design conventions above (not arbitrary hex values).
- [ ] `Render()` is under ~150 lines; large UIs are decomposed into static helpers.
- [ ] Helper methods have 7 or fewer parameters.
- [ ] Record types are at the bottom of the file.
- [ ] File-scoped namespace is used.
- [ ] The sample builds with `dotnet build Nuri.sln -c Release`.

## Required References

- Runtime changes: [Runtime Architecture](../../docs/architecture/RUNTIME_ARCHITECTURE.md), [Runtime Identity](../../docs/architecture/RUNTIME_IDENTITY.md), and [Lifecycle](../../docs/architecture/LIFECYCLE.md).
- Renderer changes: [Renderer Contracts](../../docs/renderers/README.md).
- Full repository instructions: [AGENTS.md](../../AGENTS.md).
