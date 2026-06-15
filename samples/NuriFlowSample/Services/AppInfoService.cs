namespace NuriFlowSample.Services
{
    public sealed class AppInfoService : IAppInfoService
    {
        public AppInfoService(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
