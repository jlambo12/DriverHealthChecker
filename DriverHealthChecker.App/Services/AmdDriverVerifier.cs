using System;

namespace DriverHealthChecker.App;

internal sealed class AmdDriverVerifier : IVendorDriverVerifier
{
    public bool CanHandle(DriverIdentity identity)
    {
        return DriverIdentityVendorMatcher.IsAmd(identity);
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        // TODO: Implement real AMD official verification using a trusted official source.
        return new DriverVerificationResult
        {
            Status = DriverVerificationStatus.UnableToVerifyReliably,
            LatestKnownVersion = null,
            VerificationSource = "AMD official verification (stub)",
            VerificationTimestamp = DateTimeOffset.UtcNow,
            FailureReason = "Official AMD verification is not implemented yet.",
            EvidenceSummary = "Routing selected AMD verifier, but the verifier is currently a stub."
        };
    }
}
