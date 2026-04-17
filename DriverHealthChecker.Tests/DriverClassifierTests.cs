using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverClassifierTests
{
    private readonly IDriverClassifier _classifier = new DriverClassifier();

    [Theory]
    [InlineData("NVIDIA GeForce RTX 4080", "NVIDIA", "GPU")]
    [InlineData("Intel(R) Wi-Fi 6E AX211", "Intel", "Network")]
    [InlineData("Samsung NVMe Controller", "Samsung", "Storage")]
    [InlineData("Realtek(R) Audio", "Realtek", "AudioMain")]
    [InlineData("Focusrite USB Audio", "Focusrite", "AudioExternal")]
    public void TryClassify_ShouldReturnExpectedCategory(string name, string manufacturer, string expectedCategory)
    {
        var result = _classifier.TryClassify(name, manufacturer, out var category, out var reason);

        Assert.True(result);
        Assert.Equal(expectedCategory, category);
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Theory]
    [InlineData("NVIDIA Virtual Audio Device", "NVIDIA")]
    [InlineData("WAN Miniport (SSTP)", "Microsoft")]
    [InlineData("PCI Express Root Port", "Intel")]
    public void TryClassify_ShouldIgnoreBlacklistedItems(string name, string manufacturer)
    {
        var result = _classifier.TryClassify(name, manufacturer, out _, out var reason);

        Assert.False(result);
        Assert.StartsWith("Скрыто:", reason);
    }

    [Fact]
    public void TryClassify_UnknownDevice_ShouldReturnFalse()
    {
        var result = _classifier.TryClassify("Unknown Diagnostic Device", "Contoso", out _, out _);

        Assert.False(result);
    }
}
