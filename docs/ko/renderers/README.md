# Renderer 계약

Nuri Core는 platform-neutral virtual UI, diffing, runtime state, lifecycle, value model 및 neutral event/animation description을 소유합니다. Renderer project는 framework materialization과 scheduling을 소유합니다. Native framework type을 Core에 노출하면 안 됩니다.

## 공통 계약

- Component `Render()` method는 virtual UI description을 생성합니다.
- Renderer는 invalidation을 schedule하고 commit된 virtual tree를 적용하거나 projection한 뒤 commit 이후 effect를 실행합니다.
- Renderer 차이가 runtime identity, keyed reconciliation, deterministic cleanup 또는 patch semantics를 바꾸면 안 됩니다.
- 기존 application이 native delegate 또는 host type을 요구하는 compatibility overload는 renderer가 소유한 상태로 유지합니다.
- Parity는 동일한 native 구현 방식이 아니라 동등한 사용자 관찰 semantics를 의미합니다.

## Renderer 역할

| Renderer | 역할 |
|---|---|
| WPF | Retained-control 기준 adapter입니다. WPF control 생성, property/event mapping, native patching, Dispatcher integration, animation materialization 및 WPF window hosting을 소유합니다. |
| Avalonia | 기존 retained-control adapter이자 regression baseline입니다. 지원하는 동작을 보존하되 Avalonia type으로 Core 계약을 구성하면 안 됩니다. |
| Duxel | 우선 개발하는 immediate-mode adapter입니다. 각 frame에 최신 commit된 virtual tree를 projection하며 Windows project가 native window, input, frame loop, theme 및 modeless-window integration을 소유합니다. |

상세한 현재 구현, 알려진 gap, 측정값 및 renderer별 동작은 [Runtime Architecture](../architecture/RUNTIME_ARCHITECTURE.md)에서 관리합니다. 한 renderer의 지속적인 materialization 계약을 그 문서에서 명확하게 검토하기 어려워질 때만 renderer별 문서를 추가합니다.
