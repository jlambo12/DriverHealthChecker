using System;
using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal sealed class IntelStubVersionSource : IVendorVersionSource
{
    private static readonly IReadOnlyDictionary<string, string> DeviceIdToLatestVersion = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["1234"] = "1.0.0",
        ["5678"] = "2.0.0"
    };

    public string SourceDetails => "Intel official dataset (stub)";

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
