using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class OnlineTargetValidatorTests
{
    private readonly IOnlineTargetValidator _validator = new OnlineTargetValidator();

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?q=1")]
    public void IsValidUrl_ForHttpAndHttps_ReturnsTrue(string url)
    {
        Assert.True(_validator.IsValidUrl(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///c:/temp/a.txt")]
    [InlineData("not-a-url")]
    public void IsValidUrl_ForInvalidTargets_ReturnsFalse(string url)
    {
        Assert.False(_validator.IsValidUrl(url));
    }
}
