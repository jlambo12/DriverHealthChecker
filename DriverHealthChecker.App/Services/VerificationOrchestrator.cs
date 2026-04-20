using System;
using System.Collections.Generic;
using System.Linq;

namespace DriverHealthChecker.App;

internal interface IVendorDriverVerifier
{
    bool CanHandle(DriverIdentity identity);
    DriverVerificationResult Verify(DriverIdentity identity);
}

internal interface IVerificationOrchestrator
{
    DriverVerificationResult Verify(DriverIdentity identity);
    IVendorDriverVerifier? SelectVerifier(DriverIdentity identity);
}

internal sealed class VerificationOrchestrator : IVerificationOrchestrator
{
    private readonly IReadOnlyList<IVendorDriverVerifier> _vendorDriverVerifiers;

    public VerificationOrchestrator(IEnumerable<IVendorDriverVerifier> vendorDriverVerifiers)
    {
        _vendorDriverVerifiers = vendorDriverVerifiers.ToList();
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        var verifier = SelectVerifier(identity);
        if (verifier != null)
            return verifier.Verify(identity);

        return new DriverVerificationResult
        {
            Status = DriverVerificationStatus.UnableToVerifyReliably,
            LatestKnownVersion = null,
            VerificationSource = "VerificationOrchestrator fallback",
            VerificationTimestamp = DateTimeOffset.UtcNow,
            FailureReason = "No vendor verifier is registered for this identity.",
            EvidenceSummary = "Verification orchestrator could not match the identity to any registered vendor verifier."
        };
    }

    public IVendorDriverVerifier? SelectVerifier(DriverIdentity identity)
    {
        foreach (var vendorDriverVerifier in _vendorDriverVerifiers)
        {
            if (vendorDriverVerifier.CanHandle(identity))
                return vendorDriverVerifier;
        }

        return null;
    }
}
