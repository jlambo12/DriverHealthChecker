using System;

namespace DriverHealthChecker.App;

internal sealed class DriverVerificationResult
{
    public DriverVerificationStatus Status { get; init; } = DriverVerificationStatus.UnableToVerifyReliably;
    public string? LatestKnownVersion { get; init; }
    public string VerificationSource { get; init; } = string.Empty;
    public DateTimeOffset? VerificationTimestamp { get; init; }
    public string? FailureReason { get; init; }
    public string? EvidenceSummary { get; init; }
}
