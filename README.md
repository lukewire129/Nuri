# Nuri

Nuri is a C# MVU UI library that keeps application UI descriptions platform-neutral and renders them through adapter projects such as `Nuri.WPF`.

Components return virtual UI descriptions from `Render()`. They do not create native WPF controls directly. Native controls are materialized by the renderer layer.

## Project Layout

- `src/Nuri`: platform-neutral core runtime, virtual UI model, diffing, patch operations, state, events, values, and DSL.
- `src/Nuri.WPF`: WPF renderer adapter, control registry, property/event mapping, animation/materialization, and application runner.
- `src/Nuri.Avalonia`: renderer scaffold. It intentionally has no external Avalonia package dependency yet.
- `src/Nuri.Template`: dotnet template project.
- `samples`: focused sample apps.
- `perf`: performance sanity harnesses.

## Basic App

`App.xaml.cs` owns the WPF application startup and delegates window creation to `NuriApplication`.

```csharp
using System.Windows;
using Nuri.WPF;
using NuriSample.Components;

namespace NuriSample
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            NuriApplication.Run<CounterComponent>("Nuri Sample", width: 400, height: 300);
        }
    }
}
```

Components use the platform-neutral DSL from `Nuri.UI.Dsl`.

```csharp
using Nuri.UI.Dsl;

namespace NuriSample.Components
{
    public class CounterComponent : Component
    {
        public override IElement Render()
        {
            var (count, setCount) = useState(0);

            return Div(
                Button($"Count: {count}", () => setCount(count + 1)),
                Button("Reset", () => setCount(0))
            );
        }
    }
}
```

WPF-familiar factory aliases are available while still producing platform-neutral Nuri elements:

```csharp
Button("Save", Save);
TextBox().OnTextChanged(setText);
CheckBox("Enabled", setEnabled);
RadioButton("Option A", setSelected);
ToggleButton("Pinned", setPinned);
PasswordBox();
```

## Subwindows

Use `NuriApplication.Show<TComponent>()` when the renderer should create and show a new native WPF window.

```csharp
NuriApplication.Show<CounterComponent>("Counter", width: 400, height: 300);
```

Use `NuriApplication.Attach<TComponent>()` when you already own the native `Window` instance.

```csharp
var window = new Window();
NuriApplication.Attach<CounterComponent>(window, "Counter", width: 400, height: 300);
window.Show();
```

Each window gets its own `ApplicationRoot` and virtual tree prefix, so root updates and component subtree updates stay isolated per window.

## Keys And Updates

Nuri performs dirty component subtree render/diff/patch when possible, with root rebuild as fallback.

## Layout

Use `Grid(...)` with fluent `.Rows(...)` and `.Columns(...)` when defining grid layout. This keeps layout definitions separate from visual children.

```csharp
return Grid(
        Header().Row(0).ColumnSpan(2),
        Sidebar().Row(1).Column(0),
        Content().Row(1).Column(1)
    )
    .Rows(Auto, Star)
    .Columns(Pixels(240), Star);
```

The older `Div(Rows(...), Columns(...), children...)` overloads remain available for compatibility, but new code should prefer fluent layout definitions.

## Animation

Call `.Transition(...)` immediately after the property setter that should animate.

```csharp
Text("Play")
    .Margin(30, isPlaying ? 0 : 100, 0, 0)
    .Transition(500, EasingValue.CubicInOut);
```

The older `.Transitions("Margin", ...)` form remains available when the animated property needs to be selected explicitly.

Use explicit keys for reordered lists:

```csharp
Button("Save")
    .Key("save-button")
    .OnClick(Save);
```

`Name` remains a key fallback for compatibility, but new code should prefer `.Key("...")`.

## Template

```powershell
dotnet new install Nuri.Template
```

## Validation

```powershell
dotnet build "Nuri.sln" -c Release
```

Performance sanity checks:

```powershell
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WPFPerformance.csproj" -c Release -- --label after
```

Patch count is an important metric. For keyed reorder sanity, the expected patch count is `1`.
