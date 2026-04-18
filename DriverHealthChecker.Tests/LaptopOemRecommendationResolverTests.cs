using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class LaptopOemRecommendationResolverTests
{
    private readonly ILaptopOemRecommendationResolver _resolver = new LaptopOemRecommendationResolver();

    [Theory]
    [InlineData("LENOVO", "ThinkPad T14", "Lenovo Vantage", DriverRules.LenovoVantageUrl)]
    [InlineData("ASUSTeK COMPUTER INC.", "ROG Strix", "MyASUS", DriverRules.AsusMyAsusUrl)]
    [InlineData("Hewlett-Packard", "EliteBook", "HP Support Assistant", DriverRules.HpSupportAssistantUrl)]
    [InlineData("Dell Inc.", "XPS 15", "Dell SupportAssist", DriverRules.DellSupportAssistUrl)]
    [InlineData("Acer", "Swift", "Acer Care Center", DriverRules.AcerCareCenterUrl)]
    [InlineData("Micro-Star International", "Stealth", "MSI Center", DriverRules.MsiCenterUrl)]
    [InlineData("HUAWEI", "MateBook", "Huawei PC Manager", DriverRules.HuaweiPcManagerUrl)]
    public void Resolve_KnownOem_ReturnsExpectedUrl(string manufacturer, string model, string expectedButton, string expectedUrl)
    {
        var action = _resolver.Resolve(manufacturer, model);

        Assert.Equal(OfficialActionKind.Url, action.Kind);
        Assert.Equal(expectedButton, action.ButtonText);
        Assert.Equal(expectedUrl, action.Target);
    }

    [Fact]
    public void Resolve_UnknownOem_ReturnsSafeMessage()
    {
        var action = _resolver.Resolve("Contoso", "Book 1");

        Assert.Equal(OfficialActionKind.None, action.Kind);
        Assert.Contains("OEM", action.ButtonText);
    }
}
