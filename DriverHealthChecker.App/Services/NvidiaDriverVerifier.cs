using System;
namespace DriverHealthChecker.App;

internal sealed class NvidiaDriverVerifier : IVendorDriverVerifier
{
    private const string NvidiaVendorId = "10DE";

    private readonly IDriverVersionComparer _driverVersionComparer;
    private readonly INvidiaVersionSource _versionSource;

    public NvidiaDriverVerifier()
        : this(new DriverVersionComparer(), new NvidiaStubVersionSource())
    {
    }

    internal NvidiaDriverVerifier(IDriverVersionComparer driverVersionComparer, INvidiaVersionSource versionSource)
    {
        _driverVersionComparer = driverVersionComparer;
        _versionSource = versionSource;
    }

    public bool CanHandle(DriverIdentity identity)
    {
        return DriverIdentityVendorMatcher.IsNvidia(identity);
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        if (!TryExtractVendorAndDeviceId(identity, out var vendorId, out var deviceId))
        {
            return BuildFailureResult(
                latestOfficialVersion: null,
                VerificationSourceType.Unknown,
                "NVIDIA verifier identity extraction",
                VerificationFailureReasonType.VerificationFailed,
                "Could not extract a deterministic NVIDIA VEN_/DEV_ pair from DriverIdentity.",
                "NVIDIA verifier could not extract vendorId/deviceId from PnP or hardware identity.");
        }

        if (!string.Equals(vendorId, NvidiaVendorId, StringComparison.Ordinal))
        {
            return BuildFailureResult(
                latestOfficialVersion: null,
                VerificationSourceType.Unknown,
                "NVIDIA verifier routing guard",
                VerificationFailureReasonType.NoMatchingVendor,
                $"Verifier received vendorId {vendorId}, which does not match NVIDIA.",
                $"NVIDIA verifier received a non-NVIDIA identity after routing for deviceId {deviceId}.");
        }

        if (!_versionSource.TryGetLatestVersion(deviceId, out var latestOfficialVersion))
        {
            return BuildFailureResult(
                latestOfficialVersion: null,
                VerificationSourceType.OfficialApi,
                _versionSource.SourceDetails,
                VerificationFailureReasonType.DeviceNotInOfficialDataset,
                $"No NVIDIA official dataset entry exists for deviceId {deviceId}.",
                $"{_versionSource.SourceDetails} has no record for deviceId {deviceId}.");
        }

        if (string.IsNullOrWhiteSpace(identity.InstalledVersion))
        {
            return BuildFailureResult(
                latestOfficialVersion,
                VerificationSourceType.OfficialApi,
                _versionSource.SourceDetails,
                VerificationFailureReasonType.VerificationFailed,
                "Installed driver version is missing from DriverIdentity.",
                $"{_versionSource.SourceDetails} matched deviceId {deviceId}, but installed version is unavailable.");
        }

        var comparison = _driverVersionComparer.Compare(identity.InstalledVersion, latestOfficialVersion);
        var status = comparison < 0
            ? DriverVerificationStatus.UpdateAvailable
            : DriverVerificationStatus.UpToDate;

        return new DriverVerificationResult
        {
            Status = status,
            LatestOfficialVersion = latestOfficialVersion,
            VerificationSourceType = VerificationSourceType.OfficialApi,
            SourceDetails = _versionSource.SourceDetails,
            VerificationTimestamp = DateTimeOffset.UtcNow,
            EvidenceSummary = $"{_versionSource.SourceDetails} matched deviceId {deviceId}: installed={identity.InstalledVersion}, latest={latestOfficialVersion}, comparison={comparison}."
        };
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

    private static bool TryExtractVendorAndDeviceId(DriverIdentity identity, out string vendorId, out string deviceId)
    {
        foreach (var identifier in EnumerateIdentityCandidates(identity))
        {
            if (!TryExtractToken(identifier, "VEN_", out vendorId))
                continue;

            if (!TryExtractToken(identifier, "DEV_", out deviceId))
                continue;

            return true;
        }

        vendorId = string.Empty;
        deviceId = string.Empty;
        return false;
    }

    private static IEnumerable<string?> EnumerateIdentityCandidates(DriverIdentity identity)
    {
        yield return identity.PnpDeviceId;

        foreach (var hardwareId in identity.HardwareIds)
            yield return hardwareId;

        foreach (var compatibleId in identity.CompatibleIds)
            yield return compatibleId;
    }

    private static bool TryExtractToken(string? identifier, string prefix, out string tokenValue)
    {
        tokenValue = string.Empty;
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        var segments = identifier.Split(['\\', '&'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            if (!segment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (segment.Length != prefix.Length + 4)
                continue;

            var candidate = segment.Substring(prefix.Length, 4).ToUpperInvariant();
            if (!IsHexToken(candidate))
                continue;

            tokenValue = candidate;
            return true;
        }

        return false;
    }

    private static bool IsHexToken(string candidate)
    {
        foreach (var ch in candidate)
        {
            var isDigit = ch >= '0' && ch <= '9';
            var isUpperHex = ch >= 'A' && ch <= 'F';
            if (!isDigit && !isUpperHex)
                return false;
        }

        return candidate.Length == 4;
    }
}
