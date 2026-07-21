# Duxel 성능 분석

Nuri CPU 작업과 Duxel Windows/Vulkan 경로를 분리하려면 두 하네스를 사용한다.

## Headless CPU 비교

```powershell
dotnet run --project "perf\Nuri.DuxelPerformance\Nuri.DuxelPerformance.csproj" -c Release -- --label current --size 1000
```

첫 번째 표는 기존 투영 및 패치 수 시나리오를 유지한다. 첫 프레임 표는 순수 Duxel 위젯 명령 생성, 미리 만든 `VirtualEntry` 투영, 전체 Nuri 컴포넌트 프레임을 비교한다. 세 경로 모두 `GetDrawData`에서 측정을 끝내며 Vulkan 제출과 present는 포함하지 않는다.

## Windows 리사이즈 추적

같은 크기, 리사이즈 횟수, VSync 설정, 디스플레이, DPI, GPU 조건에서 각 모드를 실행한다.

```powershell
dotnet run --project "perf\Nuri.DuxelWindowsPerformance\Nuri.DuxelWindowsPerformance.csproj" -c Release -- --mode raw --size 1000 --resize-steps 60 --vsync true
dotnet run --project "perf\Nuri.DuxelWindowsPerformance\Nuri.DuxelWindowsPerformance.csproj" -c Release -- --mode projection --size 1000 --resize-steps 60 --vsync true
dotnet run --project "perf\Nuri.DuxelWindowsPerformance\Nuri.DuxelWindowsPerformance.csproj" -c Release -- --mode nuri --size 1000 --resize-steps 60 --vsync true
```

하네스는 네이티브 창을 1120x720과 700x480 사이에서 번갈아 변경하고 Duxel 시작 및 프레임별 로그를 활성화한 뒤 p50/p95/p99를 출력한다. Nuri 모드는 런타임, 투영, 전체 프레임, 리사이즈에서 투영까지의 지연, 원시 `WM_SIZE` 수, 투영된 리사이즈 프레임 수도 보고한다.

- 순수 Duxel부터 느리면 Duxel 프레임 스케줄링, swapchain 재생성, Vulkan 제출, present, GPU/드라이버를 조사한다.
- 순수 Duxel은 빠르고 미리 만든 트리 투영이 느리면 `DuxelVirtualEntryRenderer` 레이아웃과 명령 생성을 조사한다.
- 미리 만든 트리 투영은 빠르고 전체 Nuri만 느리면 컴포넌트 렌더, 가상 트리 생성, diff, commit, effect를 조사한다.
- 모든 CPU 단계는 빠르지만 화면 표시가 늦으면 Duxel 로그 타임스탬프를 PresentMon 또는 PIX와 연계한다. Duxel 0.2.10 공개 API를 통해서는 Nuri가 GPU 완료 시점을 관찰할 수 없다.

애플리케이션별 추적에는 `NuriDuxelPerformanceOptions`를 `NuriApplication.Run`에 전달한다. 수집은 명시적으로 켜야 하며 observer가 없으면 `NuriDuxelScreen`은 성능 타임스탬프를 읽지 않는다.

```csharp
NuriApplication.Run(
    root,
    performance: new NuriDuxelPerformanceOptions
    {
        FrameCompleted = timing => Record(timing),
        ResizeMessageReceived = message => Record(message),
        DuxelLog = Console.WriteLine
    });
```
