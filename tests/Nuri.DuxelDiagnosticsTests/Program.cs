using Nuri.Duxel;
using Nuri.Duxel.Diagnostics;
using Nuri.Runtime.Diagnostics;
using Nuri.UI.Dsl;

var host = NuriApplication.Create<ProbeComponent>("Duxel diagnostics test");
var configured = host.UseAttachDevTools();

if (!ReferenceEquals(host, configured))
    throw new InvalidOperationException("UseAttachDevTools should preserve the Duxel builder instance.");
if (!NuriDiagnostics.IsEnabled)
    throw new InvalidOperationException("UseAttachDevTools should enable diagnostics before startup.");
if (host.HasStarted || host.IsClosed)
    throw new InvalidOperationException("Configuring DevTools should not start or close the Duxel host.");

try
{
    host.UseAttachDevTools((DebugKey)13);
    throw new InvalidOperationException("UseAttachDevTools should reject keys outside F1 through F12.");
}
catch (ArgumentOutOfRangeException)
{
}

Console.WriteLine("Nuri.DuxelDiagnosticsTests passed.");

internal sealed class ProbeComponent : Component
{
    public override IElement Render() => Text("Probe");
}
