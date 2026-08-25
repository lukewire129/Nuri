# Renderer 계약

Nuri Core는 platform-neutral virtual UI, diffing, runtime state, lifecycle, value model 및 neutral event/animation description을 소유합니다. Renderer project는 framework materialization과 scheduling을 소유합니다. Native framework type을 Core에 노출하면 안 됩니다.

## 공통 계약

- Component `Render()` method는 virtual UI description을 생성합니다.
- Renderer는 invalidation을 schedule하고 commit된 virtual tree를 적용하거나 projection한 뒤 commit 이후 effect를 실행합니다.
- Renderer 차이가 runtime identity, keyed reconciliation, deterministic cleanup 또는 patch semantics를 바꾸면 안 됩니다.
- 기존 application이 native delegate 또는 host type을 요구하는 compatibility overload는 renderer가 소유한 상태로 유지합니다.
- Parity는 동일한 native 구현 방식이 아니라 동등한 사용자 관찰 semantics를 의미합니다.

## Native Island

`Native<TNative>(mount: ..., render: ...)`는 Core가 해당 framework를 참조하지 않은 채 기존 native control을 도입하는 renderer 소유 escape hatch입니다. Core는 native CLR type, factory, mount cleanup 및 render callback만 neutral descriptor로 전달합니다. WPF는 `FrameworkElement` type을 받고 Avalonia는 `Control` type을 받으며, 다른 renderer는 호환되지 않는 type을 명시적으로 거부해야 합니다.

`mount`는 유지되는 native instance마다 한 번 실행되며 unmount cleanup을 반환할 수 있습니다. `render`는 초기 mount 뒤와 유지되는 Nuri render가 commit된 뒤 실행되므로 `INotifyPropertyChanged` 없이 Nuri state를 native control에 projection할 수 있습니다. Native island는 leaf입니다. Nuri는 내부 native tree를 reconcile하지 않고, 내부 event를 map하지 않으며, renderer 간 portability를 제공하지 않습니다. Native control이 sibling 사이에서 이동할 수 있으면 안정적인 `.Key(...)`를 사용합니다.

Visual Studio와 VS Code preview extension은 WPF project에 동일한 Nuri WPF preview host를 사용하므로 editor별 integration 없이 두 preview에서 `Native<FrameworkElement>`가 동작합니다. Duxel은 immediate-mode renderer이므로 native island를 materialize하지 않습니다.

## Renderer 역할

| Renderer | 역할 |
|---|---|
| WPF | Retained-control 기준 adapter입니다. WPF control 생성, property/event mapping, native patching, Dispatcher integration, animation materialization 및 WPF window hosting을 소유합니다. |
| Avalonia | 기존 retained-control adapter이자 regression baseline입니다. 지원하는 동작을 보존하되 Avalonia type으로 Core 계약을 구성하면 안 됩니다. `Nuri.Avalonia.WindowExtensions`는 transparency, client-area title bar, chrome hints, decorations, resizing, taskbar visibility, window state 및 size limit을 위한 fluent helper를 제공합니다. |
| Duxel | 우선 개발하는 immediate-mode adapter입니다. 각 frame에 최신 commit된 virtual tree를 projection하며 Windows project가 native window, input, frame loop, theme 및 modeless-window integration을 소유합니다. |

상세한 현재 구현, 알려진 gap, 측정값 및 renderer별 동작은 [Runtime Architecture](../architecture/RUNTIME_ARCHITECTURE.md)에서 관리합니다. 한 renderer의 지속적인 materialization 계약을 그 문서에서 명확하게 검토하기 어려워질 때만 renderer별 문서를 추가합니다.
