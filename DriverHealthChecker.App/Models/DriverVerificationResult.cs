using System;

namespace DriverHealthChecker.App;

internal sealed class DriverVerificationResult
{
    public DriverVerificationStatus Status { get; init; } = DriverVerificationStatus.UnableToVerifyReliably;
    public string? LatestOfficialVersion { get; init; }
    public VerificationSourceType VerificationSourceType { get; init; } = VerificationSourceType.Unknown;
    public string SourceDetails { get; init; } = string.Empty;
    public DateTimeOffset? VerificationTimestamp { get; init; }
    public VerificationFailureReasonType? FailureReasonType { get; init; }
    public string? FailureReason { get; init; }
    public string? EvidenceSummary { get; init; }
}
