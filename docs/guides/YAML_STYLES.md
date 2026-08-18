# YAML Styles

`Nuri.UI.Styles` separates visual values from a component's `Render()` tree. C# continues to create elements, state, and event handlers. YAML can only supply supported visual properties; it cannot create elements, bind data, select types, or install handlers.

## Configure Sources

Configure sources before creating an application root. Later sources override earlier properties in the same style. Missing external files are ignored.

```csharp
StyleManager.Configure(new StyleConfiguration()
    .AddEmbeddedResource(typeof(Program).Assembly, "MyApp.styles.embedded-default.yml")
    .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "default.yml"))
    .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "theme.yml"))
    .AddFile(Path.Combine(AppContext.BaseDirectory, "styles", "override.yml")));
```

The embedded resource is the startup fallback. The sample at `samples/WPF/Nuri.YamlStyleSample` copies the three external files beside the executable and demonstrates this order.

At startup, `AppContext.BaseDirectory` is the directory containing the running executable. External YAML is read once when `StyleManager.Configure(...)` runs; Nuri intentionally does not watch style files or reload them in a running application. For the sample, modify `bin/<Configuration>/<TargetFramework>/styles/*.yml`, then restart the process. A deployment modifies the files beside its executable and restarts.

Invalid external YAML at startup leaves the embedded fallback registry active and raises `StyleManager.LoadFailed`; its `StyleLoadError` contains source, line, column, and the validation message.

## Apply a Style

`.Style("name")` is independent from `.Key("name")`; keys retain their reconciliation-only meaning. Inline element values win over YAML values.

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

The deterministic precedence is: inherited style, named style, then inline C# property. A style property removed by a successful reload is removed from elements that previously received it from the style; inline values are never removed.

## YAML Schema

A document has only `theme` and `styles` root mappings. Unknown root or style property names fail validation. Supported property names map to existing neutral Nuri properties:

| YAML | Nuri property |
|---|---|
| `width`, `height`, `min-width`, `min-height`, `max-width`, `max-height` | `Width`, `Height`, `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight` |
| `padding`, `margin` | `Padding`, `Margin` |
| `gap` | `Spacing` on Row/Column |
| `background`, `foreground` | `Background`, `Foreground` |
| `radius` | `CornerRadius` |
| `border-width`, `border-color` | `BorderThickness`, `BorderBrush` |
| `font-size`, `font-weight` | `FontSize`, `FontWeight` |
| `opacity` | `Opacity` |

WPF and Avalonia materialize all six size properties. Duxel currently honors `width` and `height`; its immediate-mode layout does not yet expose min/max constraints. State styles (`hover`, `pressed`, `disabled`) are intentionally not accepted in this slice; they require neutral input-state contracts instead of a CSS-like selector system.

Numeric visual values must be finite and non-negative. `opacity` is constrained to `0..1`. Colors are hex strings accepted by `ColorValue.FromHex`. `padding`, `margin`, and `border-width` accept one number, `[vertical, horizontal]`, or an explicit mapping with all `top`, `right`, `bottom`, and `left` values.

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

A token reference is exactly `$` followed by its dotted `theme` path. There is no expression engine, CSS variables, selector cascade, or component-tree YAML. `extends` accepts one base style only; inheritance cycles and missing bases fail validation.
