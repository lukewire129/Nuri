# Nuri

Nuri is a C# MVU UI library that keeps application UI descriptions platform-neutral and renders them through adapter projects such as `Nuri.WPF`.

Components return virtual UI descriptions from `Render()`. They do not create native WPF controls directly. Native controls are materialized by the renderer layer.

## Project Layout

- `src/Nuri`: platform-neutral core runtime, virtual UI model, diffing, patch operations, state, events, values, and DSL.
- `src/Nuri.WPF`: WPF renderer adapter, control registry, property/event mapping, animation/materialization, and application runner.
- `src/Nuri.Avalonia`: renderer scaffold. It intentionally has no external Avalonia package dependency yet.
- `src/Nuri.OpenSilver`: renderer scaffold. It intentionally has no external OpenSilver package dependency yet.
- `src/Nuri.MewUI`: renderer scaffold. It intentionally has no external MewUI package dependency yet.
- `src/Nuri.Template`: dotnet template project.
- `samples`: focused sample apps, including `NuriFlowSample` for host-first router/window/native-control integration.
- `perf`: performance sanity harnesses.

## Basic App

`App.xaml.cs` can delegate window creation to `NuriApplication` for small apps and samples.

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

For larger WPF apps, keep the native application startup and attach Nuri to a host control instead of letting Nuri own the window.

```csharp
NuriApplication.Attach(MainContentHost, new AppShell(), services =>
{
    services.AddSingleton<IRouter>(NuriRouter.Create<HomePage>());
});
```

If the app already has an `IServiceProvider`, pass it as the fallback provider. Nuri services can override or add flow-specific services while existing MVVM services remain available through `useService<T>()`.

```csharp
NuriApplication.Attach(MainContentHost, new AppShell(), appServices, services =>
{
    services.AddSingleton<IRouter>(NuriRouter.Create<HomePage>());
});
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

Components can also open windows or dialogs through the runtime window service. This keeps window flow in Nuri components instead of pushing it into view models.

```csharp
public sealed class ToolsPage : Component
{
    public override IElement Render()
    {
        var windows = useWindows();

        return Div(
            Button("Open inspector", () =>
                windows.Show<InspectorPage>(new NuriWindowOptions
                {
                    Title = "Inspector",
                    Width = 420,
                    Height = 640
                })),
            Button("Confirm", () =>
                windows.ShowDialogAsync<ConfirmDialog>(new NuriWindowOptions
                {
                    Title = "Confirm",
                    Width = 360,
                    Height = 220
                }))
        );
    }
}
```

Dialog components can close themselves with a result through `useDialog()`.

```csharp
public sealed class ConfirmDialog : Component
{
    public override IElement Render()
    {
        var dialog = useDialog();

        return Div(
            Text("Continue?"),
            Button("OK", () => dialog.Close(true)),
            Button("Cancel", () => dialog.Close(false))
        );
    }
}
```

`Nuri.WPF` registers the WPF window service by default for `NuriApplication.Attach(...)`, `Run(...)`, and `NuriComponentHost<TComponent>`. Other renderers can provide their own `IWindowService` without changing Core components.

## App Flow

Nuri is intended to own state-driven screen flow without requiring each UI framework renderer to own startup. Keep `IRouter` as an application service, read it with `useRouter()`, and render the current page with `Outlet(router)`.

```csharp
using Nuri.Navigation;
using Nuri.UI.Dsl;

public sealed class AppShell : Component
{
    public override IElement Render()
    {
        var router = useRouter();

        return Grid(
                Sidebar(),
                Outlet(router)
                    .FadeIn(180, EasingValue.CubicOut)
                    .FadeOut(120, EasingValue.CubicIn)
                    .Column(1)
            )
            .Columns(Pixels(240), Star);
    }

    private IElement Sidebar()
    {
        var router = useRouter();

        return Div(
            Button("Home", () => router.Navigate<HomePage>()),
            Button("Settings", () => router.Navigate<SettingsPage>()),
            Button("User 42", () => router.Navigate<UserDetailPage>(42)),
            Button("Back", () => router.Back())
        );
    }
}
```

Route pages can read parameters from the current route.

```csharp
public sealed class UserDetailPage : Component
{
    public override IElement Render()
    {
        var userId = useRouteParameter<int>();
        return Text($"User {userId}");
    }
}
```

When route changes remove or replace component subtrees, Nuri disposes component owners attached to the old virtual subtree.

Page components can override lifecycle hooks when they need to start or stop work around screen transitions.

```csharp
public sealed class SettingsPage : Component
{
    protected override void OnEnter()
    {
        // Start page-local work.
    }

    protected override void OnLeave()
    {
        // Stop page-local work before the page is removed.
    }

    protected override void OnDispose()
    {
        // Release resources owned by this component.
    }

    public override IElement Render()
    {
        return Text("Settings");
    }
}
```

This keeps page transition decisions out of view models while still allowing WPF, Avalonia, OpenSilver, or other renderers to decide how a Nuri root is hosted.

## Renderer Hosting

Renderers should not require ownership of application startup. The preferred integration shape is a host adapter that attaches a Nuri root to a native host control already owned by the framework app.

```csharp
public sealed class WpfHostAdapter : INuriHostAdapter<ContentControl, FrameworkElement>
{
    public NuriMountedRoot<FrameworkElement> Attach(
        ContentControl host,
        IElement rootElement,
        NuriServiceProvider? services = null)
    {
        var root = NuriApplication.Attach(host, rootElement, services);
        return new NuriMountedRoot<FrameworkElement>(
            root.RootVisual,
            root.Services,
            root.Dispose);
    }
}
```

Avalonia, OpenSilver, MewUI, and other C# UI renderers should follow the same shape. The scaffold projects compile this contract without taking an external renderer package dependency until package direction is decided:

```text
native app startup
  -> native host control
    -> renderer host adapter Attach(host, rootElement, services)
      -> Nuri runtime/diff/animation/navigation services
```

This lets each UI framework keep its own startup model while Nuri owns screen flow, page lifecycle, route transitions, and window/dialog services inside the mounted root.

## Native Controls

Nuri should not wrap every native or third-party control. Use renderer-specific native escape hatches for heavy controls such as charts, grids, document viewers, or editors.

```csharp
using Nuri.WPF;

public sealed class ChartPage : Component
{
    public override IElement Render()
    {
        return Div(
            Text("Sales"),
            WpfNative.Control(() =>
            {
                var chart = new CartesianChart();
                chart.Series = CreateSeries();
                return chart;
            })
        );
    }
}
```

The native control is created by the renderer path, not by `Nuri` Core. This keeps Core platform-neutral while still allowing WPF, Avalonia, OpenSilver, MewUI, or other renderers to expose their own native-control adapters.

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

## Styling

Use `.Style(...)` to apply a WPF style resource by key through the WPF renderer.

```csharp
Button("Save", Save)
    .Style("PrimaryButton");
```

Use `.Class(...)` or `.Classes(...)` when you want an Avalonia-like class list in the Nuri DSL.

```csharp
Button("Save", Save)
    .Style("ButtonBase")
    .Classes("Primary", "Dense");
```

In `Nuri.WPF`, class names are resolved as WPF `Style` resource keys and their setters are applied in order. WPF does not have native multi-class selectors, so use `.Style(...)` for full WPF styles with templates or triggers, and use `.Classes(...)` for small composable setter styles.

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
