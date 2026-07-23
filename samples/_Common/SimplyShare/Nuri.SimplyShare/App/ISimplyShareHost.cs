using Nuri.SimplyShare.Features.Discovery;

namespace Nuri.SimplyShare.App;

public interface ISimplyShareHost
{
    void OpenSettings();

    void OpenChat(DeviceInfo device);

    string[] SelectFiles(string title);
}
