using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class WmiDriverScannerTests
{
    [Fact]
    public void ScanSignedDrivers_PlatformSmokeTest_ReturnsOperationResultContract()
    {
        var scanner = new WmiDriverScanner();

        var result = scanner.ScanSignedDrivers();

        Assert.NotNull(result);
        if (result.IsSuccess)
        {
            Assert.NotNull(result.Value);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }
    }
}
