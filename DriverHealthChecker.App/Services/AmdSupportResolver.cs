namespace DriverHealthChecker.App;

internal sealed class AmdSupportResolver
{
    public bool TryResolve(DriverIdentity identity, out OfficialSupportChannel channel)
    {
        if (!DriverIdentityVendorMatcher.IsAmd(identity))
        {
            channel = new OfficialSupportChannel();
            return false;
        }

        channel = new OfficialSupportChannel
        {
            Type = OfficialSupportChannelType.DirectDriverPage,
            Target = DriverRules.AmdDriversUrl,
            DisplayName = "AMD Drivers and Support",
            Description = "Официальная страница драйверов AMD."
        };
        return true;
    }
}
