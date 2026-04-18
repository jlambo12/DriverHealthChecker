using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class OfficialActionResolverTests
{
    private readonly IOfficialActionResolver _resolver = new OfficialActionResolver();

    [Fact]
    public void Resolve_IntelNetwork_ReturnsIntelToolUrl()
    {
        var action = _resolver.Resolve("Intel(R) Wi-Fi 6E AX211", "Intel", "Network");

        Assert.Equal(OfficialActionKind.Url, action.Kind);
        Assert.Equal(DriverRules.IntelWirelessDriversUrl, action.Target);
    }

    [Fact]
    public void Resolve_NonIntelStorage_ReturnsSafeMessage()
    {
        var action = _resolver.Resolve("Samsung NVMe Controller", "Samsung", "Storage");

        Assert.Equal(OfficialActionKind.None, action.Kind);
        Assert.False(string.IsNullOrWhiteSpace(action.Message));
    }

    [Fact]
    public void Resolve_RealtekAudio_ReturnsRealtekOfficialUrl()
    {
        var action = _resolver.Resolve("Realtek(R) Audio", "Realtek", "AudioMain");

        Assert.Equal(OfficialActionKind.Url, action.Kind);
        Assert.Equal(DriverRules.RealtekDownloadsUrl, action.Target);
    }

    [Fact]
    public void Resolve_HuaweiLaptopAudioComponent_PrefersHuaweiPcManager()
    {
        var action = _resolver.Resolve("HWVE Audio Effects Component", "Realtek", "AudioMain", "HUAWEI", true);

        Assert.Equal(OfficialActionKind.Url, action.Kind);
        Assert.Equal(DriverRules.HuaweiPcManagerUrl, action.Target);
    }

    [Fact]
    public void Resolve_IntelBluetoothNetwork_ReturnsIntelBluetoothUrl()
    {
        var action = _resolver.Resolve("Intel Wireless Bluetooth", "Intel", "Network");

        Assert.Equal(OfficialActionKind.Url, action.Kind);
        Assert.Equal(DriverRules.IntelBluetoothDriversUrl, action.Target);
    }

    [Fact]
    public void Resolve_UnknownNetwork_ReturnsSafeMessage()
    {
        var action = _resolver.Resolve("Contoso Network Adapter", "Contoso", "Network");

        Assert.Equal(OfficialActionKind.None, action.Kind);
        Assert.Contains("OEM", action.Message);
    }
}
