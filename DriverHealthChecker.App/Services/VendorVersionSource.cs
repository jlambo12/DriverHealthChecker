namespace DriverHealthChecker.App;

internal interface IVendorVersionSource
{
    string SourceDetails { get; }
    bool TryGetLatestVersion(string deviceId, out string latestOfficialVersion);
}
