namespace DriverHealthChecker.App;

internal sealed class IntelSupportResolver
{
    private readonly IInstalledOfficialAppCatalog _installedOfficialAppCatalog;

    public IntelSupportResolver(IInstalledOfficialAppCatalog installedOfficialAppCatalog)
    {
        _installedOfficialAppCatalog = installedOfficialAppCatalog;
    }

    public bool TryResolve(DriverIdentity identity, out OfficialSupportChannel channel)
    {
        if (!DriverIdentityVendorMatcher.IsIntel(identity))
        {
            channel = new OfficialSupportChannel();
            return false;
        }

        if (_installedOfficialAppCatalog.TryResolveInstalledApp(identity, out channel))
            return true;

        channel = new OfficialSupportChannel
        {
            Type = OfficialSupportChannelType.OfficialAppInstall,
            Target = DriverRules.IntelSupportAssistantUrl,
            DisplayName = "Intel Driver & Support Assistant",
            Description = "Официальный путь обновления для поддерживаемых Intel-устройств."
        };
        return true;
    }
}
