using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Duxel.App;
using Duxel.Core;
using Duxel.Windows.App;
using Nuri.Duxel;
using Nuri.UI.Dsl;
using Nuri.VirtualDom;

internal static class Program
{
    private const uint WindowClose = 0x0010;
    private const uint NoMove = 0x0002;
    private const uint NoZOrder = 0x0004;
    private const uint NoActivate = 0x0010;
    private static readonly ConcurrentQueue<NuriDuxelFrameTiming> NuriFrames = new();
    private static readonly ConcurrentQueue<double> ScreenFrames = new();
    private static int _resizeMessageCount;

    private static int Main(string[] args)
    {
        var mode = GetOption(args, "--mode") ?? "nuri";
        var size = GetIntOption(args, "--size", 1_000);
        var resizeSteps = GetIntOption(args, "--resize-steps", 60);
        var exitDelayMs = GetIntOption(args, "--exit-delay-ms", 500);
        var vsync = GetBoolOption(args, "--vsync", true);

        Console.WriteLine(
            $"mode={mode} size={size} resize_steps={resizeSteps} exit_delay_ms={exitDelayMs} vsync={vsync}");
        switch (mode.ToLowerInvariant())
        {
            case "raw":
                RunDirect(new RawDuxelScreen(size), "Raw Duxel", resizeSteps, exitDelayMs, vsync);
                break;
            case "projection":
                RunDirect(
                    new ProjectionScreen(PerfTreeFactory.CreateReorderedTree(size).Old),
                    "Nuri VirtualEntry Projection",
                    resizeSteps,
                    exitDelayMs,
                    vsync);
                break;
            case "nuri":
                RunNuri(size, resizeSteps, exitDelayMs, vsync);
                break;
            default:
                Console.Error.WriteLine("--mode must be raw, projection, or nuri.");
                return 2;
        }

        PrintSummary(mode);
        return 0;
    }

    private static void RunDirect(
        UiScreen screen,
        string title,
        int resizeSteps,
        int exitDelayMs,
        bool vsync)
    {
        var session = new DuxelAppSession();
        var timedScreen = new TimedScreen(screen, elapsed => ScreenFrames.Enqueue(elapsed.TotalMilliseconds));
        var options = DuxelApp.Options(timedScreen, title, 900, 640, vsync);
        var existingCreated = options.Window.WindowCreated;
        options = options with
        {
            Debug = options.Debug with
            {
                Log = message => Console.WriteLine($"duxel\t{message}"),
                LogStartupTimings = true,
                LogEveryNFrames = 1
            },
            Renderer = options.Renderer with
            {
                Profile = DuxelPerformanceProfile.Render
            },
            Window = options.Window with
            {
                UseDuxelTitleBar = false,
                IntegrateSystemChrome = false,
                WindowCreated = handle =>
                {
                    existingCreated?.Invoke(handle);
                    StartResizeDriver(
                        handle,
                        resizeSteps,
                        exitDelayMs,
                        () => PostMessage(handle, WindowClose, 0, 0));
                }
            }
        };

        DuxelWindowsApp.Run(options, session);
    }

    private static void RunNuri(int size, int resizeSteps, int exitDelayMs, bool vsync)
    {
        NuriApplication.Run(
            new FirstFrameComponent(size),
            title: "Full Nuri Duxel",
            width: 900,
            height: 640,
            vsync: vsync,
            includeInDiagnostics: false,
            windowCreated: handle => StartResizeDriver(
                handle,
                resizeSteps,
                exitDelayMs,
                () => PostMessage(handle, WindowClose, 0, 0)),
            performance: new NuriDuxelPerformanceOptions
            {
                FrameCompleted = NuriFrames.Enqueue,
                ResizeMessageReceived = _ => Interlocked.Increment(ref _resizeMessageCount),
                DuxelLog = message => Console.WriteLine($"duxel\t{message}"),
                LogDuxelStartupTimings = true,
                DuxelLogEveryNFrames = 1
            });
    }

    private static void StartResizeDriver(nint windowHandle, int steps, int exitDelayMs, Action exit)
    {
        _ = Task.Run(() =>
        {
            Thread.Sleep(750);
            for (var index = 0; index < steps; index++)
            {
                var large = (index & 1) == 0;
                _ = SetWindowPos(
                    windowHandle,
                    0,
                    0,
                    0,
                    large ? 1120 : 700,
                    large ? 720 : 480,
                    NoMove | NoZOrder | NoActivate);
                Thread.Sleep(16);
            }

            Thread.Sleep(exitDelayMs);
            exit();
        });
    }

    private static void PrintSummary(string mode)
    {
        Console.WriteLine();
        Console.WriteLine("metric\tcount\tp50_ms\tp95_ms\tp99_ms");
        if (NuriFrames.Count > 0)
        {
            PrintDistribution(
                "nuri_initial_total",
                NuriFrames.Where(frame => frame.IsInitialFrame).Select(frame => frame.TotalDuration.TotalMilliseconds));
            PrintDistribution(
                "nuri_initial_runtime",
                NuriFrames.Where(frame => frame.IsInitialFrame).Select(frame => frame.RuntimeUpdateDuration.TotalMilliseconds));
            PrintDistribution(
                "nuri_initial_projection",
                NuriFrames.Where(frame => frame.IsInitialFrame).Select(frame => frame.ProjectionDuration.TotalMilliseconds));
            PrintDistribution("nuri_total", NuriFrames.Select(frame => frame.TotalDuration.TotalMilliseconds));
            PrintDistribution("nuri_runtime", NuriFrames.Select(frame => frame.RuntimeUpdateDuration.TotalMilliseconds));
            PrintDistribution("nuri_projection", NuriFrames.Select(frame => frame.ProjectionDuration.TotalMilliseconds));
            PrintDistribution(
                "resize_to_projection",
                NuriFrames
                    .Where(frame => frame.ResizeToProjectionDuration is not null)
                    .Select(frame => frame.ResizeToProjectionDuration!.Value.TotalMilliseconds));
            Console.WriteLine($"wm_size_messages\t{_resizeMessageCount}\t-\t-\t-");
            Console.WriteLine($"projected_resize_frames\t{NuriFrames.Count(frame => frame.HadResizeInput)}\t-\t-\t-");
        }
        else
        {
            PrintDistribution($"{mode}_screen", ScreenFrames);
            PrintDistribution($"{mode}_initial_screen", ScreenFrames.Take(1));
        }
    }

    private static void PrintDistribution(string name, IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        Console.WriteLine(
            $"{name}\t{ordered.Length}\t{Percentile(ordered, 0.50):F4}\t{Percentile(ordered, 0.95):F4}\t{Percentile(ordered, 0.99):F4}");
    }

    private static double Percentile(double[] orderedValues, double percentile)
    {
        if (orderedValues.Length == 0)
        {
            return 0d;
        }

        var index = (int)Math.Ceiling(percentile * orderedValues.Length) - 1;
        return orderedValues[Math.Clamp(index, 0, orderedValues.Length - 1)];
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static int GetIntOption(string[] args, string name, int fallback)
    {
        return int.TryParse(GetOption(args, name), out var parsed) && parsed >= 0
            ? parsed
            : fallback;
    }

    private static bool GetBoolOption(string[] args, string name, bool fallback)
    {
        return bool.TryParse(GetOption(args, name), out var parsed) ? parsed : fallback;
    }

    private sealed class TimedScreen(UiScreen inner, Action<TimeSpan> completed) : UiScreen
    {
        public override void Render(UiImmediateContext ui)
        {
            var started = Stopwatch.GetTimestamp();
            inner.Render(ui);
            completed(Stopwatch.GetElapsedTime(started));
        }
    }

    private sealed class RawDuxelScreen(int size) : UiScreen
    {
        public override void Render(UiImmediateContext ui)
        {
            ui.EnableRootViewportContentLayout(contentPadding: 0f);
            for (var index = 0; index < size; index++)
            {
                ui.Text("value");
            }
        }
    }

    private sealed class ProjectionScreen : UiScreen, IDisposable
    {
        private readonly DuxelVirtualEntryRenderer _renderer = new();
        private readonly VirtualEntry _entry;

        public ProjectionScreen(VirtualEntry entry)
        {
            _entry = entry;
        }

        public override void Render(UiImmediateContext ui)
        {
            ui.EnableRootViewportContentLayout(contentPadding: 0f);
            var viewport = ui.GetMainViewport();
            _renderer.Render(
                ui,
                _entry,
                new UiRect(
                    viewport.WorkPos.X,
                    viewport.WorkPos.Y,
                    viewport.WorkSize.X,
                    viewport.WorkSize.Y));
        }

        public void Dispose()
        {
            _renderer.Dispose();
        }
    }

    private sealed class FirstFrameComponent(int size) : Component
    {
        public override IElement Render()
        {
            var children = new IElement[size];
            for (var index = 0; index < children.Length; index++)
            {
                children[index] = Text("value").Key($"item-{index}");
            }

            return Div(children);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(
        nint windowHandle,
        uint message,
        nuint wParam,
        nint lParam);
}
