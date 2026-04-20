namespace DriverHealthChecker.App;

internal sealed class NvidiaSupportResolver
{
    private readonly IInstalledOfficialAppCatalog _installedOfficialAppCatalog;

    public NvidiaSupportResolver(IInstalledOfficialAppCatalog installedOfficialAppCatalog)
    {
        _installedOfficialAppCatalog = installedOfficialAppCatalog;
    }

    public bool TryResolve(DriverIdentity identity, out OfficialSupportChannel channel)
    {
        if (!DriverIdentityVendorMatcher.IsNvidia(identity))
        {
            channel = new OfficialSupportChannel();
            return false;
        }

        if (_installedOfficialAppCatalog.TryResolveInstalledApp(identity, out channel))
            return true;

        channel = new OfficialSupportChannel
        {
            Type = OfficialSupportChannelType.OfficialAppInstall,
            Target = DriverRules.NvidiaAppUrl,
            DisplayName = "NVIDIA App",
            Description = "Официальный путь обновления для поддерживаемых NVIDIA-устройств."
        };
        return true;
    }
}
