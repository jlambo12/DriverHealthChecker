using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class DriverIdentityTokenExtractorTests
{
    [Fact]
    public void TryExtract_UsesPnpDeviceIdBeforeOtherSources()
    {
        var extractor = new DriverIdentityTokenExtractor();

        var found = extractor.TryExtract(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_1C82",
            HardwareIds = { @"PCI\VEN_8086&DEV_51F0" },
            CompatibleIds = { @"PCI\VEN_1002&DEV_9999" }
        }, out var tokens);

        Assert.True(found);
        Assert.Equal("10DE", tokens.VendorId);
        Assert.Equal("1C82", tokens.DeviceId);
    }

    [Fact]
    public void TryExtract_FallsBackToHardwareIdsWhenPnpDeviceIdIsMissing()
    {
        var extractor = new DriverIdentityTokenExtractor();

        var found = extractor.TryExtract(new DriverIdentity
        {
            HardwareIds = { @"PCI\VEN_10DE&DEV_2206" }
        }, out var tokens);

        Assert.True(found);
        Assert.Equal("10DE", tokens.VendorId);
        Assert.Equal("2206", tokens.DeviceId);
    }

    [Fact]
    public void TryExtract_RejectsLooseSubstringMatching()
    {
        var extractor = new DriverIdentityTokenExtractor();

        var found = extractor.TryExtract(new DriverIdentity
        {
            PnpDeviceId = @"PCI\XVEN_10DE&XDEV_2206"
        }, out var tokens);

        Assert.False(found);
        Assert.Equal(string.Empty, tokens.VendorId);
        Assert.Equal(string.Empty, tokens.DeviceId);
    }
}
