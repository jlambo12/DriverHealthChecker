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
    private const bool DefaultUseVerificationForStatus = true;

    private readonly IDriverStatusEvaluator _driverStatusEvaluator;
    private readonly bool _useVerificationForStatus;
    internal int LastDecisionTotalCount { get; private set; }
    internal int LastDecisionUsedVerificationCount { get; private set; }
    internal int LastDecisionFallbackCount { get; private set; }

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
            var finalStatus = ApplyStatusDecision(
                driver,
                isRescan,
                previousSnapshot,
                ref recentlyUpdatedCount,
                ref usedVerificationCount,
                ref stayedLegacyCount);

            driver.StatusKind = finalStatus;
        }

        LastDecisionTotalCount = currentDrivers.Count;
        LastDecisionUsedVerificationCount = usedVerificationCount;
        LastDecisionFallbackCount = stayedLegacyCount;

        AppLogger.Info(
            $"Comparison applied. isRescan={isRescan}, total={currentDrivers.Count}, recentlyUpdated={recentlyUpdatedCount}, usedVerification={usedVerificationCount}, stayedLegacy={stayedLegacyCount}.");
        AppLogger.Info(
            $"Verification decision aggregate. totalDrivers={LastDecisionTotalCount}, usedVerification={LastDecisionUsedVerificationCount}, fallbackToLegacy={LastDecisionFallbackCount}.");
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

    private DriverHealthStatus ApplyStatusDecision(
        DriverItem driver,
        bool isRescan,
        IReadOnlyDictionary<string, DriverSnapshot> previousSnapshot,
        ref int recentlyUpdatedCount,
        ref int usedVerificationCount,
        ref int stayedLegacyCount)
    {
        if (IsRecentlyUpdated(driver, isRescan, previousSnapshot))
        {
            recentlyUpdatedCount++;
            stayedLegacyCount++;
            return DriverHealthStatus.RecentlyUpdated;
        }

        if (driver.CategoryKind == DriverCategory.DeviceRecommendation)
        {
            stayedLegacyCount++;
            return DriverHealthStatus.Recommendation;
        }

        var legacyStatus = GetLegacyStatus(driver);

        try
        {
            if (!_useVerificationForStatus)
            {
                stayedLegacyCount++;
                return legacyStatus;
            }

            var normalizedVerificationStatus = GetVerificationDerivedStatus(driver, driver.VerificationStatus);
            if (normalizedVerificationStatus == null)
            {
                stayedLegacyCount++;
                return legacyStatus;
            }

            var verificationStatus = MapNormalizedVerificationStatus(normalizedVerificationStatus.Value);
            if (verificationStatus != legacyStatus)
            {
                AppLogger.Info(
                    $"Verification decision mismatch. category={driver.Category}, name={driver.Name}, legacyStatus={legacyStatus}, verificationStatus={driver.VerificationStatus!.Value}.");
            }

            usedVerificationCount++;
            return verificationStatus;
        }
        catch (Exception ex)
        {
            stayedLegacyCount++;
            AppLogger.Error("Не удалось применить verification-based decision layer.", ex);
            return legacyStatus;
        }
    }

    // TODO: cleanup after legacy removal phase.
    private DriverHealthStatus GetLegacyStatus(DriverItem driver)
    {
        return _driverStatusEvaluator.EvaluateStatus(driver.Date);
    }

    private static NormalizedVerificationStatus? GetVerificationDerivedStatus(
        DriverItem driver,
        DriverVerificationStatus? verificationStatus)
    {
        if (verificationStatus == null)
            return null;

        return VerificationStatusNormalization.Normalize(
            verificationStatus.Value,
            driver.Name);
    }

    private static DriverHealthStatus MapNormalizedVerificationStatus(
        NormalizedVerificationStatus normalizedVerificationStatus)
    {
        return normalizedVerificationStatus switch
        {
            NormalizedVerificationStatus.UpToDate => DriverHealthStatus.UpToDate,
            NormalizedVerificationStatus.NeedsAttention => DriverHealthStatus.NeedsAttention,
            NormalizedVerificationStatus.NeedsReview => DriverHealthStatus.NeedsReview,
            _ => throw new ArgumentOutOfRangeException(
                nameof(normalizedVerificationStatus),
                normalizedVerificationStatus,
                "Unsupported normalized verification status.")
        };
    }

    private static bool IsRecentlyUpdated(
        DriverItem driver,
        bool isRescan,
        IReadOnlyDictionary<string, DriverSnapshot> previousSnapshot)
    {
        if (!isRescan)
            return false;

        if (!previousSnapshot.TryGetValue(BuildKey(driver), out var previous))
            return false;

        var versionChanged = !string.Equals(previous.Version, driver.Version, StringComparison.OrdinalIgnoreCase);
        var dateChanged = !string.Equals(previous.Date, driver.Date, StringComparison.OrdinalIgnoreCase);
        return versionChanged || dateChanged;
    }
}
