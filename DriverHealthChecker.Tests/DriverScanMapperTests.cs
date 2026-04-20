using System.Collections.Generic;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverScanMapperTests
{
    [Fact]
    public void Build_ClassifiedRecord_ProducesSelectedDriverWithAction()
    {
        var mapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService());

        var result = mapper.Build(
            [new ScannedDriverRecord { Name = "Intel Wi-Fi", Manufacturer = "Intel", Version = "1.0", RawDate = "20260101000000.000000+000" }],
            profile: null);

        Assert.Single(result.SelectedDrivers);
        Assert.Empty(result.HiddenDrivers);
        Assert.Equal("Intel Tool", result.SelectedDrivers[0].ButtonText);
    }

    [Fact]
    public void Build_UnclassifiedRecord_ProducesHiddenDriver()
    {
        var mapper = new DriverScanMapper(
            new StubClassifier(classify: false),
            new StubActionResolver(),
            new StubSelectionService());

        var result = mapper.Build([new ScannedDriverRecord { Name = "Noise Device", Manufacturer = "Unknown" }], profile: null);

        Assert.Empty(result.SelectedDrivers);
        Assert.Single(result.HiddenDrivers);
        Assert.Equal("Скрыт", result.HiddenDrivers[0].Status);
    }

    private sealed class StubClassifier : IDriverClassifier
    {
        private readonly bool _classify;

        public StubClassifier(bool classify) => _classify = classify;

        public bool TryClassify(string name, string? manufacturer, out DriverCategory category, out string reason)
        {
            if (_classify)
            {
                category = DriverCategory.Network;
                reason = "stub";
                return true;
            }

            category = DriverCategory.Unknown;
            reason = "noise";
            return false;
        }
    }

    private sealed class StubActionResolver : IOfficialActionResolver
    {
        public OfficialAction Resolve(string name, string? manufacturer, DriverCategory category, string? oemManufacturer = null, bool isLaptop = false)
        {
            return OfficialAction.ForUrl("https://example.com", "Intel Tool", "stub");
        }
    }

    private sealed class StubSelectionService : IDriverSelectionService
    {
        public List<DriverItem> SelectBestDrivers(List<DriverItem> drivers) => drivers;
    }
}
