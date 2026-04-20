using System;

namespace DriverHealthChecker.App;

internal sealed class IntelDriverVerifier : IVendorDriverVerifier
{
    public bool CanHandle(DriverIdentity identity)
    {
        return DriverIdentityVendorMatcher.IsIntel(identity);
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        // TODO: Implement real Intel official verification using a trusted official source.
        return new DriverVerificationResult
        {
            Status = DriverVerificationStatus.UnableToVerifyReliably,
            LatestKnownVersion = null,
            VerificationSource = "Intel official verification (stub)",
            VerificationTimestamp = DateTimeOffset.UtcNow,
            FailureReason = "Official Intel verification is not implemented yet.",
            EvidenceSummary = "Routing selected Intel verifier, but the verifier is currently a stub."
        };
    }
}
