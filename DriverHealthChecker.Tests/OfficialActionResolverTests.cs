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
        Assert.Equal(DriverRules.IntelSupportAssistantUrl, action.Target);
    }

    [Fact]
    public void Resolve_NonIntelStorage_ReturnsWindowsUpdate()
    {
        var action = _resolver.Resolve("Samsung NVMe Controller", "Samsung", "Storage");

        Assert.Equal(OfficialActionKind.WindowsUpdate, action.Kind);
    }

    [Fact]
    public void Resolve_RealtekAudio_ReturnsSearch()
    {
        var action = _resolver.Resolve("Realtek(R) Audio", "Realtek", "AudioMain");

        Assert.Equal(OfficialActionKind.Search, action.Kind);
        Assert.Contains("official driver site", action.Target);
    }

    [Fact]
    public void Resolve_UnknownCategory_ReturnsMessageAction()
    {
        var action = _resolver.Resolve("Unknown Device", "Contoso", "UnknownCategory");

        Assert.Equal(OfficialActionKind.None, action.Kind);
        Assert.False(string.IsNullOrWhiteSpace(action.Message));
    }
}
