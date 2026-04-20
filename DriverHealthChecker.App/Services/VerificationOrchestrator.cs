using System;
using System.Collections.Generic;
using System.Linq;

namespace DriverHealthChecker.App;

internal interface IOfficialDriverVerifier
{
    DriverVerificationResult Verify(DriverIdentity identity);
}

internal interface IVendorDriverVerifier
{
    bool CanHandle(DriverIdentity identity);
    DriverVerificationResult Verify(DriverIdentity identity);
}

internal interface IVerificationOrchestrator : IOfficialDriverVerifier
{
    IVendorDriverVerifier? SelectVerifier(DriverIdentity identity);
}

internal sealed class VerificationOrchestrator : IVerificationOrchestrator
{
    private readonly IOfficialSupportChannelResolver _officialSupportChannelResolver;
    private readonly IReadOnlyList<IVendorDriverVerifier> _vendorDriverVerifiers;

    public VerificationOrchestrator(
        IOfficialSupportChannelResolver officialSupportChannelResolver,
        IEnumerable<IVendorDriverVerifier> vendorDriverVerifiers)
    {
        _officialSupportChannelResolver = officialSupportChannelResolver;
        _vendorDriverVerifiers = vendorDriverVerifiers.ToList();
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        var supportChannel = _officialSupportChannelResolver.Resolve(identity);
        var verifier = SelectVerifier(identity);
        if (verifier != null)
            return verifier.Verify(identity);

        return new DriverVerificationResult
        {
            Status = DriverVerificationStatus.UnableToVerifyReliably,
            LatestKnownVersion = null,
            VerificationSource = BuildFallbackSource(supportChannel),
            VerificationTimestamp = DateTimeOffset.UtcNow,
            FailureReason = "No vendor verifier is registered for this identity.",
            EvidenceSummary = $"Resolved support channel: {supportChannel.Type}. Verification routing stopped before any official source call."
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

    private static string BuildFallbackSource(OfficialSupportChannel supportChannel)
    {
        if (!string.IsNullOrWhiteSpace(supportChannel.DisplayName))
            return supportChannel.DisplayName;

        return $"OfficialSupportChannel:{supportChannel.Type}";
    }
}
