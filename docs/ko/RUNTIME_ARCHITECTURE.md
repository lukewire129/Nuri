# Runtime 아키텍처 방향

이 문서는 Nuri 컴포넌트 runtime의 설계 방향과 측정된 성능 기준을 기록합니다. scheduling, hook, reconciliation 또는 lifecycle을 변경하기 전에 확인해야 합니다.

## 결정 사항

Nuri는 React와 유사한 선언형 UI 및 lifecycle 의미론을 따르지만 React Fiber 구현을 복제하지 않습니다.

Nuri의 방향은 virtual UI diff를 사용하는 .NET 기반의 가벼운 retained runtime입니다.

```text
state 변경
  -> 플랫폼 scheduler batching
  -> dirty component subtree render
  -> virtual tree diff
  -> 최소 renderer patch
  -> commit 이후 effect 실행
```

유지할 React 계열 계약:

- 부모, 컴포넌트 type, key를 기준으로 하는 논리적 identity
- 순서가 안정적인 hook slot과 논리적 rerender 사이의 state 보존
- keyed reconciliation과 이동 중 state 보존
- key/type 교체 및 unmount 시 cleanup
- commit 이후 effect 실행

측정된 Nuri 요구가 없다면 추가하지 않을 구현:

- alternate/current Fiber 이중 트리
- lane 및 concurrent scheduling
- 중단하거나 재시작할 수 있는 rendering
- 브라우저 DOM event delegation
- Suspense 전용 runtime 구조

WPF Dispatcher, Duxel frame scheduling 및 향후 renderer scheduler는 renderer가 소유합니다. Core는 중립적인 runtime identity, hook, lifecycle, diff 및 patch 설명을 소유합니다.

## Renderer 우선순위

2026-07-15부터 Duxel을 다음 UI backend 개발 우선순위로 둡니다. Avalonia는 기존 adapter 및 regression baseline으로 유지하지만, 프로젝트 우선순위를 명시적으로 다시 정하지 않는 한 신규 backend parity, materialization 및 sample 확장은 Duxel부터 진행합니다.

Duxel 개발에서도 Core 중립성을 유지해야 합니다. 물리 및 solution folder인 `Nuri.Duxel` 아래에는 네 project를 둡니다. `src/Nuri.Duxel/Nuri.Duxel`은 immediate-mode frame projection과 Duxel 전용 property, event, animation materialization을 소유하고, `src/Nuri.Duxel/Nuri.Duxel.Windows`는 Windows application 및 frame-loop integration을 소유하며, `src/Nuri.Duxel/Nuri.Duxel.Diagnostics`는 Duxel diagnostics package를 소유하고, `src/Nuri.Duxel/Nuri.Duxel.PreviewHost`는 out-of-process Visual Studio/VS Code preview host를 소유합니다. 어느 project도 retained native-control 가정을 중심으로 Core 구조를 바꾸면 안 됩니다. Preview extension은 선택한 project의 transitive reference에서 WPF 또는 Duxel을 선택하며, 두 renderer를 모두 참조하는 project는 모호하므로 거부합니다. Visual Studio VSIX와 VS Code prepublish output은 모두 WPF 및 Duxel host를 package합니다.

WPF 및 Duxel preview host는 모두 `net8.0-windows`와 `net9.0-windows`를 target으로 합니다. Visual Studio와 VS Code는 네 host output을 모두 package하고 preview project에 첫 번째로 선언된 target framework와 일치하는 host를 우선 선택하며, 다른 version은 fallback으로 유지합니다.

Duxel preview의 resize 및 zoom 변경은 commit된 Nuri virtual tree를 rebuild하지 않고 projection frame만 요청합니다. Full rebuild는 component output을 바꿀 수 있는 source 또는 metadata update에만 사용합니다.

Duxel preview source 저장 reload에는 150 ms debounce를 사용합니다. `PreviewBuildService`의 Roslyn 경로는 project의 `OutputType`을 반영하고 restore된 project output의 dependency assembly를 포함해야 하며, 이를 통해 `WinExe` 및 top-level statement를 사용하는 Duxel project가 매 edit마다 `dotnet build`로 fallback하지 않게 합니다. 2026-07-21 workstation에서 `Nuri.DuxelSample` partial reload를 반복 측정한 결과 command부터 `Previewing` status까지 median latency가 3,161.9 ms(5회)에서 596.0 ms(7회)로 줄었고, 실제 한 줄 `Text` string 변경은 359.0 ms에 완료되었습니다.

## 현재 Runtime 구조

Runtime tree node는 메모리에 유지되는 identity입니다. state, reducer, ref, memo, effect, store hook 데이터는 runtime node reference를 소유권 key로 사용합니다.

현재 `ComponentBase` 객체는 할당된 runtime node를 직접 캐시합니다. Render 경계에서 node를 한 번 resolve하고, 각 hook은 `Component.Id`로 전역 registry를 반복 검색하지 않고 이 reference를 바로 사용합니다.

`Component.Id`와 `VirtualEntry.Id` 같은 문자열은 호환성, diagnostics, virtual tree 검색 및 renderer patch target을 위해 유지합니다. 문자열을 파싱해서 tree ancestry 또는 hook 소유권을 판단하면 안 됩니다.

WPF와 Avalonia application root는 native build, patch, commit 및 commit 이후 effect 실행에 `RenderCoordinator`를 공통으로 사용합니다. 두 root 모두 `ComponentInvalidationQueue`를 사용하므로 dirty subtree 포함 관계는 renderer별 문자열 ancestry 검사가 아니라 `RuntimeTreeIdentity`를 따릅니다.

`Nuri.Duxel`은 immediate-mode renderer adapter이며 native control tree를 유지하거나 patch하지 않습니다. Nuri state invalidation은 Duxel frame을 요청하고 포함 관계로 정리된 dirty component virtual subtree만 render/diff하며, 각 Duxel frame은 최신 commit된 `VirtualEntry` tree를 `UiImmediateContext` command로 투영합니다. Effect는 이 투영이 commit된 뒤 실행되고, 제거된 component state는 계속 Nuri diff에서 cleanup됩니다. 현재 투영은 linear layout, Pixel/Auto/Star row 및 column track을 사용하는 중첩 Grid placement, 행별 최대 높이에 따른 전진, column span, Scroll child region, scoped margin, padding 및 spacing, draw-list 기반 Div background, border 및 corner radius, 명시적인 widget size, font size, solid foreground color, Grid, Row 및 Column layout slot의 horizontal/vertical element alignment, button horizontal/vertical content alignment, input frame padding 및 click/text-change/check-change event를 지원합니다. Core와 WPF는 더 넓은 neutral hover, mouse, keyboard, focus 및 loaded/unloaded compatibility event model을 제공하며, 더 넓은 Duxel event materialization과 `UnsupportedEvent` diagnostics는 parity 작업으로 남아 있습니다. Opacity transition은 stable virtual entry ID와 Duxel `AnimateFloat`를 사용합니다. Linear/default 및 `CubicOut` easing, 현재 projection value에서 시작하는 interruption, 상속되는 subtree alpha 및 continuation frame 요청을 지원합니다. `Nuri.DuxelAnimatedDashboardSample`은 WPF/Avalonia host와 동일한 platform-neutral `AnimatedDashboardComponent`를 compile합니다. `Nuri.DuxelExplorerTreeSample`도 WPF Explorer component source file을 공유하고 Duxel frame projection을 통해 recursive keyed state와 effect cleanup을 검증합니다. Duxel root, invalidation, rebuild 및 patch batch는 `NuriDiagnostics`에 등록되며, diagnostics가 활성화되면 알려진 unsupported property, animation property 및 opacity easing mode를 중복 없는 `UnsupportedProperty` diagnostics로 기록합니다. Grid row span은 향후 renderer 작업으로 남아 있습니다. Renderer project는 `Duxel.App` `0.2.11-preview`에 의존하고 `net8.0` 및 `net9.0`을 target으로 하며, Windows host project는 `Duxel.Windows.App` `0.2.11-preview`에 의존하고 `net8.0-windows` 및 `net9.0-windows`를 target으로 합니다. Windows host sample은 `net9.0-windows`를 target으로 합니다. Duxel type은 Core 밖에 유지합니다.

`NuriApplication.Run(..., theme: ...)`은 Duxel `UiTheme` preset을 받으며 theme을 생략하면 계속 Windows app color scheme을 따릅니다. `useDuxelTitleBar`와 `integrateSystemChrome`은 window 생성 옵션이며 둘 다 기본값은 `false`입니다. 기본 Windows host는 system이 소유한 native chrome을 유지하고, `integrateSystemChrome`을 활성화하면 Duxel이 native DWM caption/text/border color를 제공하며, `useDuxelTitleBar`를 활성화하면 client area 안에 Duxel title bar를 render합니다. Direct screen host는 `NuriDuxelScreen.RequestTheme(...)`으로 runtime palette 변경을 queue할 수 있고, `NuriApplication`이 host하는 Nuri component는 host scope의 `DuxelThemeController`를 전달받아 global state 없이 같은 변경을 요청할 수 있습니다. `NuriDuxelScreen.CurrentTheme`과 `DuxelThemeController.CurrentTheme`은 첫 frame 전까지 nullable이고, 이후에는 아직 pending인 요청이 아니라 Duxel이 실제 render한 frame에서 관찰한 palette를 제공합니다. `DuxelThemeController.CurrentThemeChanged`는 적용된 palette가 실제로 바뀔 때만 발생하므로 component가 render loop 없이 hook state를 동기화할 수 있습니다. Duxel theme의 `UiColor`는 `.Background(theme.WindowBg)`처럼 neutral color DSL에 직접 사용할 수 있고 `ToColorValue()`로 명시적으로 변환할 수도 있습니다. Adapter는 Core에 Duxel type을 추가하지 않고 RGBA를 보존합니다. Duxel은 mount된 Nuri tree와 hook state를 유지한 채 요청된 palette를 다음 frame 경계에서 적용합니다.

`Nuri.Duxel.Windows`는 host application에 `DuxelPerformanceProfile.Render`와 `DuxelTextRenderingMode.DirectText`를 선택합니다. Duxel 기본값인 `MsaaSamples = 0`에서 `Render`는 `Display` profile의 4x MSAA 대신 1x MSAA로 결정되어 VSync를 변경하지 않으면서 multisample edge 품질보다 UI 처리량을 우선합니다. `DirectText`는 모든 glyph에 Windows platform text backend를 사용하며 품질 우선 기본값입니다. 더 빠른 `Auto` atlas 경로는 실제 application text에서 glyph 모양이 달라지거나 잘못 render될 수 있으므로 선택하지 않습니다. 1,000-text 화면의 12-process cold 측정에서 `Auto`는 첫 frame projection을 15.59 ms에서 11.42 ms로 줄였지만 실행부터 첫 present까지는 637.55 ms에서 634.86 ms로만 바뀌었으므로, 작은 end-to-end 이득이 관찰된 text 품질 저하를 정당화하지 못합니다.

게시된 Duxel application은 첫 frame JIT 작업 대부분을 제거하기 위해 `<PublishReadyToRun>true</PublishReadyToRun>`를 활성화해야 하며, source build는 managed 상태이므로 이 이점을 얻지 못합니다. `samples/Duxel` 아래 project와 `perf/Nuri.DuxelWindowsPerformance`는 publish에 ReadyToRun을 활성화하고, sample은 managed 실행에 `TieredCompilationQuickJitForLoops`도 활성화합니다. 로컬 1,000-text process-cold 비교에서 managed 첫 frame Nuri CPU 중앙값은 5개 process에서 43.13 ms였고 ReadyToRun은 10개 process에서 22.14 ms였으며, 실행부터 첫 present까지의 중앙값은 각각 695.93 ms와 594.60 ms였습니다. ReadyToRun은 측정된 managed application file을 1.63 MB에서 4.10 MB로 늘리므로 `Nuri.Duxel.Windows`가 이 배포 tradeoff를 강제하지 않고 package consumer가 executable project에서 opt in해야 합니다.

`VirtualEntry`는 비어 있는 property, event 및 animation map을 immutable instance로 공유하고 leaf entry에는 child storage를 할당하지 않습니다. Duxel adapter도 비어 있는 element collection의 임시 list를 생략합니다. 1,000-text headless 첫 frame benchmark에서 평균 할당량은 patch count 변경 없이 2,265.37 KB에서 1,988.42 KB로 12.2% 감소했습니다.

Duxel host는 Nuri frame 요청을 받는 동일한 `DuxelAppSession`을 실행하므로 idle-frame skipping 중에도 state 및 animation invalidation에 즉시 깨어납니다. Root content는 이동 가능한 중첩 Duxel window를 만들지 않고 Duxel viewport work area에 직접 투영됩니다. `NuriDuxelScreen`은 frame마다 한 번 `GetMainViewport().WorkPos`와 `WorkSize`를 snapshot하고, 그 logical-coordinate rectangle을 명시적인 root bounds로 전달합니다. 기본 native title bar에서는 Win32 client coordinate에서 non-client chrome이 이미 제외되므로 Nuri renderer가 title-bar inset을 빼지 않습니다. Duxel title bar에서는 host가 측정한 client height에서 설정된 Duxel title-bar height를 빼고 Duxel이 일치하는 viewport work-position inset을 제공합니다. Scroll Div는 content child를 최대 하나만 갖는 viewport입니다. 여러 element는 Column, Row 또는 Grid로 감싸야 하며 spacing은 Scroll이 아니라 그 content layout에 둡니다. 세로 container는 사용 가능한 width를 전달하지만 height는 document order로 소비하며, 명시적 height가 없는 Scroll 영역이나 Star row를 가진 Grid는 모든 child가 parent 전체 height를 상속하는 대신 남은 height를 받습니다. 이 bounds를 넘는 natural content는 공유 Animated Dashboard root와 Explorer detail panel처럼 Scroll을 명시해야 합니다.

`NuriApplication`은 Duxel window creation callback 이후 창 표시를 억제하고 초기 Nuri projection, commit 및 effect flush 뒤에 표시를 해제합니다. 따라서 Vulkan device, swapchain, pipeline 및 font startup 동안 창을 숨기며, 일반적인 사용자 표시 전환은 오랫동안 빈 client area를 보여 주는 대신 창이 없는 상태에서 content가 채워진 첫 frame으로 바로 이어집니다. Renderer가 readiness callback에 도달하지 못하면 5초 native timer가 창 표시를 해제합니다. Direct `DuxelWindowsApp` host는 변경되지 않습니다. 로컬 performance host에서는 upstream Duxel create/show 순서가 callback으로 숨기기 전에 `IsWindowVisible`에 9-13 ms 동안 visible로 관찰되었고 이후 첫 frame 경계까지 hidden 상태를 유지했습니다. 이 마지막 callback 이전 구간까지 제거하려면 Duxel의 `StartHidden` window option이 필요합니다.

Windows host는 순서가 있는 pointer, wheel, key, text, focus 및 실제 `WM_SIZE` resize event를 `DuxelInputEventQueue`에 기록합니다. 연속 pointer-move와 resize sample은 합칠 수 있지만 semantic transition과 wheel sample은 순서와 event 발생 시점의 위치를 유지합니다. 상속된 modal predictive `WM_SIZING` 경로는 Win32가 rectangle을 확정하기 전에 제안된 dimension을 이전 swapchain에 투영할 수 있으므로, grip을 계속 움직일 때 창을 늘리면 작아지고 줄이면 커지는 것처럼 보입니다. 따라서 Windows bridge는 native `WM_NCLBUTTONDOWN` 이후 mouse edge 및 corner grip을 소유하고 pointer를 capture한 뒤 각 움직임을 `SetWindowPos`로 적용합니다. Message pump가 non-modal 상태를 유지하므로 각 실제 `WM_SIZE`가 Duxel의 일반 snapshot 및 swapchain recreation 경로에 도달한 다음 Nuri가 다음 frame을 투영합니다. Pixel/Auto content, text 및 native title-bar metric은 고정되고 Star track만 변경된 client space를 소비하므로 확대와 축소 모두 WPF와 같은 layout 동작을 만듭니다.

Nuri animation, 관성 Scroll 움직임 및 pending input은 Duxel `IsAnimationActiveProvider`에 합성됩니다. Frame 요청은 frame queue가 아니라 계속 합쳐지는 wake signal입니다. Windows 경계의 wheel capture는 event 발생 시점의 pointer 위치와 Nuri Scroll 영역에 overflow가 있는지만 사용합니다. 방향과 현재 offset은 renderer가 순서가 있는 batch를 적용할 때 판단하므로 빠른 방향 반전이 이전 frame snapshot을 기준으로 잘못 분류되지 않습니다. Renderer는 wheel sample을 영역별 velocity impulse로 변환하고 Duxel `GetTime()` frame delta로 offset을 전진시킵니다. 현재 방향의 반복 입력은 제한된 최대 속도 안에서 가속하고, 반대 입력은 기존 velocity를 감속하거나 반전시키며, exponential friction은 움직임을 정지시키고 경계에서는 바깥 방향 velocity를 제거합니다. 빠른 동일 크기의 위/아래 묶음이 화면 표시 전에 상쇄되지 않도록 frame마다 연속된 한 방향의 wheel 묶음을 표시하며, 같은 방향 sample은 계속 한 batch로 처리합니다. `NuriDuxelScreen`은 지연된 입력 또는 관성 Scroll 움직임이 남아 있는 동안 continuation frame을 요청합니다. 이 input queue가 연결되면 Duxel renderer가 Scroll hit region, offset, clipping, 방향별 경계 소비 및 scrollbar drag를 소유하여 wheel 입력을 같은 frame의 해당 content projection 전에 적용합니다. Nuri 소유 Scroll interaction 밖의 Windows message는 계속 Duxel로 전달되어 기존 keyboard, text 및 IME 동작을 보존합니다. Queue가 없는 direct/non-Windows screen host는 Duxel `BeginChild` Scroll 경로를 유지합니다. .NET metadata update가 발생하면 Duxel frame loop에서 전체 root rebuild를 요청하며 stable component identity의 logical runtime state는 유지합니다.

Grid track에는 암시적 gap이 없습니다. 생략된 definition은 하나의 암시적 track을 나타냅니다. 제약된 암시적 row는 arrange된 height를 채우고, 제약되지 않은 암시적 row는 content를 측정합니다. Auto row의 projected height가 estimate와 다르면 이후 row를 client area 밖으로 밀지 않고 원래 arrange height 안에서 남은 Star row를 다시 계산합니다.

Row container는 child에 하나의 공통 vertical origin을 제공하고 측정된 Row height만큼 전진합니다. Duxel의 암시적인 post-item spacing은 Row content height에 포함하지 않으며, Nuri `Spacing` 값만 Row child 사이를 띄웁니다.

Direct/non-Windows host에서는 `GetMainViewport().WorkSize`가 계속 기본 root size입니다. Windows host는 `GetClientRect`와 실제 `WM_SIZE` message에서 측정한 logical client dimension으로 이를 override합니다. 이렇게 native title bar 제외를 명시하고 오래되었거나 host마다 다른 viewport work size가 하단 layout 경계를 이동시키지 않게 합니다.

key, lifecycle, 중복 key 및 cleanup 불변식은 [RUNTIME_IDENTITY.md](RUNTIME_IDENTITY.md)를 참고합니다.

### Duxel Theme 선택

고정 theme용 `NuriApplication.Run(theme, rootFactory, ...)` overload는 선택한 하나의 `UiTheme`을 root factory와 Duxel host 양쪽에 전달합니다. Component에서 palette color가 필요하지만 runtime에 host theme을 변경할 필요는 없을 때 이 overload를 사용합니다. Root에서 palette 값이 필요하지 않은 경우 기존 `NuriApplication.Run(..., theme: ...)` parameter가 간결한 경로로 유지되며, `Func<DuxelThemeController, IElement>`는 runtime switching과 적용된 theme 관찰을 위한 opt-in 경로로 유지됩니다.

## 평탄화 Virtualized Items

큰 renderer-owned 목록은 플랫폼 중립적인 `VirtualizedItems<T>(items, itemTemplate, ...)` 계약을 사용합니다. 고정 sizing은 기본 fast path로 유지됩니다. `itemExtent` 기본값은 `36`이고 item buffer 기본값은 viewport 앞뒤로 각각 `5`입니다. 하나의 `buffer` 값은 양쪽에 동일하게 적용되며, `bufferBefore, bufferAfter` overload로 서로 다른 값을 지정할 수 있습니다. 가변 sizing은 `VirtualizedItems(items, itemTemplate, estimatedItemExtent: ..., bufferPixels: ...)`로 opt-in합니다. Renderer가 측정하기 전까지 estimate로 보이지 않는 row의 위치를 계산하며, buffer는 item 개수가 아니라 pixel 단위 scroll 길이입니다. 선택적인 `itemKey`는 삽입, 삭제, 이동에도 유지되는 안정적인 identity를 제공하고 측정된 extent도 소유합니다. 생략하면 identity는 위치 기준이며 row-local state 또는 측정된 extent가 재정렬된 item을 따라갈 것으로 기대하지 않습니다. 이전 `VirtualizedItems<T>(items, keySelector, itemExtent, itemTemplate, comparer)` overload는 호환성을 위해 유지합니다. Core는 전달된 items를 불변 render snapshot으로 복사하고 `itemTemplate`을 호출하지 않은 채 keyed add, remove, move, update 변경을 담은 `UpdateVirtualizedItemsPatch`를 생성합니다.

`itemTemplate`은 lazy이고 stateless입니다. 일반 `IElement` tree는 반환할 수 있지만 `Component` instance 또는 hook을 포함하면 안 됩니다. Key는 안정적이고 고유해야 합니다. 중복 key는 `DuplicateKey` diagnostics를 생성하고 index-qualified fallback identity를 사용합니다.

WPF adapter는 recycling `VirtualizingStackPanel`과 viewport 기반 row 준비로 이 계약을 materialize합니다. 고정 sizing은 container height를 강제하고 앞/뒤 buffer를 item 단위 `VirtualizationCacheLength`로 매핑합니다. 측정 sizing은 container height를 natural 상태로 두고 `bufferPixels`를 pixel 단위 cache length로 매핑합니다. 동일 key source update와 보이는 keyed move는 native item container를 보존하며, filtering, empty/full replacement, duplicate-key fallback row, 반복 scrolling 및 unload/reload 복구에서도 bounded realized work를 유지합니다.

Duxel은 고정 경로를 고정 extent immediate-mode row clipping으로 유지합니다. 측정 경로는 item identity별 extent를 유지하고, 보이지 않는 row에는 estimate를 사용하며, 누적 scroll 길이를 Fenwick index로 관리하여 extent update와 offset-to-index 탐색을 `O(log n)`으로 유지합니다. 각 frame은 viewport와 설정된 pixel buffer에 대해서만 `itemTemplate`을 호출하고, materialize된 row 높이를 학습하며, 측정값이 바뀌면 안정화 frame을 요청합니다. 첫 번째 visible item은 앞선 측정값이 변하는 동안 scroll anchor로 유지되어 viewport가 점프하지 않습니다. Windows/Nuri renderer-owned Scroll 경로와 direct Duxel `BeginChild` 경로 모두 측정 extent를 지원하며, 고정 direct 경로는 계속 `CalcListClipping`을 사용합니다. Materialize된 row entry에는 item identity 기반 widget id를 부여하며 `VirtualizedItemsSnapshot`은 제한된 frame별 projected count를 보고합니다. `Nuri.DuxelVirtualExplorerTreeSample`은 이 계약으로 10,101개 고정 row를 실행합니다. 기존 `Items(...)`, `ItemsTypes.Tree` 및 eager child reconciliation은 변경되지 않습니다. Avalonia는 아직 `ItemsTypes.Virtualized`를 materialize하지 않습니다.

WPF reconciliation은 작은 구조 변경을 incremental하게 유지합니다. 이전 index의 longest increasing subsequence로 retained-key move count를 추정하며, add, remove 및 필요한 move의 합이 256을 초과하면 제곱 비용의 native move sequence 대신 retained item handle을 재사용하는 collection reset 한 번을 수행합니다. WPF Large List sample은 10,000-row update, swap, reverse, filter, add, remove, replace, reset 및 selection 경로를 실행합니다.

Warmup 이후 `--explorer-comparison` WPF harness는 2026-07-14에 700px viewport에서 동일한 두 버튼 row UI와 10,101개 visible row를 측정했습니다. Eager materialization은 4414.35 ms, 543.17 MB 할당 및 10,101개 native row 생성이었고, fixed-extent virtualization은 269.10 ms, 3.27 MB 할당 및 19개 native row 생성이었습니다. 이 로컬 workload에서 materialization 시간은 16.4x, 할당은 166.2x 감소했습니다.

## Runtime Diagnostics

`NuriDiagnostics`가 활성화되면 등록된 각 application root는 적용된 diff-batch count, 누적 patch count, 마지막 batch size 및 `PatchOperationType`별 count를 기록합니다. 이 counter는 초기 native materialization 이후 `RenderCoordinator` rebuild batch를 대상으로 하며, renderer 내부의 realized-row diff는 root patch batch에 포함하지 않습니다.

Application-root 존재 여부는 `NuriDiagnostics.IsEnabled`와 무관하게 등록하며 component, hook, log 및 patch 상세 수집만 이 값으로 제어합니다. 따라서 application root가 mount된 뒤 DevTools를 활성화해도 현재 live root를 즉시 찾을 수 있습니다. Root disposal은 diagnostics가 비활성화된 상태에서도 항상 존재 record를 unregister하므로 나중에 활성화했을 때 stale root가 나타나지 않습니다.

Renderer-owned virtualized host는 host id, virtual item count 및 realized 또는 projected row count를 담은 중립 `VirtualizedItemsSnapshot` entry도 게시할 수 있습니다. WPF와 Duxel root disposal은 이 entry를 결정적으로 제거합니다. WPF Large List stress 화면은 직전 commit의 patch batch, 누적 patch, component render count 및 realized row count를 표시하여 interactive operation에서 full rebuild 또는 unbounded materialization을 드러냅니다.

Diagnostics가 활성화된 경우 WPF property 경로는 mapper, 쓰기 가능한 CLR property 및 attached-property fallback이 모두 실패한 뒤에만 `RuntimeLogKind.UnsupportedProperty`를 기록합니다. Host-only window property는 의도적인 제외 상태를 유지합니다. Message에는 property와 native control type을 포함하며 `NuriDiagnostics.ClearLogs()`가 호출될 때까지 native control CLR type과 property 이름 조합으로 중복을 억제합니다.

WPF event add 경로도 중립 event를 변환할 수 없거나 변환된 native event가 대상 control에 없으면 `RuntimeLogKind.UnsupportedEvent`를 기록합니다. Native delegate 호환성은 그대로 유지하고 event 제거는 warning을 만들지 않으며, `NuriDiagnostics.ClearLogs()`가 호출될 때까지 native control CLR type과 source event 이름 조합으로 message 중복을 억제합니다.

`Nuri.WPF.Diagnostics`와 `Nuri.Duxel.Diagnostics`는 renderer별 diagnostics package입니다. 두 package는 동일한 platform-neutral inspector component source를 compile하며, 이 component는 `RuntimeSnapshot`을 읽고 Core DSL을 통해 component tree, detail, hook, store, runtime-log 및 console view를 렌더합니다. 두 package는 서로 의존하지 않습니다. WPF는 secondary WPF window에서 inspector를 host하고 Duxel은 별도 Duxel window에서 host합니다. `NuriDiagnostics.Changed`는 검사 대상 renderer thread에서 hook을 변경하지 않고 renderer가 소유한 inspector rebuild를 요청합니다.

`Nuri.WPFDiagnosticsSample`과 `Nuri.DuxelDiagnosticsSample`은 Debug build에서 `UseAttachDevTools()`를 설정하고 hook, store, keyed lifecycle, patch count, console capture 및 renderer별 inspector host를 검증합니다. WPF sample은 선택 component highlighting도 검증합니다.

`WpfDevTools.OpenInspector(...)`, `DuxelDevTools.OpenInspector(...)` 및 `DuxelDevTools.RunInspector(...)`는 선택적인 `snapshotProvider`를 받습니다. 변경 가능한 virtual tree를 소유한 retained renderer는 UI thread에서 snapshot을 capture해야 합니다. WPF는 Dispatcher를 통해 capture를 dispatch하고 Duxel application builder는 Duxel frame boundary에서 capture를 실행합니다.

WPF와 Duxel은 `INuriDebugHost`를 구현하는 lazy `NuriApplication.Create<TComponent>(...)` builder를 노출합니다. Diagnostics package extension인 `UseAttachDevTools()` 및 `UseAttachDevTools(DebugKey)`는 기본 `F12` 또는 명시적인 `F1`부터 `F12` key를 설정합니다. Startup 전에 extension을 호출하면 첫 render 전에 diagnostics가 활성화됩니다. WPF는 한 번의 warning과 함께 늦은 설정을 허용하며, Duxel은 blocking `Run()`이 시작되기 전에 shortcut을 설정해야 합니다.

Inspector root는 WPF와 Duxel 모두에서 `includeInDiagnostics: false`를 사용합니다. Core diagnostics는 initial render 전에 inspector root와 descendant를 제외하고 dispose 시 exclusion을 해제합니다. 따라서 검사 대상 application diagnostics는 계속 활성화하면서 inspector 자신의 render가 diagnostics-change/rebuild loop를 만드는 것을 방지합니다.

기존 Duxel-hosted 공통 `Nuri.DevTools` package는 제거했습니다. Application은 renderer에 맞는 `Nuri.WPF.Diagnostics` 또는 `Nuri.Duxel.Diagnostics` package만 참조합니다. 공유 inspector source는 각 package에 compile되며 세 번째 common package로 publish하지 않습니다.

## 중립 Transform Animation

Core는 renderer-neutral `.Translate(x, y)`, `.TranslateX(...)`, `.TranslateY(...)`, `.Scale(value)`, `.Scale(x, y)`, `.ScaleX(...)` 및 `.ScaleY(...)` DSL property를 제공합니다. `TranslateX`, `TranslateY`, `ScaleX`, `ScaleY` 값은 scalar double이며 `Rotate`와 함께 `.Transition(duration, easing)`에 참여합니다.

WPF는 transform property를 Scale, Rotate, Translate 순서의 중앙 기준 `TransformGroup` 하나로 materialize합니다. 각 축은 독립적인 `DoubleAnimation`을 소유하므로 native control을 교체하지 않고 active animation을 교체하거나 제거할 수 있습니다. 최신 property 값은 animation base value로 유지되며 property를 제거하면 Scale은 `1`, Rotate는 `0`, Translate는 `0`으로 복구됩니다. `Nuri.WPFAnimatedDashboardSample`이 결합 transform transition을 보여줍니다.

## 성능 기준

2026-07-11 Release 빌드에서 측정했습니다. 이 값은 로컬 비교 기준이며 모든 환경에 적용되는 성능 예산은 아닙니다. 같은 장비와 workload에서 전후 결과를 비교하고, 정확성 카운터는 반드시 유지해야 합니다.

Core runtime, warmup 10회 이후 100회 측정:

| 시나리오 | 크기 | 평균 ms | 할당 KB | 필수 결과 |
|---|---:|---:|---:|---:|
| Keyed reorder diff | 1,000 | 1.5134 | 548.55 | patch 1개 |
| State hook 안정 렌더 | 1 | 0.0006 | 0.25 | hook 1개 |
| State hook 안정 렌더 | 10 | 0.0018 | 1.48 | hook 10개 |
| State hook 안정 렌더 | 50 | 0.0102 | 6.95 | hook 50개 |
| Keyed component state mount/dispose | 1,000 | 3.1037 | 1596.31 | state 1,000개 정리 |
| 부모/자식 invalidation 병합 | 자식 1,000개 | 0.3749 | 226.81 | 부모 invalidation 1개 |
| Effect mount/unmount | 1,000 | 2.1434 | 1895.83 | cleanup 1,000개 |

2026-07-19 editor-shaped 집중 측정에서 순서가 바뀌지 않은 keyed reconciliation의 할당이 드러났습니다. Aligned-key fast path는 이제 기존 virtual ID를 유지하고 child를 직접 diff하며, reorder, add, remove 및 duplicate-key case는 일반 reconciliation 경로를 유지합니다.

WPF phase comparison은 동일한 benchmark source를 published Nuri.WPF 0.2.0과 현재 source에 각각 compile합니다. 아래 값은 독립 Release process 5개의 중앙값이며, 각 process는 1,000개 eager keyed line에 대해 warmup 30회와 측정 300회를 수행했습니다. Gen0는 측정 300회 전체의 collection count입니다.

| Phase | Package 0.2.0 ms / KB / Gen0 | 현재 source ms / KB / Gen0 | 필수 결과 |
|---|---:|---:|---:|
| Virtual tree creation | 0.7947 / 695.74 / 25 | 0.7697 / 695.74 / 25 | entry 1,000개 |
| VirtualTreeDiff | 1.3372 / 523.76 / 19 | 0.8431 / 180.98 / 6 | patch 1개 |
| WPF initial build | 6.3666 / 1393.81 / 51 | 6.2872 / 1393.81 / 51 | root 1개 |
| WPF property patch | 0.0003 / 0.04 / 0 | 0.0003 / 0.04 / 0 | patch 1개 |
| Full sequential update | 0.8217 / 1219.54 / 44 | 0.5815 / 876.77 / 32 | patch 1개 |

Isolated diff는 약 36.9% 빨라졌고 할당은 약 65.4%, Gen0 collection은 68.4% 감소했습니다. Virtual-tree creation, diff 및 WPF patch를 포함한 full path는 약 29.2% 빨라졌고 할당은 약 28.1%, Gen0 collection은 27.3% 감소했습니다. 이 결과는 fast path 유지를 뒷받침합니다. Interactive sample이 느리게 나온다면 그 관찰만으로 aligned-key loop를 원인으로 보지 않고 hook, Dispatcher, effect, layout 또는 measurement처럼 이 phase보다 위의 경로를 조사해야 합니다. Nuri.DuxelEditorStressSample은 관련 workload를 100,000개 virtual line으로 interactive하게 유지합니다.

WPF renderer, warmup 10회 이후 100회 측정:

| 시나리오 | 크기 | 평균 ms | 할당 KB | 필수 patch 수 |
|---|---:|---:|---:|---:|
| 초기 native build | 1,000 | 10.0946 | 1393.99 | 0 |
| Keyed native reorder | 1,000 | 6.1023 | 1603.84 | 1 |

Invalidation 병합은 O(1) 중복 enqueue 검사와 runtime parent 순회를 사용합니다. 자식 1,000개 결과는 부모 invalidation 1개를 유지하면서 `3.9679 ms / 673.73 KB`에서 `0.3749 ms / 226.81 KB`로 개선됐습니다.

자식 10,000개 stress 비교는 최적화 전 10회, 최적화 후 확인 측정 30회를 사용했습니다.

| 버전 | 평균 ms | 할당 KB | 유지된 invalidation |
|---|---:|---:|---:|
| 최적화 전 | 232.4499 | 6820.32 | 1 |
| 최적화 후 | 3.0302 | 2224.42 | 1 |

확인 측정에서 시간은 약 76배 개선됐고 할당량은 약 67% 감소했습니다. 결과 수 `1`은 정확성 불변식이며 시간은 실행 환경에 따라 달라질 수 있습니다.

Runtime node 캐시는 안정 렌더 100,000회로 별도 측정했습니다. 집중 측정 결과는 state hook 1개 `0.0005 ms`, 10개 `0.0022 ms`, 50개 `0.0046 ms`였습니다. Hook 렌더 할당량은 각각 `0.25 KB`, `1.48 KB`, `6.95 KB`로 유지됐습니다. 캐시는 hook 값 할당이 아니라 registry 검색을 제거하기 때문입니다. Cached reference 때문에 임시 component 객체 하나당 약 8 bytes가 추가되며, component 1,000개 mount 시나리오에서는 약 `7.8 KB`가 증가했습니다. 짧은 표준 측정은 노이즈 영향을 받으므로 고정된 성능 향상 비율을 주장하지 않고, hook별 registry lock 제거와 소유 구조 단순화를 변경 근거로 삼습니다.

Diagnostics 비활성 상태의 hook formatting은 2026-07-12에 최적화했습니다. 개선 전과 개선 후 값은 각각 warmup 10회와 측정 100회를 수행한 독립 Release process 7개의 중앙값입니다. 이제 value와 dependency summary는 `NuriDiagnostics.IsEnabled`가 `true`일 때만 formatting하며, diagnostics 활성 상태에서는 기존 hook kind, display type, summary를 그대로 유지합니다.

| 시나리오 | 개선 전 할당 KB | 예상 할당 KB | 개선 후 할당 KB | 필수 결과 |
|---|---:|---:|---:|---:|
| State hook 안정 렌더 | 1: 0.25 | 0.18-0.22 | 0.23 | hook 1개 |
| State hook 안정 렌더 | 10: 1.48 | 0.95-1.15 | 1.24 | hook 10개 |
| State hook 안정 렌더 | 50: 6.95 | 4.40-5.40 | 5.78 | hook 50개 |

Hook 50개 할당량은 약 16.8% 감소했지만 예상 상한인 5.40 KB에는 도달하지 못했습니다. 이 결과는 비활성 summary formatting에서 hook당 약 24 bytes가 제거됐고 setter delegate, closure 및 다른 hook 비용은 남아 있음을 보여 줍니다. 더 광범위한 hook store 재작성 근거가 아니라 측정된 부분 성공으로 취급합니다.

State setter 재사용은 2026-07-12에 동일한 독립 process 7개, warmup 10회, 측정 100회 방식으로 측정했습니다. Release IL에서 기존 `useState<T>` 경로가 매 render의 각 hook마다 display class 1개와 setter delegate 1개를 생성하는 것을 확인했습니다. 이제 state slot은 logical runtime node와 hook index에 setter 1개를 유지하고, 이후 render에서 현재 CLR component owner를 갱신합니다.

| 시나리오 | 개선 전 할당 KB | 예상 할당 KB | 개선 후 할당 KB | 개선 전/후 중앙값 ms | 필수 결과 |
|---|---:|---:|---:|---:|---:|
| State hook 안정 렌더 | 1: 0.23 | 0.10-0.15 | 0.12 | 0.0009 / 0.0013 | hook 1개 |
| State hook 안정 렌더 | 10: 1.24 | 0.10-0.20 | 0.15 | 0.0018 / 0.0022 | hook 10개 |
| State hook 안정 렌더 | 50: 5.78 | 0.10-0.30 | 0.31 | 0.0063 / 0.0077 | hook 50개 |

Hook 50개 안정 렌더 할당량은 약 94.6% 감소했습니다. 사전에 정한 예상 상한을 0.01 KB 초과했고 짧은 100회 지연 시간이 증가했으므로 사전 기준에 따른 판정은 부분 성공입니다. Keyed component 1,000개 mount 할당량도 1580.69 KB에서 1557.25 KB로 감소했으며 dispose된 state 수는 정확히 1,000개를 유지했습니다. 개선 후 시간 중앙값은 6.6388 ms이고 범위는 5.9019-7.7229 ms였습니다.

짧은 실행은 GC 비용을 반영할 만큼 충분한 할당 압력을 만들지 않습니다. 별도의 지속 비교에서는 commit된 개선 전 runtime과 working runtime을 분리 worktree에서 실행했습니다. 독립 Release process 7개가 각각 warmup 10,000회 이후 state hook 50개 렌더를 100,000회 측정했습니다.

| 버전 | 총시간 중앙값 ms | Renders/sec | 렌더당 할당 KB | 총 할당 MB | Gen0 | 필수 결과 |
|---|---:|---:|---:|---:|---:|---:|
| 개선 전 | 626.04 | 159734 | 5.77 | 563.83 | 70 | hook 50개 |
| 개선 후 | 197.44 | 506491 | 0.30 | 29.76 | 3 | hook 50개 |

지속 실행 총시간은 약 68.5% 감소했고 처리량은 약 3.17배 증가했습니다. 총 할당량은 약 94.7%, Gen0 횟수는 약 95.7% 감소했습니다. 따라서 문서에 기록한 짧은 지연 tradeoff를 유지하면서도 hook이 많은 지속 workload에서는 setter 재사용을 지지하는 결과입니다.

Application 형태 WPF와 invalidation 검증을 2026-07-12에 추가했습니다. WPF 시나리오는 `Nuri.TodoValidationSample`과 같은 header/input/keyed-list virtual 구조를 사용하며, 의도적으로 sample assembly를 직접 참조하지 않는 renderer harness입니다. 값은 동일한 Release harness에서 commit된 개선 전 runtime과 working runtime을 실행한 중앙값입니다.

| 시나리오 | 개선 전 | 개선 후 | 개선 전/후 할당 KB | 필수 결과 |
|---|---:|---:|---:|---:|
| Todo 형태 initial build, item 1,000개 | 6.1149 ms | 6.2102 ms | 1398.58 / 1398.58 | patch 0개 |
| Todo 형태 keyed reorder, item 1,000개 | 6.4502 ms | 6.2962 ms | 1608.87 / 1608.87 | patch 1개 |
| Invalidation enqueue, child 1,000개 | 0.1941 ms | 0.1862 ms | 147.33 / 147.33 | pending 결과 1 |
| Parent/child coalescing, child 1,000개 | 0.3843 ms | 0.3189 ms | 226.81 / 226.81 | retained 결과 1 |

차이는 실행 환경 노이즈 범위이며 queue 또는 renderer 복잡도를 추가할 근거가 없습니다. 신규 시나리오는 회귀 및 측정 coverage로 유지하고, 이번 slice에서는 추가 runtime 최적화를 하지 않았습니다.

확장한 harness는 개선 전/예상/개선 후 판정에 사용하지 않는 향후 기준선도 제공합니다. Hook 100개 안정 렌더는 0.50 KB와 0.0178 ms였습니다. Hook 1, 10, 50, 100개 first mount 결과는 1.33/3.10/11.21/22.34 KB이고 시간 중앙값은 0.0052/0.0053/0.0136/0.0257 ms였습니다. Setter identity, latest-owner invalidation, functional update, hook slot 격리, trimming, dispose는 회귀 테스트로 검증합니다.

Stale setter 보호를 2026-07-13에 추가했습니다. `useState` setter와 `useReducer` dispatcher는 소유 runtime node reference를 유지하며, replacement가 나중에 동일한 문자열 ID를 재사용하더라도 정확히 그 node가 더 이상 등록되어 있지 않으면 no-op이 됩니다. 100회 sanity run에서 hook 1/10/50/100개의 안정 렌더 할당량은 0.12/0.15/0.31/0.50 KB를 유지했고 keyed reorder 필수 결과도 patch 1개였습니다. 추가 runtime node reference 때문에 first mount 할당량은 1.34/3.18/11.57/23.11 KB로 hook당 약 8 bytes 증가했으며 안정 렌더 할당량은 증가하지 않습니다.

## 측정 시나리오

Core performance harness는 다음을 포함합니다.

- state hook 1개, 10개, 50개를 사용하는 stable component render
- keyed component 1,000개의 state mount 및 runtime node dispose
- parent와 dirty child 1,000개를 하나의 subtree rebuild 요청으로 병합
- effect 1,000개의 mount 및 cleanup
- entry 1,000개 keyed reorder에서 patch 1개 유지

Test suite는 다음을 별도로 검증합니다.

- keyed state 및 effect identity
- key replacement cleanup 및 mount 순서
- nested 및 연속 navigation update
- duplicate-key 격리 및 diagnostics
- root dispose 이후 runtime ancestry registry cleanup

## 최적화 순서

Runtime 복잡성을 늘리기 전에 측정합니다.

1. 할당량 측정 결과가 필요성을 보여줄 때 hook 종류별 dictionary를 순서 기반의 작은 hook slot으로 변경합니다.
2. profiling에서 GC 압력이 확인된 뒤에만 임시 component 또는 closure 할당을 줄입니다.
3. renderer batching과 subtree rendering으로 부족하다는 측정 결과가 있을 때만 scheduling을 복잡하게 만듭니다.

작은 시간 개선을 위해 patch 수, 결정적인 cleanup, keyed state 보존 또는 플랫폼 중립성을 포기하면 안 됩니다.

## 검증 명령

```powershell
dotnet run --project "tests\Nuri.Tests\Nuri.Tests.csproj" -c Release
dotnet run --project "tests\Nuri.RendererTests\Nuri.RendererTests.csproj" -c Release
dotnet build "Nuri.sln" -c Release
dotnet run --project "perf\Nuri.Performance\Nuri.Performance.csproj" -c Release -- --label after
dotnet run --project "perf\Nuri.WPFPerformance\Nuri.WpfPerformance.csproj" -c Release -- --label after
```

Performance 변경에서는 TSV output을 review 또는 handoff note에 유지하고 동일 환경의 before/after 결과를 비교합니다.
