using System;
using Nuri.Navigation;
using Nuri.Runtime;
using Nuri.UI.Dsl;
using Nuri.WPF;

var router = NuriRouter.Create<HomePage>();

var services = new NuriServiceCollection();
services.AddNuriWpfServices();
services.AddSingleton<IProbeService>(new ProbeService("probe-service"));
services.AddSingleton<IRouter>(router);
var provider = services.BuildServiceProvider();

var outlet = new RouterOutlet(router);
outlet.LoadNodeNumber("probe", 1);

using (NuriRuntimeContext.PushServices(provider))
    outlet.ToVirtualEntry();

router.Navigate<DetailPage>(42);

IElement routedElement;
using (NuriRuntimeContext.PushServices(provider))
    routedElement = outlet.Render();

using (NuriRuntimeContext.PushServices(provider))
    routedElement.ToVirtualEntry();

Console.WriteLine("Router service provider probe passed.");

internal interface IProbeService
{
    string Name { get; }
}

internal sealed class ProbeService : IProbeService
{
    public ProbeService(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

internal sealed class HomePage : Component
{
    public override IElement Render()
    {
        var service = useService<IProbeService>();
        if (service.Name != "probe-service")
            throw new InvalidOperationException("Home service was not resolved from the current Nuri service scope.");

        return Text("Home");
    }
}

internal sealed class DetailPage : Component
{
    public override IElement Render()
    {
        var service = useService<IProbeService>();
        if (service.Name != "probe-service")
            throw new InvalidOperationException("Detail service was not resolved from the current Nuri service scope.");

        var id = useRouteParameter<int>();
        if (id != 42)
            throw new InvalidOperationException($"Route parameter mismatch: {id}");

        return Text("Detail");
    }
}
