using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal sealed class DriverIdentity
{
    public string DisplayName { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string NormalizedManufacturer { get; init; } = string.Empty;
    public string? PnpDeviceId { get; init; }
    public List<string> HardwareIds { get; init; } = new();
    public List<string> CompatibleIds { get; init; } = new();
    public string? DriverClass { get; init; }
    public string? ClassGuid { get; init; }
    public string? DriverProviderName { get; init; }
    public string? DriverInfName { get; init; }
    public string? DriverSignerName { get; init; }
}
