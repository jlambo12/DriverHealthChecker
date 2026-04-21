using System;
using System.Collections.Generic;
using System.Linq;

namespace DriverHealthChecker.App;

internal sealed class DriverVerifierRegistry
{
    private readonly IReadOnlyList<IVendorDriverVerifier> _verifiers;

    public DriverVerifierRegistry()
        : this(
        [
            new NvidiaDriverVerifier(),
            new IntelDriverVerifier(),
            new AmdDriverVerifier()
        ])
    {
    }

    public DriverVerifierRegistry(IEnumerable<IVendorDriverVerifier> verifiers)
    {
        _verifiers = verifiers?.ToList() ?? throw new ArgumentNullException(nameof(verifiers));
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        foreach (var verifier in _verifiers)
        {
            if (verifier.CanHandle(identity))
                return verifier.Verify(identity);
        }

        return CreateNoMatchingVendorResult();
    }

    private static DriverVerificationResult CreateNoMatchingVendorResult()
    {
        return new DriverVerificationResult
        {
            Status = DriverVerificationStatus.UnableToVerifyReliably,
            VerificationSourceType = VerificationSourceType.Unknown,
            SourceDetails = "DriverVerifierRegistry fallback",
            VerificationTimestamp = DateTimeOffset.UtcNow,
            FailureReasonType = VerificationFailureReasonType.NoMatchingVendor,
            FailureReason = "No vendor verifier matched the provided identity.",
            EvidenceSummary = "DriverVerifierRegistry could not route the identity to any registered vendor verifier."
        };
    }
}
