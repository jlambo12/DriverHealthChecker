using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class OfficialSupportChannelResolverTests
{
    [Fact]
    public void Resolve_NvidiaWithInstalledApp_ReturnsInstalledOfficialApp()
    {
        var appCatalog = new StubInstalledOfficialAppCatalog(
            nvidiaChannel: BuildInstalledAppChannel("NVIDIA App", @"C:\Apps\NVIDIA App.exe"));
        var resolver = BuildResolver(appCatalog);

        var channel = resolver.Resolve(new DriverIdentity
        {
            Manufacturer = "NVIDIA",
            NormalizedManufacturer = "NVIDIA"
        });

        Assert.Equal(OfficialSupportChannelType.InstalledOfficialApp, channel.Type);
        Assert.Equal(@"C:\Apps\NVIDIA App.exe", channel.Target);
    }

    [Fact]
    public void Resolve_NvidiaWithoutInstalledApp_ReturnsOfficialAppInstall()
    {
        var resolver = BuildResolver(new StubInstalledOfficialAppCatalog());

        var channel = resolver.Resolve(new DriverIdentity
        {
            Manufacturer = "NVIDIA",
            NormalizedManufacturer = "NVIDIA"
        });

        Assert.Equal(OfficialSupportChannelType.OfficialAppInstall, channel.Type);
        Assert.Equal(DriverRules.NvidiaAppUrl, channel.Target);
    }

    [Fact]
    public void Resolve_IntelWithoutInstalledApp_ReturnsOfficialAppInstall()
    {
        var resolver = BuildResolver(new StubInstalledOfficialAppCatalog());

        var channel = resolver.Resolve(new DriverIdentity
        {
            Manufacturer = "Intel",
            NormalizedManufacturer = "INTEL"
        });

        Assert.Equal(OfficialSupportChannelType.OfficialAppInstall, channel.Type);
        Assert.Equal(DriverRules.IntelSupportAssistantUrl, channel.Target);
    }

    [Fact]
    public void Resolve_AmdByHardwareId_ReturnsDirectDriverPage()
    {
        var resolver = BuildResolver(new StubInstalledOfficialAppCatalog());

        var channel = resolver.Resolve(new DriverIdentity
        {
            HardwareIds = { @"PCI\VEN_1002&DEV_164E" }
        });

        Assert.Equal(OfficialSupportChannelType.DirectDriverPage, channel.Type);
        Assert.Equal(DriverRules.AmdDriversUrl, channel.Target);
    }

    [Fact]
    public void Resolve_UnknownVendor_ReturnsManualExplanation()
    {
        var resolver = BuildResolver(new StubInstalledOfficialAppCatalog());

        var channel = resolver.Resolve(new DriverIdentity
        {
            DisplayName = "Contoso Device",
            Manufacturer = "Contoso",
            NormalizedManufacturer = "CONTOSO"
        });

        Assert.Equal(OfficialSupportChannelType.ManualExplanation, channel.Type);
        Assert.Equal("Contoso Device", channel.DisplayName);
    }

    private static OfficialSupportChannelResolver BuildResolver(IInstalledOfficialAppCatalog installedOfficialAppCatalog)
    {
        return new OfficialSupportChannelResolver(
            new NvidiaSupportResolver(installedOfficialAppCatalog),
            new IntelSupportResolver(installedOfficialAppCatalog),
            new AmdSupportResolver(),
            new GenericSupportResolver());
    }

    private static OfficialSupportChannel BuildInstalledAppChannel(string displayName, string target)
    {
        return new OfficialSupportChannel
        {
            Type = OfficialSupportChannelType.InstalledOfficialApp,
            Target = target,
            IsInstalled = true,
            DisplayName = displayName,
            Description = "stub"
        };
    }

    private sealed class StubInstalledOfficialAppCatalog : IInstalledOfficialAppCatalog
    {
        private readonly OfficialSupportChannel? _nvidiaChannel;
        private readonly OfficialSupportChannel? _intelChannel;

        public StubInstalledOfficialAppCatalog(
            OfficialSupportChannel? nvidiaChannel = null,
            OfficialSupportChannel? intelChannel = null)
        {
            _nvidiaChannel = nvidiaChannel;
            _intelChannel = intelChannel;
        }

        public bool TryResolveInstalledApp(DriverIdentity identity, out OfficialSupportChannel channel)
        {
            if (DriverIdentityVendorMatcher.IsNvidia(identity) && _nvidiaChannel != null)
            {
                channel = _nvidiaChannel;
                return true;
            }

            if (DriverIdentityVendorMatcher.IsIntel(identity) && _intelChannel != null)
            {
                channel = _intelChannel;
                return true;
            }

            channel = new OfficialSupportChannel();
            return false;
        }
    }
}
