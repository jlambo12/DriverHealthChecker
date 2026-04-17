using System;
using System.IO;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class LocalAppValidatorTests
{
    private readonly ILocalAppValidator _validator = new LocalAppValidator();

    [Fact]
    public void Exists_WhenFilePresent_ReturnsTrue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"dhc-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempFile, "ok");

        try
        {
            Assert.True(_validator.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:\\non-existent\\app.exe")]
    public void Exists_WhenInvalidOrMissing_ReturnsFalse(string path)
    {
        Assert.False(_validator.Exists(path));
    }
}
