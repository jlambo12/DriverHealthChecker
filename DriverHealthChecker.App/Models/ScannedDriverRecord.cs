namespace DriverHealthChecker.App;

internal sealed class ScannedDriverRecord
{
    public string Name { get; init; } = string.Empty;
    public string? Manufacturer { get; init; }
    public string? Version { get; init; }
    public string? RawDate { get; init; }
}
