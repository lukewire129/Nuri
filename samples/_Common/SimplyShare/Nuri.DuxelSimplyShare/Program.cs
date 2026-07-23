using Nuri.SimplyShare.App;

using var host = new DuxelSimplyShareHost();
AppServices.Initialize(host);

try
{
    NuriApplication.Run<AppShell>(
        "SimplyShare",
        width: 960,
        height: 680,
        windowCreated: host.AttachMainWindow);
}
finally
{
    AppServices.Dispose();
}
