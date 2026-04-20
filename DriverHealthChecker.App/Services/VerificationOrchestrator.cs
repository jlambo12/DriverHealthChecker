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
        if (verifier == null)
        {
            return BuildFallbackResult(
                VerificationFailureReasonType.NoMatchingVendor,
                "No vendor verifier is registered for this identity.",
                "Verification orchestrator could not match the identity to any registered vendor verifier.");
        }

        try
        {
            return verifier.Verify(identity);
        }
        catch (Exception ex)
        {
            return BuildFallbackResult(
                VerificationFailureReasonType.VerificationFailed,
                "Vendor verifier threw an exception before any real official verification completed.",
                $"Verifier routing failed with exception type {ex.GetType().Name}.");
        }
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

    private static DriverVerificationResult BuildFallbackResult(
        VerificationFailureReasonType failureReasonType,
        string failureReason,
        string evidenceSummary)
    {
        return new DriverVerificationResult
        {
            Status = DriverVerificationStatus.UnableToVerifyReliably,
            LatestOfficialVersion = null,
            VerificationSourceType = VerificationSourceType.Unknown,
            SourceDetails = "VerificationOrchestrator fallback",
            VerificationTimestamp = DateTimeOffset.UtcNow,
            FailureReasonType = failureReasonType,
            FailureReason = failureReason,
            EvidenceSummary = evidenceSummary
        };
    }
}
