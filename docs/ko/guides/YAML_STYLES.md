# YAML 스타일

`Nuri.UI.Styles`는 컴포넌트의 `Render()` 트리에서 시각 값을 분리한다. C#은 계속해서 Element, State, Event Handler를 생성한다. YAML은 지원되는 시각 속성만 제공할 수 있으며, Element 생성, 데이터 바인딩, Type 선택, Handler 설치는 할 수 없다.

## Source 구성

애플리케이션 Root를 만들기 전에 Source를 구성한다. 뒤에 오는 Source는 같은 Style 안의 앞선 Property를 Override한다. 없는 외부 파일은 무시한다.

```csharp
StyleManager.Configure(new StyleConfiguration()
    .AddEmbeddedResource(typeof(Program).Assembly, "MyApp.styles.embedded-default.yml")
    .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "default.yml"))
    .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "theme.yml"))
    .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "override.yml")));
```

Embedded Resource는 시작 시 Fallback이다. `samples/WPF/Nuri.YamlStyleSample` Sample은 세 외부 파일을 실행 파일 옆에 복사하고 이 순서를 보여 준다.

시작 시 `AppContext.BaseDirectory`는 실행 파일이 있는 Directory다. 외부 YAML은 `StyleManager.Configure(...)`가 실행될 때 한 번 읽는다. Nuri는 의도적으로 실행 중 Style File을 감시하거나 Reload하지 않는다. Sample에서는 `bin/<Configuration>/<TargetFramework>/styles/*.yml`을 수정한 뒤 process를 다시 시작한다. 배포 환경에서는 실행 파일 옆 파일을 수정한 뒤 다시 시작한다.

시작 시 외부 YAML이 잘못되면 Embedded Fallback Registry를 유지하고 `StyleManager.LoadFailed`를 발생시킨다. 해당 `StyleLoadError`에는 Source, Line, Column, Validation Message가 있다.

## Style 적용

`.Style("name")`은 `.Key("name")`과 독립적이다. Key는 reconciliation 전용 의미를 유지한다. Inline Element 값은 YAML 값보다 우선한다.

```csharp
public override IElement Render()
{
    return Column(new IElement[]
    {
        Text("Docker").Style("title"),
        Button("Restart").Style("primary")
    })
    .Style("card");
}
```

Deterministic 우선순위는 inherited style, named style, Inline C# Property 순서다. 성공한 Reload로 Style Property가 제거되면 이전에 그 Style로 Property를 받은 Element에서 제거된다. Inline 값은 제거되지 않는다.

## YAML Schema

문서 Root에는 `theme`와 `styles` Mapping만 올 수 있다. 알 수 없는 Root 또는 Style Property는 Validation에 실패한다. 지원되는 Property 이름은 기존 neutral Nuri Property에 매핑된다.

| YAML | Nuri property |
|---|---|
| `width`, `height`, `min-width`, `min-height`, `max-width`, `max-height` | `Width`, `Height`, `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight` |
| `padding`, `margin` | `Padding`, `Margin` |
| `gap` | Row/Column의 `Spacing` |
| `background`, `foreground` | `Background`, `Foreground` |
| `radius` | `CornerRadius` |
| `border-width`, `border-color` | `BorderThickness`, `BorderBrush` |
| `font-size`, `font-weight` | `FontSize`, `FontWeight` |
| `opacity` | `Opacity` |

WPF와 Avalonia는 여섯 Size Property를 모두 materialize한다. Duxel은 현재 `width`와 `height`를 적용하며 immediate-mode layout은 아직 min/max constraint를 노출하지 않는다. State Style(`hover`, `pressed`, `disabled`)도 이 slice에서는 의도적으로 허용하지 않는다. CSS 같은 selector system이 아니라 neutral input-state contract가 필요하기 때문이다.

숫자 시각 값은 finite이고 non-negative여야 한다. `opacity`는 `0..1` 범위다. Color는 `ColorValue.FromHex`가 허용하는 hex string이다. `padding`, `margin`, `border-width`는 숫자 하나, `[vertical, horizontal]`, 또는 `top`, `right`, `bottom`, `left`를 모두 가지는 명시적 Mapping을 허용한다.

```yaml
theme:
  colors:
    surface: "#18191D"
    text: "#F5F5F5"
    primary: "#5B8CFF"
  spacing:
    lg: 20
  radius:
    lg: 16

styles:
  card:
    padding: $spacing.lg
    gap: 12
    background: $colors.surface
    radius: $radius.lg

  title:
    font-size: 24
    font-weight: 700
    foreground: $colors.text

  button:
    padding: [10, 16]
    radius: 8

  primary-button:
    extends: button
    background: $colors.primary
    foreground: "#FFFFFF"
```

Token Reference는 정확히 `$` 뒤에 dotted `theme` path를 쓰는 형태다. Expression Engine, CSS Variable, Selector Cascade, Component-tree YAML은 없다. `extends`는 Base Style 하나만 허용한다. Inheritance Cycle과 없는 Base는 Validation에 실패한다.
