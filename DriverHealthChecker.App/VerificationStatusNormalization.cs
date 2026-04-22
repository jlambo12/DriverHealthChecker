using System;

namespace DriverHealthChecker.App;

internal enum NormalizedVerificationStatus
{
    Unknown = 0,
    UpToDate,
    NeedsAttention,
    NeedsReview
}

internal static class VerificationStatusNormalization
{
    public static NormalizedVerificationStatus Normalize(
        DriverVerificationStatus verificationStatus,
        string? driverName = null)
    {
        if (TryNormalize(verificationStatus, out var normalizedStatus))
            return normalizedStatus;

        var detail = string.IsNullOrWhiteSpace(driverName)
            ? string.Empty
            : $" for driver '{driverName}'";

        throw new ArgumentOutOfRangeException(
            nameof(verificationStatus),
            verificationStatus,
            $"Unsupported verification status{detail}.");
    }

    public static bool TryNormalize(
        DriverVerificationStatus verificationStatus,
        out NormalizedVerificationStatus normalizedStatus)
    {
        switch (verificationStatus)
        {
            case DriverVerificationStatus.UpToDate:
                normalizedStatus = NormalizedVerificationStatus.UpToDate;
                return true;
            case DriverVerificationStatus.UpdateAvailable:
                normalizedStatus = NormalizedVerificationStatus.NeedsAttention;
                return true;
            case DriverVerificationStatus.UnableToVerifyReliably:
                normalizedStatus = NormalizedVerificationStatus.NeedsReview;
                return true;
            default:
                normalizedStatus = NormalizedVerificationStatus.Unknown;
                return false;
        }
    }

    public static DriverHealthStatus MapToDriverHealthStatus(
        DriverVerificationStatus verificationStatus,
        string? driverName = null)
    {
        return Normalize(verificationStatus, driverName) switch
        {
            NormalizedVerificationStatus.UpToDate => DriverHealthStatus.UpToDate,
            NormalizedVerificationStatus.NeedsAttention => DriverHealthStatus.NeedsAttention,
            NormalizedVerificationStatus.NeedsReview => DriverHealthStatus.NeedsReview,
            _ => throw new ArgumentOutOfRangeException(
                nameof(verificationStatus),
                verificationStatus,
                "Unsupported normalized verification status.")
        };
    }

    public static bool TryMapToDriverHealthStatus(
        DriverVerificationStatus verificationStatus,
        out DriverHealthStatus normalizedStatus)
    {
        if (!TryNormalize(verificationStatus, out var normalizedVerificationStatus))
        {
            normalizedStatus = DriverHealthStatus.Unknown;
            return false;
        }

        normalizedStatus = normalizedVerificationStatus switch
        {
            NormalizedVerificationStatus.UpToDate => DriverHealthStatus.UpToDate,
            NormalizedVerificationStatus.NeedsAttention => DriverHealthStatus.NeedsAttention,
            NormalizedVerificationStatus.NeedsReview => DriverHealthStatus.NeedsReview,
            _ => DriverHealthStatus.Unknown
        };

        return normalizedStatus != DriverHealthStatus.Unknown;
    }
}
