using System.Net.NetworkInformation;

namespace DriverHealthChecker.App;

internal interface INetworkStatusProvider
{
    bool IsNetworkAvailable();
}

internal sealed class NetworkStatusProvider : INetworkStatusProvider
{
    public bool IsNetworkAvailable() => NetworkInterface.GetIsNetworkAvailable();
}

internal interface IOnlineActionGuard
{
    bool CanOpenOnlineAction(out string message);
}

internal sealed class OnlineActionGuard : IOnlineActionGuard
{
    private readonly INetworkStatusProvider _networkStatusProvider;

    public OnlineActionGuard(INetworkStatusProvider networkStatusProvider)
    {
        _networkStatusProvider = networkStatusProvider;
    }

    public bool CanOpenOnlineAction(out string message)
    {
        if (_networkStatusProvider.IsNetworkAvailable())
        {
            message = string.Empty;
            return true;
        }

        message = "Нет доступа к сети. Проверь подключение к интернету и повтори попытку.";
        return false;
    }
}
