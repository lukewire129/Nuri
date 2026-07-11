# Nuri

Nuri is a C# MVU UI library. Components describe UI with platform-neutral virtual elements, and renderer adapters materialize those descriptions into native controls.

The current supported renderer path is WPF through `Nuri.WPF`.

## What Nuri Does

- Component `Render()` methods return virtual UI descriptions, not native WPF controls.
- State changes re-render the dirty component subtree when possible.
- Virtual trees are diffed into patch operations and applied by the renderer.
- Keys preserve row/component identity across list insert, remove, move, and replace.
- Events, values, animation descriptions, routing, and lifecycle hooks live in Core.
- WPF-specific control creation, property mapping, event mapping, and animation materialization stay in `Nuri.WPF`.

## Project Layout

- `src/Nuri`: platform-neutral runtime, DSL, virtual DOM, diffing, patch operations, values, events, routing, and lifecycle hooks.
- `src/Nuri.WPF`: WPF renderer adapter, WPF control registry, WPF property/event mapping, WPF animation materialization, and application host.
- `samples/WPF`: focused WPF samples that exercise concrete behavior.
- `tests/Nuri.Tests`: lightweight Core behavior tests.
- `perf`: performance sanity harnesses.

## Basic WPF App

Start a Nuri WPF app through `NuriApplication`:

```csharp
using Nuri.WPF;

namespace NuriSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        NuriApplication.Run<CounterComponent>("Nuri Sample", width: 480, height: 320);
    }
}
```

Create components by inheriting `Component` and returning `IElement` from `Render()`:

```csharp
using Nuri.UI.Dsl;

namespace NuriSample;

public sealed class CounterComponent : Component
{
    public override IElement Render()
    {
        var (count, setCount) = useState(0);

            return Div(
                Button($"Count: {count}", () => setCount(current => current + 1)),
                Button("Reset", () => setCount(_ => 0))
            );
        }
    }
}
```

`useState` setters receive the current stored value. Use `setCount(current => current + 1)` for updates based on existing state, and `_ => value` to assign a specific value.

WPF-familiar factory aliases are available while still producing platform-neutral Nuri elements:

```csharp
Button("Save", Save);
TextBox().OnTextChanged(value => setText(_ => value));
CheckBox("Enabled", value => setEnabled(_ => value));
RadioButton("Option A", value => setSelected(_ => value));
ToggleButton("Pinned", value => setPinned(_ => value));
PasswordBox();
```

## State And Hooks

Use `useState` for local component state:

```csharp
var (text, setText) = useState(string.Empty);

return TextBox(text, setText);
```

Use `useEffect` for post-render effects. Omitting dependencies runs after every render. Passing `[]` runs on mount and cleans up on unmount.

If no cleanup is needed, use the `Action` overload and do not return anything:

```csharp
useEffect(() =>
{
    TrackRender();
}, [route]);
```

Return a cleanup action only when the effect owns something that should be disposed or unsubscribed:

```csharp
useEffect(() =>
{
    StartSubscription();
    return StopSubscription;
}, []);
```

Track dependencies with C# collection expressions:

```csharp
useEffect(() =>
{
    Refresh(route);
    return null;
}, [route]);
```

## Routing

Use `useNavigation` when a component owns local navigation state:

```csharp
var (navigation, navigator) = useNavigation("overview");

return Div(
    Button("Overview", () => navigator.Navigate("overview")),
    Button("Details", () => navigator.Navigate("details")),
    Button("Back", navigator.GoBack),
    Router(navigation,
        Route("overview", () => Text("Overview")),
        Route("details", () => Text("Details")))
);
```

`Navigator` supports:

- `Navigate(route)`: push current route and move to `route`.
- `Replace(route)`: change route without adding history.
- `GoBack()`: return to the previous route when available.
- `CanGoBack`: inspect whether the back stack has entries.

## Layout

Use `Grid(...)` with fluent `.Rows(...)` and `.Columns(...)` for layout:

```csharp
return Grid(
        Header().Row(0).ColumnSpan(2),
        Sidebar().Row(1).Column(0),
        Content().Row(1).Column(1)
    )
    .Rows(Auto, Star)
    .Columns(Pixels(240), Star);
```

Use `Div(DivTypes.Scroll, ...)` for scrollable vertical content:

```csharp
return Grid(Rows(Auto, Star),
    Toolbar().Row(0),
    Div(DivTypes.Scroll, rows).Row(1));
```

## Keys

Use explicit keys for rows and components whose identity must survive reorder, filter, edit, or remove operations:

```csharp
Div(items.Select(item =>
    (IElement)new TodoItemComponent(item).Key(item.Id)
).ToArray());
```

`Name` remains a key fallback for compatibility, but new code should prefer `.Key("...")`.

## Animation

Call `.Transition(...)` after the property setter that should animate:

```csharp
Text("Play")
    .Margin(30, isPlaying ? 0 : 100, 0, 0)
    .Transition(500, EasingValue.CubicInOut);
```

Use `.Transitions("Margin", ...)` only when the animated property needs to be selected explicitly.

## WPF Samples

Run samples with `dotnet run --project ... -c Release`.

```powershell
dotnet run --project "samples\WPF\Nuri.TodoValidationSample\Nuri.TodoValidationSample.csproj" -c Release
dotnet run --project "samples\WPF\Nuri.SettingsPreferencesSample\Nuri.SettingsPreferencesSample.csproj" -c Release
dotnet run --project "samples\WPF\Nuri.ModalDialogSample\Nuri.ModalDialogSample.csproj" -c Release
dotnet run --project "samples\WPF\Nuri.CommandPaletteSample\Nuri.CommandPaletteSample.csproj" -c Release
dotnet run --project "samples\WPF\RouterSample\RouterSample.csproj" -c Release
```

Sample coverage:

- `Nuri.TodoValidationSample`: controlled input, list diff, filter, item edit, remove, keyed rows, scroll layout.
- `Nuri.SettingsPreferencesSample`: checkbox, radio, toggle, form grouping, validation.
- `Nuri.ModalDialogSample`: mount/unmount, effect cleanup, overlay layering.
- `Nuri.CommandPaletteSample`: controlled search input, keyboard events, filtered keyed list, selection, command execution.
- `RouterSample`: router, nested router, `useNavigation`, effects, keyed list behavior.

## Validation

Build the solution after meaningful changes:

```powershell
dotnet build "Nuri.sln" -c Release
```

Run Core tests:

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
```

Performance sanity checks:

```powershell
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```

Patch count matters, especially for keyed reconciliation and reorder behavior.
