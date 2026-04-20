namespace DriverHealthChecker.App;

internal sealed class ScannedDriverRecord
{
    public string Name { get; init; } = string.Empty;
    public string? Manufacturer { get; init; }
    public string? Version { get; init; }
    public string? RawDate { get; init; }

    // Reserved for the honest verification pipeline. These fields are optional
    // and are not populated by the current scan implementation yet.
    public string? PnpDeviceId { get; init; }
    public List<string> HardwareIds { get; init; } = new();
    public List<string> CompatibleIds { get; init; } = new();
    public string? DriverProviderName { get; init; }
    public string? DriverInfName { get; init; }
    public string? DriverSignerName { get; init; }
    public string? DriverClass { get; init; }
    public string? ClassGuid { get; init; }
}
