using System;
using System.Collections.Generic;
using System.IO;
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

    [Fact]
    public void Build_NormalizesDeviceAndManufacturerWithoutChangingFlow()
    {
        var classifier = new CapturingClassifier();
        var resolver = new CapturingActionResolver();
        var mapper = new DriverScanMapper(
            classifier,
            resolver,
            new StubSelectionService());

        var result = mapper.Build(
            [new ScannedDriverRecord
            {
                Name = "  Intel   Wi-Fi   ",
                Manufacturer = "  Intel   Corporation  ",
                Version = "1.0"
            }],
            profile: null);

        Assert.Single(result.SelectedDrivers);
        Assert.Equal("Intel Wi-Fi", classifier.ReceivedName);
        Assert.Equal("Intel Corporation", classifier.ReceivedManufacturer);
        Assert.Equal("Intel Wi-Fi", resolver.ReceivedName);
        Assert.Equal("Intel Corporation", resolver.ReceivedManufacturer);
        Assert.Equal("Intel Wi-Fi", result.SelectedDrivers[0].Name);
        Assert.Equal("Intel", result.SelectedDrivers[0].Manufacturer);
    }

    [Fact]
    public void Build_InvokesRegistryAndStoresVerificationResult()
    {
        var probeVerifier = new ProbeVendorDriverVerifier();
        var registry = new DriverVerifierRegistry([probeVerifier]);
        var mapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService(),
            registry);

        var result = mapper.Build(
            [new ScannedDriverRecord
            {
                Name = "Intel Wi-Fi",
                Manufacturer = "Intel",
                Version = "1.0.0",
                PnpDeviceId = @"PCI\VEN_8086&DEV_1234"
            }],
            profile: null);

        Assert.Single(result.SelectedDrivers);
        Assert.Single(result.VerificationObservations);
        Assert.Equal(1, probeVerifier.VerifyCallCount);
        Assert.Equal("Intel Wi-Fi", result.VerificationObservations[0].DriverName);
        Assert.Equal(DriverVerificationStatus.UpToDate, result.VerificationObservations[0].Result.Status);
        Assert.Equal(DriverHealthStatus.NeedsReview, result.VerificationObservations[0].LegacyStatus);
        Assert.Equal(DriverVerificationStatus.UpToDate, result.VerificationObservations[0].VerificationStatus);
        Assert.False(result.VerificationObservations[0].IsMatch);
    }

    [Fact]
    public void Build_VerificationFailureDoesNotBreakExistingFlow()
    {
        var registry = new DriverVerifierRegistry([new ThrowingVendorDriverVerifier()]);
        var mapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService(),
            registry);

        var result = mapper.Build(
            [new ScannedDriverRecord
            {
                Name = "Intel Wi-Fi",
                Manufacturer = "Intel",
                Version = "1.0.0",
                PnpDeviceId = @"PCI\VEN_8086&DEV_1234"
            }],
            profile: null);

        Assert.Single(result.SelectedDrivers);
        Assert.Empty(result.HiddenDrivers);
        Assert.Empty(result.VerificationObservations);
        Assert.Equal("Intel Tool", result.SelectedDrivers[0].ButtonText);
    }

    [Fact]
    public void Build_WhenLegacyAndVerificationStatusesMatch_StoresMatchObservation()
    {
        var probeVerifier = new ProbeVendorDriverVerifier
        {
            ResultStatus = DriverVerificationStatus.UnableToVerifyReliably
        };
        var registry = new DriverVerifierRegistry([probeVerifier]);
        var mapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService(),
            registry);

        var result = mapper.Build(
            [new ScannedDriverRecord
            {
                Name = "Intel Wi-Fi",
                Manufacturer = "Intel",
                Version = "1.0.0",
                PnpDeviceId = @"PCI\VEN_8086&DEV_1234"
            }],
            profile: null);

        var observation = Assert.Single(result.VerificationObservations);
        Assert.Equal(DriverHealthStatus.NeedsReview, observation.LegacyStatus);
        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, observation.VerificationStatus);
        Assert.True(observation.IsMatch);
        Assert.Equal(1, mapper.ValidationTotalCount);
        Assert.Equal(1, mapper.ValidationMatchCount);
        Assert.Equal(0, mapper.ValidationMismatchCount);
    }

    [Fact]
    public void Build_WhenLegacyAndVerificationStatusesMismatch_IncrementsMismatchCounter()
    {
        var probeVerifier = new ProbeVendorDriverVerifier
        {
            ResultStatus = DriverVerificationStatus.UpToDate
        };
        var registry = new DriverVerifierRegistry([probeVerifier]);
        var mapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService(),
            registry);

        var result = mapper.Build(
            [new ScannedDriverRecord
            {
                Name = "Intel Wi-Fi",
                Manufacturer = "Intel",
                Version = "1.0.0",
                PnpDeviceId = @"PCI\VEN_8086&DEV_1234"
            }],
            profile: null);

        var observation = Assert.Single(result.VerificationObservations);
        Assert.Equal(DriverHealthStatus.NeedsReview, observation.LegacyStatus);
        Assert.Equal(DriverVerificationStatus.UpToDate, observation.VerificationStatus);
        Assert.False(observation.IsMatch);
        Assert.Equal(1, mapper.ValidationTotalCount);
        Assert.Equal(0, mapper.ValidationMatchCount);
        Assert.Equal(1, mapper.ValidationMismatchCount);
    }

    [Fact]
    public void Build_LogsVerificationAggregate()
    {
        var probeVerifier = new ProbeVendorDriverVerifier
        {
            ResultStatus = DriverVerificationStatus.UnableToVerifyReliably
        };
        var registry = new DriverVerifierRegistry([probeVerifier]);
        var mapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService(),
            registry);

        const string message = "Verification aggregate. totalDrivers=2, withVerification=2, mismatches=0.";
        var beforeCount = CountLogOccurrences(message);

        mapper.Build(
            [
                new ScannedDriverRecord
                {
                    Name = "Intel Wi-Fi",
                    Manufacturer = "Intel",
                    Version = "1.0.0",
                    PnpDeviceId = @"PCI\VEN_8086&DEV_1234"
                },
                new ScannedDriverRecord
                {
                    Name = "Intel Bluetooth",
                    Manufacturer = "Intel",
                    Version = "1.0.0",
                    PnpDeviceId = @"PCI\VEN_8086&DEV_5678"
                }
            ],
            profile: null);

        Assert.Equal(beforeCount + 1, CountLogOccurrences(message));
    }

    [Fact]
    public void Build_LogsMismatchOnlyForMismatchCases()
    {
        var mismatchRegistry = new DriverVerifierRegistry(
            [new ProbeVendorDriverVerifier { ResultStatus = DriverVerificationStatus.UpToDate }]);
        var mismatchMapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService(),
            mismatchRegistry);

        const string mismatchMessage = "Verification mismatch. vendorId=8086, deviceId=BEEF, legacyStatus=NeedsReview, verificationStatus=UpToDate.";
        var mismatchBeforeCount = CountLogOccurrences(mismatchMessage);

        mismatchMapper.Build(
            [new ScannedDriverRecord
            {
                Name = "Intel Wi-Fi Mismatch",
                Manufacturer = "Intel",
                Version = "1.0.0",
                PnpDeviceId = @"PCI\VEN_8086&DEV_BEEF"
            }],
            profile: null);

        Assert.Equal(mismatchBeforeCount + 1, CountLogOccurrences(mismatchMessage));

        var matchRegistry = new DriverVerifierRegistry(
            [new ProbeVendorDriverVerifier { ResultStatus = DriverVerificationStatus.UnableToVerifyReliably }]);
        var matchMapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService(),
            matchRegistry);

        const string noMismatchMessage = "Verification mismatch. vendorId=8086, deviceId=CAFE, legacyStatus=NeedsReview, verificationStatus=UnableToVerifyReliably.";
        var noMismatchBeforeCount = CountLogOccurrences(noMismatchMessage);

        matchMapper.Build(
            [new ScannedDriverRecord
            {
                Name = "Intel Wi-Fi Match",
                Manufacturer = "Intel",
                Version = "1.0.0",
                PnpDeviceId = @"PCI\VEN_8086&DEV_CAFE"
            }],
            profile: null);

        Assert.Equal(noMismatchBeforeCount, CountLogOccurrences(noMismatchMessage));
    }

    [Fact]
    public void Build_WhenLoggerThrows_DoesNotBreakPipeline()
    {
        var probeVerifier = new ProbeVendorDriverVerifier
        {
            ResultStatus = DriverVerificationStatus.UpToDate
        };
        var registry = new DriverVerifierRegistry([probeVerifier]);
        var mapper = new DriverScanMapper(
            new StubClassifier(classify: true),
            new StubActionResolver(),
            new StubSelectionService(),
            registry);

        var logFilePath = GetLogFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

        using var logLock = new FileStream(logFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        var result = mapper.Build(
            [new ScannedDriverRecord
            {
                Name = "Intel Wi-Fi",
                Manufacturer = "Intel",
                Version = "1.0.0",
                PnpDeviceId = @"PCI\VEN_8086&DEV_1234"
            }],
            profile: null);

        Assert.Single(result.SelectedDrivers);
        Assert.Single(result.VerificationObservations);
        Assert.Equal("Intel Tool", result.SelectedDrivers[0].ButtonText);
    }

    private static int CountLogOccurrences(string message)
    {
        var logFilePath = GetLogFilePath();
        if (!File.Exists(logFilePath))
            return 0;

        var text = File.ReadAllText(logFilePath);
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(message, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += message.Length;
        }

        return count;
    }

    private static string GetLogFilePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DriverHealthChecker",
            "logs",
            "app.log");
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

    private sealed class CapturingClassifier : IDriverClassifier
    {
        public string ReceivedName { get; private set; } = string.Empty;
        public string? ReceivedManufacturer { get; private set; }

        public bool TryClassify(string name, string? manufacturer, out DriverCategory category, out string reason)
        {
            ReceivedName = name;
            ReceivedManufacturer = manufacturer;
            category = DriverCategory.Network;
            reason = "stub";
            return true;
        }
    }

    private sealed class CapturingActionResolver : IOfficialActionResolver
    {
        public string ReceivedName { get; private set; } = string.Empty;
        public string? ReceivedManufacturer { get; private set; }

        public OfficialAction Resolve(string name, string? manufacturer, DriverCategory category, string? oemManufacturer = null, bool isLaptop = false)
        {
            ReceivedName = name;
            ReceivedManufacturer = manufacturer;
            return OfficialAction.ForUrl("https://example.com", "Intel Tool", "stub");
        }
    }

    private sealed class StubSelectionService : IDriverSelectionService
    {
        public List<DriverItem> SelectBestDrivers(List<DriverItem> drivers) => drivers;
    }

    private sealed class ProbeVendorDriverVerifier : IVendorDriverVerifier
    {
        public int VerifyCallCount { get; private set; }
        public DriverVerificationStatus ResultStatus { get; init; } = DriverVerificationStatus.UpToDate;

        public bool CanHandle(DriverIdentity identity) => true;

        public DriverVerificationResult Verify(DriverIdentity identity)
        {
            VerifyCallCount++;

            return new DriverVerificationResult
            {
                Status = ResultStatus,
                LatestOfficialVersion = identity.InstalledVersion,
                VerificationSourceType = VerificationSourceType.OfficialApi,
                SourceDetails = "Probe verifier",
                VerificationTimestamp = System.DateTimeOffset.UtcNow
            };
        }
    }

    private sealed class ThrowingVendorDriverVerifier : IVendorDriverVerifier
    {
        public bool CanHandle(DriverIdentity identity) => true;

        public DriverVerificationResult Verify(DriverIdentity identity)
        {
            throw new System.InvalidOperationException("verification failed");
        }
    }

}
