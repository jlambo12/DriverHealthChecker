using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class NvidiaStubVersionSourceTests
{
    [Fact]
    public void TryGetLatestVersion_KnownDevice_ReturnsConfiguredVersion()
    {
        var source = new NvidiaStubVersionSource();

        var found = source.TryGetLatestVersion("1C82", out var latestVersion);

        Assert.True(found);
        Assert.Equal("551.86", latestVersion);
        Assert.Equal("NVIDIA official dataset (stub)", source.SourceDetails);
    }

    [Fact]
    public void TryGetLatestVersion_UnknownDevice_ReturnsFalse()
    {
        var source = new NvidiaStubVersionSource();

        var found = source.TryGetLatestVersion("9999", out var latestVersion);

        Assert.False(found);
        Assert.Equal(string.Empty, latestVersion);
    }
}
