using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class AmdDriverVerifierTests
{
    [Fact]
    public void Should_Handle_Amd_Identity()
    {
        var verifier = new AmdDriverVerifier();

        var canHandle = verifier.CanHandle(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_1002&DEV_ABCD"
        });

        Assert.True(canHandle);
    }

    [Fact]
    public void Should_Not_Handle_Non_Amd()
    {
        var verifier = new AmdDriverVerifier();

        var canHandle = verifier.CanHandle(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_8086&DEV_ABCD"
        });

        Assert.False(canHandle);
    }

    [Fact]
    public void Should_Return_UpToDate_When_Versions_Equal()
    {
        var verifier = new AmdDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_1002&DEV_ABCD",
            InstalledVersion = "10.0.1"
        });

        Assert.Equal(DriverVerificationStatus.UpToDate, result.Status);
        Assert.Equal("10.0.1", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Equal("AMD official dataset (stub)", result.SourceDetails);
    }

    [Fact]
    public void Should_Return_UpdateAvailable_When_Installed_Lower()
    {
        var verifier = new AmdDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_1002&DEV_EF01",
            InstalledVersion = "20.4.0"
        });

        Assert.Equal(DriverVerificationStatus.UpdateAvailable, result.Status);
        Assert.Equal("20.5.3", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Contains("comparison=-1", result.EvidenceSummary);
    }

    [Fact]
    public void Should_Return_DeviceNotInDataset()
    {
        var verifier = new AmdDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_1002&DEV_9999",
            InstalledVersion = "1.0.0"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.DeviceNotInOfficialDataset, result.FailureReasonType);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Contains("9999", result.FailureReason);
    }

    [Fact]
    public void Should_Return_VerificationFailed_When_No_InstalledVersion()
    {
        var verifier = new AmdDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_1002&DEV_EF01"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.VerificationFailed, result.FailureReasonType);
        Assert.Equal("20.5.3", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
    }
}
