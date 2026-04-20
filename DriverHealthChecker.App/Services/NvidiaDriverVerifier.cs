using System;

namespace DriverHealthChecker.App;

internal sealed class NvidiaDriverVerifier : IVendorDriverVerifier
{
    public bool CanHandle(DriverIdentity identity)
    {
        return DriverIdentityVendorMatcher.IsNvidia(identity);
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        // TODO: Implement real NVIDIA official verification using a trusted official source.
        return new DriverVerificationResult
        {
            Status = DriverVerificationStatus.UnableToVerifyReliably,
            LatestOfficialVersion = null,
            VerificationSourceType = VerificationSourceType.Unknown,
            SourceDetails = "NVIDIA official verification (stub)",
            VerificationTimestamp = DateTimeOffset.UtcNow,
            FailureReasonType = VerificationFailureReasonType.VerifierNotImplemented,
            FailureReason = "Official NVIDIA verification is not implemented yet.",
            EvidenceSummary = "Routing selected NVIDIA verifier, but the verifier is currently a stub."
        };
    }
}
