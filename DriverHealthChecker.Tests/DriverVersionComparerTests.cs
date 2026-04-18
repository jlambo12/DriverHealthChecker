using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverVersionComparerTests
{
    private readonly IDriverVersionComparer _comparer = new DriverVersionComparer();

    [Theory]
    [InlineData("31.0.15.5123", "31.0.15.4000", 1)]
    [InlineData("1.0", "1.0.0", 0)]
    [InlineData("2.10", "2.2", 1)]
    [InlineData("", "1.0", -1)]
    [InlineData("abc", "0", 0)]
    public void Compare_HandlesTypicalVersions(string left, string right, int expectedSign)
    {
        var result = _comparer.Compare(left, right);

        Assert.Equal(expectedSign, Math.Sign(result));
    }
}
