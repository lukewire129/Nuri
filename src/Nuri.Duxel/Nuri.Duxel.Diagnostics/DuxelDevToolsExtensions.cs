using System.Runtime.Versioning;
using Nuri.Diagnostics.Internal;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;

namespace Nuri.Duxel.Diagnostics;

[SupportedOSPlatform("windows")]
public static class DuxelDevToolsExtensions
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
            () => DuxelDevTools.OpenInspector(host.HighlightComponent, host.CaptureSnapshot));
        return host;
    }
}
