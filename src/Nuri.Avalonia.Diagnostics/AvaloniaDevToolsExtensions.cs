using Nuri.Diagnostics.Internal;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;

namespace Nuri.Avalonia.Diagnostics;

public static class AvaloniaDevToolsExtensions
{
    public static NuriApplicationBuilder<TComponent> UseAttachDevTools<TComponent>(
        this NuriApplicationBuilder<TComponent> host)
        where TComponent : Component, new()
    {
        return UseAttachDevTools(host, DebugKey.F12);
    }

    public static NuriApplicationBuilder<TComponent> UseAttachDevTools<TComponent>(
        this NuriApplicationBuilder<TComponent> host,
        DebugKey key)
        where TComponent : Component, new()
    {
        DevToolsRuntime.Configure(
            host,
            key,
            () => AvaloniaDevTools.OpenInspector(host.HighlightComponent, host.CaptureSnapshot));
        return host;
    }
}
