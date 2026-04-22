using System.Collections.Generic;
using System.Linq;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverPipelineIntegrationTests
{
    [Fact]
    public void ScanMapComparePipeline_OnRescan_MarksOnlyChangedDriverAsRecentlyUpdated()
    {
        var mapper = new DriverScanMapper(
            new DriverClassifier(),
            new OfficialActionResolver(),
            new DriverSelectionService(new DriverVersionComparer()));

        var comparison = new DriverComparisonService();

        var records = new List<ScannedDriverRecord>
        {
            new() { Name = "Intel(R) Wi-Fi 6E AX211", Manufacturer = "Intel", Version = "2.0", RawDate = "2026-01-01" },
            new() { Name = "Realtek(R) Audio", Manufacturer = "Realtek", Version = "1.0", RawDate = "2025-01-01" }
        };

        var mapped = mapper.Build(records, profile: null).SelectedDrivers;

        var previousDrivers = new List<DriverItem>
        {
            new() { Name = "Intel(R) Wi-Fi 6E AX211", Category = "Network", Version = "1.0", Date = "2026-01-01" },
            new() { Name = "Realtek(R) Audio", Category = "AudioMain", Version = "1.0", Date = "2025-01-01" }
        };
        var previousSnapshot = comparison.BuildSnapshot(previousDrivers);

        comparison.ApplyComparison(mapped, isRescan: true, previousSnapshot);

        var wifi = mapped.Single(d => d.CategoryKind == DriverCategory.Network);
        var audio = mapped.Single(d => d.CategoryKind == DriverCategory.AudioMain);

        Assert.Equal(DriverHealthStatus.RecentlyUpdated, wifi.StatusKind);
        Assert.NotEqual(DriverHealthStatus.RecentlyUpdated, audio.StatusKind);
    }
}
