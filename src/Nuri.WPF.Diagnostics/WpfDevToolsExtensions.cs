using System.Runtime.Versioning;
using Nuri.Diagnostics.Internal;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;

namespace Nuri.WPF.Diagnostics;

[SupportedOSPlatform("windows")]
public static class WpfDevToolsExtensions
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
            () => WpfDevTools.OpenInspector(host.HighlightComponent, host.CaptureSnapshot));
        return host;
    }
}
