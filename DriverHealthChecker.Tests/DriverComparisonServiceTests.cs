using System.Collections.Generic;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverComparisonServiceTests
{
    private readonly IDriverComparisonService _service = new DriverComparisonService(new DriverStatusEvaluator());

    [Fact]
    public void ApplyComparison_UpdatedDriver_MarkedAsRecentlyUpdated()
    {
        var drivers = new List<DriverItem>
        {
            new() { Name = "Intel Wi-Fi", Category = "Network", Version = "2.0", Date = "2026-01-01" }
        };

        var previous = new Dictionary<string, DriverSnapshot>
        {
            ["Network|Intel Wi-Fi"] = new() { Version = "1.0", Date = "2026-01-01" }
        };

        _service.ApplyComparison(drivers, isRescan: true, previous);

        Assert.Equal(DriverHealthStatus.RecentlyUpdated, drivers[0].StatusKind);
    }

    [Fact]
    public void ApplyComparison_UnchangedDriver_NotMarkedAsRecentlyUpdated()
    {
        var drivers = new List<DriverItem>
        {
            new() { Name = "Intel Wi-Fi", Category = "Network", Version = "2.0", Date = "2026-01-01" }
        };

        var previous = new Dictionary<string, DriverSnapshot>
        {
            ["Network|Intel Wi-Fi"] = new() { Version = "2.0", Date = "2026-01-01" }
        };

        _service.ApplyComparison(drivers, isRescan: true, previous);

        Assert.NotEqual(DriverHealthStatus.RecentlyUpdated, drivers[0].StatusKind);
    }

    [Fact]
    public void ApplyComparison_NewDriver_NotMarkedAsRecentlyUpdated()
    {
        var drivers = new List<DriverItem>
        {
            new() { Name = "New GPU", Category = "GPU", Version = "1.0", Date = "2026-01-01" }
        };

        _service.ApplyComparison(drivers, isRescan: true, new Dictionary<string, DriverSnapshot>());

        Assert.NotEqual(DriverHealthStatus.RecentlyUpdated, drivers[0].StatusKind);
    }

    [Fact]
    public void ApplyComparison_MissingPreviousEntry_NotMarkedAsRecentlyUpdated()
    {
        var drivers = new List<DriverItem>
        {
            new() { Name = "Intel BT", Category = "Network", Version = "1.0", Date = "2026-01-01" }
        };

        var previous = new Dictionary<string, DriverSnapshot>
        {
            ["Network|Different Device"] = new() { Version = "1.0", Date = "2026-01-01" }
        };

        _service.ApplyComparison(drivers, isRescan: true, previous);

        Assert.NotEqual(DriverHealthStatus.RecentlyUpdated, drivers[0].StatusKind);
    }

    [Fact]
    public void ApplyComparison_DeviceRecommendation_AlwaysMarkedAsRecommendation()
    {
        var drivers = new List<DriverItem>
        {
            new() { Name = "OEM Recommendation", Category = "DeviceRecommendation", Version = "-", Date = "-" }
        };

        _service.ApplyComparison(drivers, isRescan: true, new Dictionary<string, DriverSnapshot>());

        Assert.Equal(DriverHealthStatus.Recommendation, drivers[0].StatusKind);
    }

    [Fact]
    public void BuildSnapshot_UsesCategoryAndNameAsStableKey()
    {
        var drivers = new List<DriverItem>
        {
            new() { Name = "Intel Wi-Fi", Category = "Network", Version = "1.2.3", Date = "2026-01-01" }
        };

        var snapshot = _service.BuildSnapshot(drivers);

        Assert.True(snapshot.ContainsKey("Network|Intel Wi-Fi"));
        Assert.Equal("1.2.3", snapshot["Network|Intel Wi-Fi"].Version);
    }
}
