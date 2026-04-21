using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class DriverVerifierRegistryTests
{
    [Fact]
    public void Should_Use_Injected_Verifiers()
    {
        var registry = new DriverVerifierRegistry(
        [
            new ProbeVendorDriverVerifier()
        ]);

        var result = registry.Verify(new DriverIdentity());

        Assert.Equal(DriverVerificationStatus.UpToDate, result.Status);
        Assert.Equal("probe", result.SourceDetails);
        Assert.Equal("1.2.3", result.LatestOfficialVersion);
    }

    [Fact]
    public void Should_Route_To_Nvidia_Verifier()
    {
        var registry = new DriverVerifierRegistry();

        var result = registry.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_1C82",
            InstalledVersion = "551.86"
        });

        Assert.Equal(DriverVerificationStatus.UpToDate, result.Status);
        Assert.Equal("NVIDIA official dataset (stub)", result.SourceDetails);
        Assert.Equal("551.86", result.LatestOfficialVersion);
    }

    [Fact]
    public void Should_Route_To_Intel_Verifier()
    {
        var registry = new DriverVerifierRegistry();

        var result = registry.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_8086&DEV_1234",
            InstalledVersion = "1.0.0"
        });

        Assert.Equal(DriverVerificationStatus.UpToDate, result.Status);
        Assert.Equal("Intel official dataset (stub)", result.SourceDetails);
        Assert.Equal("1.0.0", result.LatestOfficialVersion);
    }

    [Fact]
    public void Should_Route_To_Amd_Verifier()
    {
        var registry = new DriverVerifierRegistry();

        var result = registry.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_1002&DEV_ABCD",
            InstalledVersion = "10.0.1"
        });

        Assert.Equal(DriverVerificationStatus.UpToDate, result.Status);
        Assert.Equal("AMD official dataset (stub)", result.SourceDetails);
        Assert.Equal("10.0.1", result.LatestOfficialVersion);
    }

    [Fact]
    public void Should_Return_NoMatchingVendor_When_Unknown()
    {
        var registry = new DriverVerifierRegistry();

        var result = registry.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_9999&DEV_1234",
            InstalledVersion = "1.0.0"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.NoMatchingVendor, result.FailureReasonType);
        Assert.Equal(VerificationSourceType.Unknown, result.VerificationSourceType);
        Assert.Equal("DriverVerifierRegistry fallback", result.SourceDetails);
    }

    private sealed class ProbeVendorDriverVerifier : IVendorDriverVerifier
    {
        public bool CanHandle(DriverIdentity identity) => true;

        public DriverVerificationResult Verify(DriverIdentity identity)
        {
            return new DriverVerificationResult
            {
                Status = DriverVerificationStatus.UpToDate,
                LatestOfficialVersion = "1.2.3",
                VerificationSourceType = VerificationSourceType.Unknown,
                SourceDetails = "probe",
                VerificationTimestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
