# 시작하기

이 가이드는 작은 Nuri application을 만들고, 새 application이 지켜야 하는 runtime 경계를 설명합니다.

## 1. 시작 방식 선택

Component는 platform-neutral Core DSL 안에 두고 `IElement` description을 반환합니다. WPF와 Avalonia는 두 가지 시작 방식을 지원합니다.

### 최소 시작

작은 WPF application은 Nuri가 window와 root를 만들게 합니다.

```csharp
using Nuri.WPF;

NuriApplication.Run<CounterComponent>("Nuri Sample", width: 480, height: 320);
```

Title과 size 외의 host-owned window 설정이 필요 없다면 이 경로를 사용합니다.

### `Attach`를 사용한 확장 가능한 Host 시작

Native application, window, service, lifetime event 또는 platform-specific integration을 구성해야 한다면 `Attach`를 사용합니다. Host가 window를 만들고 Nuri가 virtual root를 연결합니다.

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

`Attach`는 host application이 lifecycle, window configuration 및 application service를 계속 소유하게 하므로 권장하는 Avalonia 통합 경로입니다. Window가 닫히면 Nuri는 연결된 root를 dispose합니다.

### Avalonia Window Customization

`Nuri.Avalonia`는 host가 소유하는 window를 위한 fluent `Window` extension을 제공합니다. Nuri content를 연결하기 전에 native window를 구성합니다.

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

- `WithTransparentBackground()`는 transparent native background를 요청합니다.
- `ExtendClientAreaIntoTitleBar()`와 `WithTitleBarHeight(...)`는 application content가 title-bar 영역을 차지하게 합니다.
- `WithSystemDecorations(SystemDecorations.None)`는 native title-bar와 border decoration을 제거합니다. 이를 사용한다면 접근 가능한 대체 control과 window-drag 동작을 제공해야 합니다.
- `WithChromeHints(...)`, `WithResize(...)`, `WithTopmost(...)`, `WithTaskbarVisibility(...)`, `WithWindowState(...)`, `WithMinimumSize(...)`, `WithMaximumSize(...)`는 다른 native-window 동작을 구성합니다.

Transparency와 chrome 동작은 operating system과 compositor에 따라 달라집니다. 지원하는 모든 platform에서 선택한 조합을 test해야 합니다.

`Nuri.Duxel.Windows`는 renderer test와 experiment를 위해 계속 제공합니다. 같은 Core component API를 사용하지만 새 application의 권장 시작 renderer는 아닙니다.

```csharp
using Nuri.Duxel;

var app = NuriApplication.Create<CounterComponent>(
    title: "Nuri Duxel",
    width: 720,
    height: 480);

app.Run();
```

## 2. Component 작성

Component는 `Component`를 상속하고 `Render()`에서 UI를 description합니다. Component 안에서 native WPF, Avalonia 또는 Duxel control을 만들면 안 됩니다.

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

`setCount`는 항상 현재 저장된 값을 받습니다. 다음 값이 이전 값에 의존하면 `current => ...`를 사용하고, 값을 교체할 때는 `_ => value`를 사용합니다.

## 3. Stateful List에 명시적 Key 사용

Key는 list item이 이동, filter 또는 교체될 때 component와 hook identity를 보존합니다.

```csharp
return Div(items.Select(item =>
    (IElement)new TodoItemComponent(item).Key(item.Id)
).ToArray());
```

형제 사이에서 유일하고 안정적인 값을 사용합니다. 새 코드는 `.Key(...)`를 사용해야 하며, `Name`은 호환성을 위한 fallback일 뿐입니다.

## 4. 기존 DI Container 연결

Nuri는 `IServiceProvider`를 소비하며 DI container를 만들거나 service dispose를 소유하지 않습니다. 첫 render 전에 provider를 한 번 구성합니다.

다음 예제는 `Microsoft.Extensions.DependencyInjection`을 사용하므로 `ServiceCollection`을 사용한다면 application project에서 해당 package를 참조합니다.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nuri.Runtime;

var services = new ServiceCollection();
services.AddSingleton<ITodoService, TodoService>();

NuriServices.UseServiceProvider(services.BuildServiceProvider());
NuriApplication.Run<AppComponent>("Todo");
```

`useService<T>()`는 `Render()` 중 그 provider에서 resolve합니다. Service state는 component를 자동으로 invalidate하지 않으므로, 관찰 가능한 state는 `Store<T>`로 노출하거나 `useEffect`에서 구독해야 합니다.

## 5. Render 작업을 작게 유지

State update는 renderer가 commit된 virtual subtree를 찾을 수 있으면 dirty component subtree를 schedule합니다. 자주 변경되는 state는 그것이 필요한 가장 작은 component에 둡니다.

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

Timer를 `ClockComponent`에 두면 update 시 dashboard와 settings panel 대신 해당 subtree를 render할 수 있습니다. 외형만을 이유로 component를 나누지 말고 state, update frequency 또는 독립적으로 재사용되는 경계에서 나눕니다. Hook 순서, effect, store 및 component-design 지침은 [Hook Reference](HOOKS.md)를 참고하세요.
