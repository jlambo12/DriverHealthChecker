using System.Collections.Generic;
using System.Threading.Tasks;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task RunScanAsync_MapsVerificationDataToDriverItem()
    {
        var driver = CreateDriverItem();
        var observation = new DriverVerificationObservation
        {
            DriverKey = "Network|Intel Wi-Fi",
            DriverName = "Intel Wi-Fi",
            Manufacturer = "Intel",
            VerificationStatus = DriverVerificationStatus.UpdateAvailable,
            IsMatch = false,
            Result = new DriverVerificationResult
            {
                Status = DriverVerificationStatus.UpdateAvailable,
                VerificationSourceType = VerificationSourceType.OfficialApi,
                SourceDetails = "Test verification"
            }
        };

        var viewModel = CreateViewModel(driver, [observation]);

        var result = await viewModel.RunScanAsync(isRescan: false);

        Assert.True(result.IsSuccess);
        var mappedDriver = Assert.Single(viewModel.ApplyFilters(new DriverFilterState()));
        Assert.Equal(DriverVerificationStatus.UpdateAvailable, mappedDriver.VerificationStatus);
        Assert.False(mappedDriver.VerificationIsMatch);
    }

    [Fact]
    public async Task RunScanAsync_WhenVerificationMatches_PreservesMatchFlag()
    {
        var driver = CreateDriverItem();
        var observation = new DriverVerificationObservation
        {
            DriverKey = "Network|Intel Wi-Fi",
            DriverName = "Intel Wi-Fi",
            Manufacturer = "Intel",
            VerificationStatus = DriverVerificationStatus.UnableToVerifyReliably,
            IsMatch = true,
            Result = new DriverVerificationResult
            {
                Status = DriverVerificationStatus.UnableToVerifyReliably,
                VerificationSourceType = VerificationSourceType.OfficialApi,
                SourceDetails = "Test verification"
            }
        };

        var viewModel = CreateViewModel(driver, [observation]);

        var result = await viewModel.RunScanAsync(isRescan: false);

        Assert.True(result.IsSuccess);
        var mappedDriver = Assert.Single(viewModel.ApplyFilters(new DriverFilterState()));
        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, mappedDriver.VerificationStatus);
        Assert.True(mappedDriver.VerificationIsMatch);
    }

    [Fact]
    public async Task RunScanAsync_WhenObservationMissing_DoesNotBreakAndKeepsVerificationFieldsNull()
    {
        var driver = CreateDriverItem();
        var viewModel = CreateViewModel(driver, []);

        var result = await viewModel.RunScanAsync(isRescan: false);

        Assert.True(result.IsSuccess);
        var mappedDriver = Assert.Single(viewModel.ApplyFilters(new DriverFilterState()));
        Assert.Null(mappedDriver.VerificationStatus);
        Assert.Null(mappedDriver.VerificationIsMatch);
    }

    private static DriverItem CreateDriverItem(DriverHealthStatus statusKind = DriverHealthStatus.NeedsReview)
    {
        return new DriverItem
        {
            Name = "Intel Wi-Fi",
            Manufacturer = "Intel",
            Version = "1.0.0",
            Date = "2024-01-01",
            CategoryKind = DriverCategory.Network,
            StatusKind = statusKind
        };
    }

    private static MainWindowViewModel CreateViewModel(
        DriverItem driver,
        IReadOnlyList<DriverVerificationObservation> observations)
    {
        return new MainWindowViewModel(
            new StubDeviceProfileDetector(),
            new StubScanReportWriter(),
            new StubDriverComparisonService(),
            new StubWmiDriverScanner(),
            new StubDriverScanMapper(driver, observations),
            new StubDriverFilteringService(),
            new StubDriverPresentationService());
    }

    private sealed class StubDeviceProfileDetector : IDeviceProfileDetector
    {
        public DeviceProfile? TryGetDeviceProfile() => null;
    }

    private sealed class StubScanReportWriter : IScanReportWriter
    {
        public string? TryWrite(IReadOnlyCollection<DriverItem> drivers, bool isRescan, string? deviceKind = null) => null;
    }

    private sealed class StubDriverComparisonService : IDriverComparisonService
    {
        public void ApplyComparison(List<DriverItem> currentDrivers, bool isRescan, IReadOnlyDictionary<string, DriverSnapshot> previousSnapshot)
        {
        }

        public Dictionary<string, DriverSnapshot> BuildSnapshot(List<DriverItem> currentDrivers)
        {
            return new Dictionary<string, DriverSnapshot>();
        }
    }

    private sealed class StubWmiDriverScanner : IWmiDriverScanner
    {
        public OperationResult<List<ScannedDriverRecord>> ScanSignedDrivers()
        {
            return OperationResult<List<ScannedDriverRecord>>.Success(new List<ScannedDriverRecord>());
        }
    }

    private sealed class StubDriverScanMapper : IDriverScanMapper
    {
        private readonly DriverItem _driver;
        private readonly IReadOnlyList<DriverVerificationObservation> _observations;

        public StubDriverScanMapper(DriverItem driver, IReadOnlyList<DriverVerificationObservation> observations)
        {
            _driver = driver;
            _observations = observations;
        }

        public DriverScanBuildResult Build(IReadOnlyList<ScannedDriverRecord> records, DeviceProfile? profile)
        {
            return new DriverScanBuildResult
            {
                SelectedDrivers = new List<DriverItem> { _driver },
                VerificationObservations = new List<DriverVerificationObservation>(_observations)
            };
        }
    }

    private sealed class StubDriverFilteringService : IDriverFilteringService
    {
        public List<string> BuildCategoryItems(IReadOnlyCollection<DriverItem> currentDrivers, IReadOnlyCollection<DriverItem> hiddenDrivers, bool showHidden)
        {
            return new List<string> { "Все" };
        }

        public List<DriverItem> ApplyFilters(IReadOnlyCollection<DriverItem> currentDrivers, IReadOnlyCollection<DriverItem> hiddenDrivers, DriverFilterState filterState)
        {
            return new List<DriverItem>(currentDrivers);
        }
    }

    private sealed class StubDriverPresentationService : IDriverPresentationService
    {
        public int GetCategoryOrder(DriverCategory category) => 0;

        public DriverItem? BuildDeviceRecommendationItem() => null;
    }
}
