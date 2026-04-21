namespace DriverHealthChecker.App;

internal sealed class DriverVerificationObservation
{
    public string DriverName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public DriverVerificationResult Result { get; init; } = new();
}
