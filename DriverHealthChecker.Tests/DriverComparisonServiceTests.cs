using System.Collections.Generic;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverComparisonServiceTests
{
    private readonly IDriverComparisonService _service = new DriverComparisonService();

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

    [Theory]
    [InlineData(DriverVerificationStatus.UpToDate, DriverHealthStatus.UpToDate)]
    [InlineData(DriverVerificationStatus.UpdateAvailable, DriverHealthStatus.NeedsAttention)]
    [InlineData(DriverVerificationStatus.UnableToVerifyReliably, DriverHealthStatus.NeedsReview)]
    public void ApplyComparison_VerificationDrivesStatus(
        DriverVerificationStatus verificationStatus,
        DriverHealthStatus expectedStatus)
    {
        var service = new DriverComparisonService();
        var drivers = new List<DriverItem>
        {
            new()
            {
                Name = "Intel Wi-Fi",
                Category = "Network",
                Version = "1.0",
                Date = "2026-01-01",
                VerificationStatus = verificationStatus,
                StatusKind = DriverHealthStatus.Hidden
            }
        };

        service.ApplyComparison(drivers, isRescan: false, new Dictionary<string, DriverSnapshot>());

        Assert.Equal(expectedStatus, drivers[0].StatusKind);
    }

    [Fact]
    public void ApplyComparison_WhenVerificationMissing_UsesExplicitSafeDefault()
    {
        var service = new DriverComparisonService();
        var drivers = new List<DriverItem>
        {
            new()
            {
                Name = "Intel Wi-Fi",
                Category = "Network",
                Version = "1.0",
                Date = "2026-01-01"
            }
        };

        service.ApplyComparison(drivers, isRescan: false, new Dictionary<string, DriverSnapshot>());

        Assert.Equal(DriverHealthStatus.NeedsReview, drivers[0].StatusKind);
    }

    [Fact]
    public void ApplyComparison_WhenVerificationValueIsInvalid_UsesExplicitSafeDefault()
    {
        var service = new DriverComparisonService();
        var drivers = new List<DriverItem>
        {
            new()
            {
                Name = "Intel Wi-Fi",
                Category = "Network",
                Version = "1.0",
                Date = "2026-01-01",
                VerificationStatus = (DriverVerificationStatus)999
            }
        };

        service.ApplyComparison(drivers, isRescan: false, new Dictionary<string, DriverSnapshot>());

        Assert.Equal(DriverHealthStatus.NeedsReview, drivers[0].StatusKind);
    }

    [Fact]
    public void ApplyComparison_WhenUsingDefaultConstructor_UsesVerificationByDefault()
    {
        var service = new DriverComparisonService();
        var drivers = new List<DriverItem>
        {
            new()
            {
                Name = "Intel Wi-Fi",
                Category = "Network",
                Version = "1.0",
                Date = "2026-01-01",
                VerificationStatus = DriverVerificationStatus.UpToDate
            }
        };

        service.ApplyComparison(drivers, isRescan: false, new Dictionary<string, DriverSnapshot>());

        Assert.Equal(DriverHealthStatus.UpToDate, drivers[0].StatusKind);
    }

    [Fact]
    public void ApplyComparison_TracksAggregateDecisionCounters()
    {
        var service = new DriverComparisonService();
        var drivers = new List<DriverItem>
        {
            new()
            {
                Name = "Driver 1",
                Category = "Network",
                Version = "1.0",
                Date = "2026-01-01",
                VerificationStatus = DriverVerificationStatus.UpToDate
            },
            new()
            {
                Name = "Driver 2",
                Category = "Network",
                Version = "1.0",
                Date = "2026-01-01"
            },
            new()
            {
                Name = "Driver 3",
                Category = "DeviceRecommendation",
                Version = "-",
                Date = "-"
            }
        };

        service.ApplyComparison(drivers, isRescan: false, new Dictionary<string, DriverSnapshot>());

        Assert.Equal(3, service.LastDecisionTotalCount);
        Assert.Equal(1, service.LastDecisionUsedVerificationCount);
        Assert.Equal(1, service.LastDecisionDefaultCount);
    }

    [Fact]
    public void ApplyComparison_PreservesExistingBehaviorForRescanRecommendationAndVerification()
    {
        var service = new DriverComparisonService();
        var drivers = new List<DriverItem>
        {
            new()
            {
                Name = "Intel Wi-Fi",
                Category = "Network",
                Version = "2.0",
                Date = "2026-01-01",
                VerificationStatus = DriverVerificationStatus.UpToDate
            },
            new()
            {
                Name = "Intel Bluetooth",
                Category = "Network",
                Version = "1.0",
                Date = "2026-01-01",
                VerificationStatus = DriverVerificationStatus.UpdateAvailable
            },
            new()
            {
                Name = "OEM Recommendation",
                Category = "DeviceRecommendation",
                Version = "-",
                Date = "-",
                VerificationStatus = DriverVerificationStatus.UpToDate
            }
        };

        var previous = new Dictionary<string, DriverSnapshot>
        {
            ["Network|Intel Wi-Fi"] = new() { Version = "1.0", Date = "2026-01-01" }
        };

        service.ApplyComparison(drivers, isRescan: true, previous);

        Assert.Equal(DriverHealthStatus.RecentlyUpdated, drivers[0].StatusKind);
        Assert.Equal(DriverHealthStatus.NeedsAttention, drivers[1].StatusKind);
        Assert.Equal(DriverHealthStatus.Recommendation, drivers[2].StatusKind);
    }
}
