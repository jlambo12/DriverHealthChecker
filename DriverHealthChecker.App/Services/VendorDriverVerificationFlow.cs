using System;

namespace DriverHealthChecker.App;

internal sealed class VendorDriverVerificationFlow
{
    private readonly IDriverIdentityTokenExtractor _tokenExtractor;
    private readonly IDriverVersionComparer _versionComparer;

    public VendorDriverVerificationFlow(IDriverIdentityTokenExtractor tokenExtractor, IDriverVersionComparer versionComparer)
    {
        _tokenExtractor = tokenExtractor ?? throw new ArgumentNullException(nameof(tokenExtractor));
        _versionComparer = versionComparer ?? throw new ArgumentNullException(nameof(versionComparer));
    }

    public DriverVerificationResult Verify(
        DriverIdentity identity,
        VendorDefinition vendorDefinition)
    {
        if (!_tokenExtractor.TryExtract(identity, out var tokens))
            return CreateVerificationFailed(
                vendorDefinition,
                latestOfficialVersion: null,
                VerificationSourceType.Unknown,
                $"{vendorDefinition.VendorName} verifier identity extraction",
                $"Could not extract a deterministic {vendorDefinition.VendorName} VEN_/DEV_ pair from DriverIdentity.",
                $"{vendorDefinition.VendorName} verifier could not extract vendorId/deviceId from PnP or hardware identity.");

        if (!string.Equals(tokens.VendorId, vendorDefinition.VendorId, StringComparison.Ordinal))
            return CreateNoMatchingVendor(vendorDefinition, tokens);

        if (!vendorDefinition.Source.TryGetLatestVersion(tokens.DeviceId, out var latestOfficialVersion))
            return CreateDeviceNotInOfficialDataset(vendorDefinition, tokens);

        if (string.IsNullOrWhiteSpace(identity.InstalledVersion))
            return CreateVerificationFailed(
                vendorDefinition,
                latestOfficialVersion,
                vendorDefinition.SourceType,
                vendorDefinition.Source.SourceDetails,
                "Installed driver version is missing from DriverIdentity.",
                $"{vendorDefinition.Source.SourceDetails} matched deviceId {tokens.DeviceId}, but installed version is unavailable.");

        var comparison = _versionComparer.Compare(identity.InstalledVersion, latestOfficialVersion);
        var status = comparison < 0
            ? DriverVerificationStatus.UpdateAvailable
            : DriverVerificationStatus.UpToDate;

        return new DriverVerificationResult
        {
            Status = status,
            LatestOfficialVersion = latestOfficialVersion,
            VerificationSourceType = vendorDefinition.SourceType,
            SourceDetails = vendorDefinition.Source.SourceDetails,
            VerificationTimestamp = DateTimeOffset.UtcNow,
            EvidenceSummary = $"{vendorDefinition.Source.SourceDetails} matched deviceId {tokens.DeviceId}: installed={identity.InstalledVersion}, latest={latestOfficialVersion}, comparison={comparison}."
        };
    }

    private static DriverVerificationResult CreateNoMatchingVendor(
        VendorDefinition vendorDefinition,
        DriverIdentityTokens tokens)
    {
        return BuildFailureResult(
            latestOfficialVersion: null,
            VerificationSourceType.Unknown,
            $"{vendorDefinition.VendorName} verifier routing guard",
            VerificationFailureReasonType.NoMatchingVendor,
            $"Verifier received vendorId {tokens.VendorId}, which does not match {vendorDefinition.VendorName}.",
            $"{vendorDefinition.VendorName} verifier received a non-{vendorDefinition.VendorName} identity after routing for deviceId {tokens.DeviceId}.");
    }

    private static DriverVerificationResult CreateVerificationFailed(
        VendorDefinition vendorDefinition,
        string? latestOfficialVersion,
        VerificationSourceType verificationSourceType,
        string sourceDetails,
        string failureReason,
        string evidenceSummary)
    {
        return BuildFailureResult(
            latestOfficialVersion,
            verificationSourceType,
            sourceDetails,
            VerificationFailureReasonType.VerificationFailed,
            failureReason,
            evidenceSummary);
    }

    private static DriverVerificationResult CreateDeviceNotInOfficialDataset(
        VendorDefinition vendorDefinition,
        DriverIdentityTokens tokens)
    {
        return BuildFailureResult(
            latestOfficialVersion: null,
            vendorDefinition.SourceType,
            vendorDefinition.Source.SourceDetails,
            VerificationFailureReasonType.DeviceNotInOfficialDataset,
            $"No {vendorDefinition.VendorName} official dataset entry exists for deviceId {tokens.DeviceId}.",
            $"{vendorDefinition.Source.SourceDetails} has no record for deviceId {tokens.DeviceId}.");
    }

    private static DriverVerificationResult BuildFailureResult(
        string? latestOfficialVersion,
        VerificationSourceType verificationSourceType,
        string sourceDetails,
        VerificationFailureReasonType failureReasonType,
        string failureReason,
        string evidenceSummary)
    {
        return new DriverVerificationResult
        {
            Status = DriverVerificationStatus.UnableToVerifyReliably,
            LatestOfficialVersion = latestOfficialVersion,
            VerificationSourceType = verificationSourceType,
            SourceDetails = sourceDetails,
            VerificationTimestamp = DateTimeOffset.UtcNow,
            FailureReasonType = failureReasonType,
            FailureReason = failureReason,
            EvidenceSummary = evidenceSummary
        };
    }
}
