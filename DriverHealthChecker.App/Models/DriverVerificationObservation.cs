namespace DriverHealthChecker.App;

internal sealed class DriverVerificationObservation
{
    public string DriverKey { get; init; } = string.Empty;
    public string DriverName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string? VendorId { get; init; }
    public string? DeviceId { get; init; }
    public DriverVerificationResult Result { get; init; } = new();
    public DriverHealthStatus LegacyStatus { get; set; } = DriverHealthStatus.Unknown;
    public DriverVerificationStatus VerificationStatus { get; set; } = DriverVerificationStatus.UnableToVerifyReliably;
    public bool IsMatch { get; set; }
}
