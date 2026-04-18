using System.Collections.Generic;
using System.Linq;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverSelectionServiceTests
{
    private readonly IDriverSelectionService _service = new DriverSelectionService(new DriverVersionComparer());

    [Fact]
    public void SelectBestDrivers_UsesNumericVersionComparison_WhenDatesEqual()
    {
        var input = new List<DriverItem>
        {
            new() { Name = "Intel Wi-Fi", Category = "Network", Version = "2.10", Date = "2026-01-01" },
            new() { Name = "Intel Wi-Fi", Category = "Network", Version = "2.2", Date = "2026-01-01" }
        };

        var selected = _service.SelectBestDrivers(input);

        Assert.Single(selected);
        Assert.Equal("2.10", selected.Single().Version);
    }
}
