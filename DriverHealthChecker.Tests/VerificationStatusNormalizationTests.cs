using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class VerificationStatusNormalizationTests
{
    [Fact]
    public void Normalize_MapsVerificationStatusesToNormalizedStatuses()
    {
        Assert.Equal(
            NormalizedVerificationStatus.UpToDate,
            VerificationStatusNormalization.Normalize(DriverVerificationStatus.UpToDate));
        Assert.Equal(
            NormalizedVerificationStatus.NeedsAttention,
            VerificationStatusNormalization.Normalize(DriverVerificationStatus.UpdateAvailable));
        Assert.Equal(
            NormalizedVerificationStatus.NeedsReview,
            VerificationStatusNormalization.Normalize(DriverVerificationStatus.UnableToVerifyReliably));
    }

    [Fact]
    public void TryNormalize_WhenVerificationStatusIsInvalid_ReturnsFalseAndUnknown()
    {
        var normalized = VerificationStatusNormalization.TryNormalize(
            (DriverVerificationStatus)999,
            out var actualStatus);

        Assert.False(normalized);
        Assert.Equal(NormalizedVerificationStatus.Unknown, actualStatus);
    }
}
