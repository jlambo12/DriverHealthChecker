using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class NvidiaDriverVerifierTests
{
    [Fact]
    public void Verify_UsesInjectedStubSource()
    {
        var source = new FakeNvidiaVersionSource("600.10", "Injected NVIDIA source");
        var verifier = new NvidiaDriverVerifier(CreateVerificationFlow(), source);

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_2206",
            InstalledVersion = "599.10"
        });

        Assert.Equal(DriverVerificationStatus.UpdateAvailable, result.Status);
        Assert.Equal("600.10", result.LatestOfficialVersion);
        Assert.Equal("Injected NVIDIA source", result.SourceDetails);
        Assert.Equal("2206", source.RequestedDeviceId);
    }

    [Fact]
    public void Verify_KnownDeviceAndSameVersion_ReturnsUpToDate()
    {
        var verifier = new NvidiaDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_1C82",
            InstalledVersion = "551.86"
        });

        Assert.Equal(DriverVerificationStatus.UpToDate, result.Status);
        Assert.Equal("551.86", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Equal("NVIDIA official dataset (stub)", result.SourceDetails);
        Assert.Contains("deviceId 1C82", result.EvidenceSummary);
    }

    [Fact]
    public void Verify_KnownDeviceAndOlderVersion_ReturnsUpdateAvailable()
    {
        var verifier = new NvidiaDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_2206",
            InstalledVersion = "551.10"
        });

        Assert.Equal(DriverVerificationStatus.UpdateAvailable, result.Status);
        Assert.Equal("552.12", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Equal("NVIDIA official dataset (stub)", result.SourceDetails);
        Assert.Contains("comparison=-1", result.EvidenceSummary);
    }

    [Fact]
    public void Verify_NvidiaVendorButMissingDeviceInDataset_ReturnsDatasetSpecificFailure()
    {
        var verifier = new NvidiaDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_9999",
            InstalledVersion = "551.10"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.DeviceNotInOfficialDataset, result.FailureReasonType);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Contains("9999", result.FailureReason);
    }

    [Fact]
    public void Verify_MissingInstalledVersion_ReturnsUnableToVerifyReliably()
    {
        var verifier = new NvidiaDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_2206"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.VerificationFailed, result.FailureReasonType);
        Assert.Equal("552.12", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Contains("installed version is unavailable", result.EvidenceSummary);
    }

    [Fact]
    public void Verify_DoesNotUseLooseSubstringTokenMatching()
    {
        var verifier = new NvidiaDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\XVEN_10DE&XDEV_2206",
            InstalledVersion = "551.10"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.VerificationFailed, result.FailureReasonType);
        Assert.Equal(VerificationSourceType.Unknown, result.VerificationSourceType);
        Assert.Contains("Could not extract", result.FailureReason);
    }

    private static VendorDriverVerificationFlow CreateVerificationFlow()
    {
        return new VendorDriverVerificationFlow(
            new DriverIdentityTokenExtractor(),
            new DriverVersionComparer());
    }

    private sealed class FakeNvidiaVersionSource : IVendorVersionSource
    {
        private readonly string _latestVersion;

        public FakeNvidiaVersionSource(string latestVersion, string sourceDetails)
        {
            _latestVersion = latestVersion;
            SourceDetails = sourceDetails;
        }

        public string SourceDetails { get; }

        public string? RequestedDeviceId { get; private set; }

        public bool TryGetLatestVersion(string deviceId, out string latestOfficialVersion)
        {
            RequestedDeviceId = deviceId;
            latestOfficialVersion = _latestVersion;
            return true;
        }
    }
}
