using System;
using System.Collections.Generic;
using System.Linq;

namespace DriverHealthChecker.App;

internal interface IDriverComparisonService
{
    void ApplyComparison(List<DriverItem> currentDrivers, bool isRescan, IReadOnlyDictionary<string, DriverSnapshot> previousSnapshot);
    Dictionary<string, DriverSnapshot> BuildSnapshot(List<DriverItem> currentDrivers);
}

internal sealed class DriverComparisonService : IDriverComparisonService
{
    private readonly IDriverStatusEvaluator _driverStatusEvaluator;

    public DriverComparisonService(IDriverStatusEvaluator driverStatusEvaluator)
    {
        _driverStatusEvaluator = driverStatusEvaluator;
    }

    public void ApplyComparison(List<DriverItem> currentDrivers, bool isRescan, IReadOnlyDictionary<string, DriverSnapshot> previousSnapshot)
    {
        var recentlyUpdatedCount = 0;
        foreach (var driver in currentDrivers)
        {
            var key = BuildKey(driver);

            if (isRescan && previousSnapshot.TryGetValue(key, out var previous))
            {
                var versionChanged = !string.Equals(previous.Version, driver.Version, StringComparison.OrdinalIgnoreCase);
                var dateChanged = !string.Equals(previous.Date, driver.Date, StringComparison.OrdinalIgnoreCase);

                if (versionChanged || dateChanged)
                {
                    driver.Status = "Недавно обновлён";
                    recentlyUpdatedCount++;
                    continue;
                }
            }

            if (driver.Category == "DeviceRecommendation")
            {
                driver.Status = "Рекомендация";
                continue;
            }

            driver.Status = _driverStatusEvaluator.EvaluateStatus(driver.Date);
        }

        AppLogger.Info($"Comparison applied. isRescan={isRescan}, total={currentDrivers.Count}, recentlyUpdated={recentlyUpdatedCount}.");
    }

    public Dictionary<string, DriverSnapshot> BuildSnapshot(List<DriverItem> currentDrivers)
    {
        return currentDrivers.ToDictionary(
            BuildKey,
            d => new DriverSnapshot
            {
                Version = d.Version,
                Date = d.Date
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildKey(DriverItem driver)
    {
        return $"{driver.Category}|{driver.Name}";
    }
}
