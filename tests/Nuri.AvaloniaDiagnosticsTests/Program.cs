using Nuri.Avalonia;
using Nuri.Avalonia.Diagnostics;
using Nuri.Diagnostics.Internal;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Controls;
using Nuri.UI.Dsl;

var host = NuriApplication.Create<ProbeComponent>([], "Avalonia diagnostics test");
var configured = host.UseAttachDevTools();

if (!ReferenceEquals(host, configured))
    throw new InvalidOperationException("UseAttachDevTools should preserve the Avalonia builder instance.");
if (!NuriDiagnostics.IsEnabled)
    throw new InvalidOperationException("UseAttachDevTools should enable diagnostics before startup.");
if (host.HasStarted || host.IsClosed)
    throw new InvalidOperationException("Configuring DevTools should not start or close the Avalonia host.");

try
{
    host.UseAttachDevTools((DebugKey)13);
    throw new InvalidOperationException("UseAttachDevTools should reject keys outside F1 through F12.");
}
catch (ArgumentOutOfRangeException)
{
}

var eagerTree = RuntimeInspectorComponent.BuildVirtualizedTree([], useVirtualizedLists: false);
if (eagerTree.Type != VirtualControlTypes.Div || eagerTree.Kind != DivTypes.Scroll)
    throw new InvalidOperationException("Avalonia diagnostics should use the eager scroll fallback for inspector rows.");

Console.WriteLine("Nuri.AvaloniaDiagnosticsTests passed.");

internal sealed class ProbeComponent : Component
{
    public override IElement Render() => Text("Probe");
}
