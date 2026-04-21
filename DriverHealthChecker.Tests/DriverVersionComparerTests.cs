using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class DriverVersionComparerTests
{
    [Fact]
    public void Compare_IsNumericAwareAcrossSegments()
    {
        var comparer = new DriverVersionComparer();

        Assert.True(comparer.Compare("551.10", "552.12") < 0);
        Assert.True(comparer.Compare("552.12.1", "552.12") > 0);
        Assert.Equal(0, comparer.Compare("551.86", "551.86"));
    }
}
