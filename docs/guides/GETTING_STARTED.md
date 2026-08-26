# Getting Started

This guide creates a small Nuri application and explains the runtime boundaries that new applications need to respect.

## 1. Choose a Starting Model

Components stay in the platform-neutral Core DSL and return `IElement` descriptions. WPF and Avalonia support two starting models.

### Minimal Start

For a small WPF application, let Nuri create the window and root:

```csharp
using Nuri.WPF;

NuriApplication.Run<CounterComponent>("Nuri Sample", width: 480, height: 320);
```

Use this path when the application needs no host-owned window setup beyond title and size.

### Extensible Host Start with `Attach`

Use `Attach` when the application needs to configure the native application, window, services, lifetime events, or platform-specific integrations. The host creates the window and Nuri attaches a virtual root to it.

#### WPF

```csharp
using System.Windows;
using Nuri.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new Window
        {
            Title = "Nuri WPF",
            Width = 720,
            Height = 480
        };

        NuriApplication.Attach(window, new CounterComponent());
        window.Show();
    }
}
```

#### Avalonia

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Nuri.Avalonia;

public sealed class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new Window
            {
                Title = "Nuri Avalonia",
                Width = 720,
                Height = 480
            };

            NuriApplication.Attach(window, new CounterComponent());
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

`Attach` is the recommended Avalonia integration path because the host application continues to own its lifecycle, window configuration, and application services. Nuri disposes its attached root when the window closes.

### Avalonia Window Customization

`Nuri.Avalonia` provides fluent `Window` extensions for host-owned windows. They configure the native window before attaching Nuri content.

```csharp
var window = new Window
{
    Title = "Nuri Avalonia",
    Width = 720,
    Height = 480
}
.WithTransparentBackground()
.ExtendClientAreaIntoTitleBar()
.WithTitleBarHeight(36)
.WithSystemDecorations(SystemDecorations.None)
.WithResize();

NuriApplication.Attach(window, new CounterComponent());
```

- `WithTransparentBackground()` requests a transparent native background.
- `ExtendClientAreaIntoTitleBar()` and `WithTitleBarHeight(...)` let application content occupy the title-bar area.
- `WithSystemDecorations(SystemDecorations.None)` removes native title-bar and border decorations; provide accessible replacement controls and window-drag behavior when using it.
- `WithChromeHints(...)`, `WithResize(...)`, `WithTopmost(...)`, `WithTaskbarVisibility(...)`, `WithWindowState(...)`, `WithMinimumSize(...)`, and `WithMaximumSize(...)` configure other native-window behavior.

Transparency and chrome behavior vary by operating system and compositor. Test the chosen combination on every supported platform.

`Nuri.Duxel.Windows` remains available for renderer testing and experimentation. It uses the same Core component API, but it is not the recommended starting renderer for a new application.

```csharp
using Nuri.Duxel;

var app = NuriApplication.Create<CounterComponent>(
    title: "Nuri Duxel",
    width: 720,
    height: 480);

app.Run();
```

## 2. Write a Component

Components inherit `Component` and describe UI in `Render()`. Do not create native WPF, Avalonia, or Duxel controls inside a component.

```csharp
using Nuri.UI.Dsl;

public sealed class CounterComponent : Component
{
    public override IElement Render()
    {
        var (count, setCount) = useState(0);

        return Div(
            Text($"Count: {count}"),
            Button("Increment", () => setCount(current => current + 1)),
            Button("Reset", () => setCount(_ => 0)));
    }
}
```

`setCount` always receives the current stored value. Use `current => ...` when the next value depends on the previous value, and `_ => value` for replacement.

## 3. Use Explicit Keys for Stateful Lists

Keys preserve component and hook identity when list items move, are filtered, or are replaced.

```csharp
return Div(items.Select(item =>
    (IElement)new TodoItemComponent(item).Key(item.Id)
).ToArray());
```

Use a stable, sibling-unique value. New code should use `.Key(...)`; `Name` is only a compatibility fallback.

## 4. Connect an Existing DI Container

Nuri consumes an `IServiceProvider`; it does not create a DI container or own service disposal. Configure the provider once before the first render.

The following example uses `Microsoft.Extensions.DependencyInjection`; reference that package from the application project when using `ServiceCollection`.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nuri.Runtime;

var services = new ServiceCollection();
services.AddSingleton<ITodoService, TodoService>();

NuriServices.UseServiceProvider(services.BuildServiceProvider());
NuriApplication.Run<AppComponent>("Todo");
```

`useService<T>()` resolves from that provider during `Render()`. Service state does not automatically invalidate a component; expose observable state through `Store<T>` or subscribe in `useEffect`.

## 5. Keep Render Work Local

A state update schedules the dirty component subtree when the renderer can locate its committed virtual subtree. Place rapidly changing state in the smallest component that needs it.

```csharp
public sealed class DashboardComponent : Component
{
    public override IElement Render()
    {
        return Div(
            new ClockComponent(),
            new SettingsPanelComponent());
    }
}

public sealed class ClockComponent : Component
{
    public override IElement Render()
    {
        var (seconds, setSeconds) = useState(0);
        return Text($"Elapsed: {seconds}");
    }
}
```

Keeping a timer in `ClockComponent` lets its updates render that subtree instead of making the dashboard and settings panel participate in the update. Do not split a component only for appearance: split at a state, update-frequency, or independently reusable boundary. Use the [Hook Reference](HOOKS.md) for hook ordering, effects, stores, and component-design guidance.
