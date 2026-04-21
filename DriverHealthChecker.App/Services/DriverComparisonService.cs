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
    private const bool DefaultUseVerificationForStatus = false;

    private readonly IDriverStatusEvaluator _driverStatusEvaluator;
    private readonly bool _useVerificationForStatus;

    public DriverComparisonService(IDriverStatusEvaluator driverStatusEvaluator)
        : this(driverStatusEvaluator, DefaultUseVerificationForStatus)
    {
    }

    internal DriverComparisonService(IDriverStatusEvaluator driverStatusEvaluator, bool useVerificationForStatus)
    {
        _driverStatusEvaluator = driverStatusEvaluator;
        _useVerificationForStatus = useVerificationForStatus;
    }

    public void ApplyComparison(List<DriverItem> currentDrivers, bool isRescan, IReadOnlyDictionary<string, DriverSnapshot> previousSnapshot)
    {
        var recentlyUpdatedCount = 0;
        var usedVerificationCount = 0;
        var stayedLegacyCount = 0;

        foreach (var driver in currentDrivers)
        {
            var key = BuildKey(driver);

            if (isRescan && previousSnapshot.TryGetValue(key, out var previous))
            {
                var versionChanged = !string.Equals(previous.Version, driver.Version, StringComparison.OrdinalIgnoreCase);
                var dateChanged = !string.Equals(previous.Date, driver.Date, StringComparison.OrdinalIgnoreCase);

                if (versionChanged || dateChanged)
                {
                    driver.StatusKind = DriverHealthStatus.RecentlyUpdated;
                    recentlyUpdatedCount++;
                    stayedLegacyCount++;
                    continue;
                }
            }

            if (driver.CategoryKind == DriverCategory.DeviceRecommendation)
            {
                driver.StatusKind = DriverHealthStatus.Recommendation;
                stayedLegacyCount++;
                continue;
            }

            driver.StatusKind = _driverStatusEvaluator.EvaluateStatus(driver.Date);

            ApplyVerificationDecision(driver, ref usedVerificationCount, ref stayedLegacyCount);
        }

        AppLogger.Info(
            $"Comparison applied. isRescan={isRescan}, total={currentDrivers.Count}, recentlyUpdated={recentlyUpdatedCount}, usedVerification={usedVerificationCount}, stayedLegacy={stayedLegacyCount}.");
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

    private void ApplyVerificationDecision(
        DriverItem driver,
        ref int usedVerificationCount,
        ref int stayedLegacyCount)
    {
        try
        {
            if (!_useVerificationForStatus || driver.VerificationStatus == null)
            {
                stayedLegacyCount++;
                return;
            }

            driver.StatusKind = MapVerificationStatus(driver.VerificationStatus.Value);
            usedVerificationCount++;
        }
        catch (Exception ex)
        {
            stayedLegacyCount++;
            AppLogger.Error("Не удалось применить verification-based decision layer.", ex);
        }
    }

    private static DriverHealthStatus MapVerificationStatus(DriverVerificationStatus verificationStatus)
    {
        return verificationStatus switch
        {
            DriverVerificationStatus.UpToDate => DriverHealthStatus.UpToDate,
            DriverVerificationStatus.UpdateAvailable => DriverHealthStatus.NeedsAttention,
            DriverVerificationStatus.UnableToVerifyReliably => DriverHealthStatus.NeedsReview,
            _ => DriverHealthStatus.NeedsReview
        };
    }
}
