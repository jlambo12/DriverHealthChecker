using System.Linq;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class FixtureTemplateBuilderTests
{
    [Fact]
    public void Build_ShouldSkipDeviceRecommendation_AndMapHiddenAsUnclassified()
    {
        var builder = new FixtureTemplateBuilder();

        var input = new[]
        {
            new DriverItem { Name = "NVIDIA GeForce RTX 4070", Manufacturer = "NVIDIA", Category = "GPU" },
            new DriverItem { Name = "NVIDIA GeForce RTX 4070", Manufacturer = "NVIDIA", Category = "GPU" },
            new DriverItem { Name = "PCI Express Root Port", Manufacturer = "Intel", Category = "HiddenSystem" },
            new DriverItem { Name = "Оптимизация ноутбука (рекомендация)", Manufacturer = "Lenovo", Category = "DeviceRecommendation" }
        };

        var result = builder.Build(input).ToList();

        Assert.Equal(2, result.Count);

        var gpu = result.Single(r => r.Name.Contains("GeForce"));
        Assert.True(gpu.ShouldClassify);
        Assert.Equal("GPU", gpu.ExpectedCategory);

        var hidden = result.Single(r => r.Name.Contains("Root Port"));
        Assert.False(hidden.ShouldClassify);
        Assert.True(string.IsNullOrWhiteSpace(hidden.ExpectedCategory));
    }
}
