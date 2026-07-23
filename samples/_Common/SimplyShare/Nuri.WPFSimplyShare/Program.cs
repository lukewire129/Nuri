using Nuri.SimplyShare.App;

using var host = new WpfSimplyShareHost();
AppServices.Initialize(host);

try
{
    NuriApplication.Run<AppShell>("SimplyShare", width: 960, height: 680);
}
finally
{
    AppServices.Dispose();
}
