using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal sealed class NvidiaStubVersionSource : IVendorVersionSource
{
    private static readonly IReadOnlyDictionary<string, string> DeviceIdToLatestVersion = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["1C82"] = "551.86",
        ["2206"] = "552.12"
    };

    public string SourceDetails => "NVIDIA official dataset (stub)";

    public bool TryGetLatestVersion(string deviceId, out string latestOfficialVersion)
    {
        if (DeviceIdToLatestVersion.TryGetValue(deviceId, out var version))
        {
            latestOfficialVersion = version;
            return true;
        }

        latestOfficialVersion = string.Empty;
        return false;
    }
}
