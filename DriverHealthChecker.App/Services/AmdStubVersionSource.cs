using System;
using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal sealed class AmdStubVersionSource : IVendorVersionSource
{
    private static readonly IReadOnlyDictionary<string, string> DeviceIdToLatestVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["abcd"] = "10.0.1",
        ["ef01"] = "20.5.3"
    };

    public string SourceDetails => "AMD official dataset (stub)";

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
