using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverClassifierTests
{
    private readonly IDriverClassifier _classifier = new DriverClassifier();

    [Theory]
    [InlineData("NVIDIA GeForce RTX 4080", "NVIDIA", DriverCategory.Gpu)]
    [InlineData("Intel(R) Wi-Fi 6E AX211", "Intel", DriverCategory.Network)]
    [InlineData("Samsung NVMe Controller", "Samsung", DriverCategory.Storage)]
    [InlineData("Realtek(R) Audio", "Realtek", DriverCategory.AudioMain)]
    [InlineData("Focusrite USB Audio", "Focusrite", DriverCategory.AudioExternal)]
    [InlineData("Audio CoProcessor Device", "Contoso", DriverCategory.AudioExternal)]
    public void TryClassify_ShouldReturnExpectedCategory(string name, string manufacturer, DriverCategory expectedCategory)
    {
        var result = _classifier.TryClassify(name, manufacturer, out var category, out var reason);

        Assert.True(result);
        Assert.Equal<DriverCategory>(expectedCategory, category);
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
    public void TryClassify_BlacklistedDevice_ShouldIncludeMatchedBlacklistTermInReason()
    {
        var result = _classifier.TryClassify("WAN Miniport (SSTP)", "Microsoft", out _, out var reason);

        Assert.False(result);
        Assert.Contains("wan miniport", reason);
    }

    [Fact]
    public void TryClassify_UnknownDevice_ShouldReturnFalse()
    {
        var result = _classifier.TryClassify("Unknown Diagnostic Device", "Contoso", out _, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Qualcomm FastConnect 7800 Wi-Fi 7", "Qualcomm", DriverCategory.Network)] // term match
    [InlineData("Intel Wireless Bluetooth", "Intel", DriverCategory.Network)] // Intel heuristic
    [InlineData("Realtek PCIe GbE Family Controller", "Realtek", DriverCategory.Network)] // Realtek heuristic
    [InlineData("Generic PCI Storage Controller", "Contoso", DriverCategory.Storage)] // storage term
    [InlineData("Conexant HD Audio", "Conexant", DriverCategory.AudioMain)] // main audio term
    [InlineData("Audio CoProcessor Device", "Contoso", DriverCategory.AudioExternal)] // external generalized audio
    public void TryClassify_ShouldCoverHeuristicBranches(string name, string manufacturer, DriverCategory expectedCategory)
    {
        var result = _classifier.TryClassify(name, manufacturer, out var category, out _);

        Assert.True(result);
        Assert.Equal<DriverCategory>(expectedCategory, category);
    }

    [Theory]
    [InlineData("NVIDIA Audio Endpoint", "NVIDIA")] // blacklist should win over GPU
    [InlineData("USB Audio Device", "Contoso")] // explicit skip in external audio
    [InlineData("Realtek Audio Endpoint", "Realtek")] // blocked in external generalized audio and not main-audio positive term
    public void TryClassify_ShouldHandleNegativeBranches(string name, string manufacturer)
    {
        var result = _classifier.TryClassify(name, manufacturer, out var category, out _);

        Assert.False(result);
        Assert.Equal(DriverCategory.Unknown, category);
    }


    [Fact]
    public void TryClassify_AudioCoProcessor_ShouldNotBeBlacklistedAsProcessor()
    {
        var result = _classifier.TryClassify("Audio CoProcessor Device", "Contoso", out var category, out var reason);

        Assert.True(result);
        Assert.Equal(DriverCategory.AudioExternal, category);
        Assert.DoesNotContain("Скрыто:", reason);
    }

    [Fact]
    public void TryClassify_ShouldKeepRulePriority_BlacklistBeforeGpu()
    {
        var result = _classifier.TryClassify("NVIDIA GeForce Audio Controller", "NVIDIA", out var category, out _);

        Assert.False(result);
        Assert.Equal(DriverCategory.Unknown, category);
    }

    [Fact]
    public void TryClassify_ShouldKeepRulePriority_GpuBeforeNetworkStorageAndAudio()
    {
        var result = _classifier.TryClassify("NVIDIA GeForce Wireless Controller", "NVIDIA", out var category, out _);

        Assert.True(result);
        Assert.Equal(DriverCategory.Gpu, category);
    }

    [Fact]
    public void TryClassify_ShouldKeepRulePriority_NetworkBeforeStorage()
    {
        var result = _classifier.TryClassify("Intel Wireless Storage Adapter", "Intel", out var category, out _);

        Assert.True(result);
        Assert.Equal(DriverCategory.Network, category);
    }

    [Fact]
    public void TryClassify_ShouldKeepRulePriority_StorageBeforeAudioMainAndExternal()
    {
        var result = _classifier.TryClassify("PCI Storage Audio Controller", "Contoso", out var category, out _);

        Assert.True(result);
        Assert.Equal(DriverCategory.Storage, category);
    }

    [Fact]
    public void TryClassify_ShouldKeepRulePriority_AudioMainBeforeAudioExternal()
    {
        var result = _classifier.TryClassify("Conexant HD Audio USB", "Conexant", out var category, out _);

        Assert.True(result);
        Assert.Equal(DriverCategory.AudioMain, category);
    }
}
