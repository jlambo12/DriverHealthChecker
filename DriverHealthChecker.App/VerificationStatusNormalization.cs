using System;

namespace DriverHealthChecker.App;

internal static class VerificationStatusNormalization
{
    public static DriverHealthStatus MapToDriverHealthStatus(
        DriverVerificationStatus verificationStatus,
        string? driverName = null)
    {
        if (TryMapToDriverHealthStatus(verificationStatus, out var normalizedStatus))
            return normalizedStatus;

        var detail = string.IsNullOrWhiteSpace(driverName)
            ? string.Empty
            : $" for driver '{driverName}'";

        throw new ArgumentOutOfRangeException(
            nameof(verificationStatus),
            verificationStatus,
            $"Unsupported verification status{detail}.");
    }

    public static bool TryMapToDriverHealthStatus(
        DriverVerificationStatus verificationStatus,
        out DriverHealthStatus normalizedStatus)
    {
        switch (verificationStatus)
        {
            case DriverVerificationStatus.UpToDate:
                normalizedStatus = DriverHealthStatus.UpToDate;
                return true;
            case DriverVerificationStatus.UpdateAvailable:
                normalizedStatus = DriverHealthStatus.NeedsAttention;
                return true;
            case DriverVerificationStatus.UnableToVerifyReliably:
                normalizedStatus = DriverHealthStatus.NeedsReview;
                return true;
            default:
                normalizedStatus = DriverHealthStatus.Unknown;
                return false;
        }
    }
}
