using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class IntelDriverVerifierTests
{
    [Fact]
    public void Should_Handle_Intel_Identity()
    {
        var verifier = new IntelDriverVerifier();

        var canHandle = verifier.CanHandle(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_8086&DEV_1234"
        });

        Assert.True(canHandle);
    }

    [Fact]
    public void Should_Not_Handle_Non_Intel()
    {
        var verifier = new IntelDriverVerifier();

        var canHandle = verifier.CanHandle(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_1234"
        });

        Assert.False(canHandle);
    }

    [Fact]
    public void Should_Return_UpToDate_When_Versions_Equal()
    {
        var verifier = new IntelDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_8086&DEV_1234",
            InstalledVersion = "1.0.0"
        });

        Assert.Equal(DriverVerificationStatus.UpToDate, result.Status);
        Assert.Equal("1.0.0", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Equal("Intel official dataset (stub)", result.SourceDetails);
    }

    [Fact]
    public void Should_Return_UpdateAvailable_When_Installed_Lower()
    {
        var verifier = new IntelDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_8086&DEV_5678",
            InstalledVersion = "1.5.0"
        });

        Assert.Equal(DriverVerificationStatus.UpdateAvailable, result.Status);
        Assert.Equal("2.0.0", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
        Assert.Contains("comparison=-1", result.EvidenceSummary);
    }

    [Fact]
    public void Should_Return_DeviceNotInDataset()
    {
        var verifier = new IntelDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_8086&DEV_9999",
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
        var verifier = new IntelDriverVerifier();

        var result = verifier.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_8086&DEV_5678"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.VerificationFailed, result.FailureReasonType);
        Assert.Equal("2.0.0", result.LatestOfficialVersion);
        Assert.Equal(VerificationSourceType.OfficialApi, result.VerificationSourceType);
    }
}
