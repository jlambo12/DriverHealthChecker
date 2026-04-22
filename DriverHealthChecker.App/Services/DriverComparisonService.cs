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
    internal int LastDecisionTotalCount { get; private set; }
    internal int LastDecisionUsedVerificationCount { get; private set; }
    internal int LastDecisionDefaultCount { get; private set; }

    public DriverComparisonService()
    {
    }

    public void ApplyComparison(List<DriverItem> currentDrivers, bool isRescan, IReadOnlyDictionary<string, DriverSnapshot> previousSnapshot)
    {
        var recentlyUpdatedCount = 0;
        var usedVerificationCount = 0;
        var defaultedCount = 0;

        foreach (var driver in currentDrivers)
        {
            var finalStatus = ApplyStatusDecision(
                driver,
                isRescan,
                previousSnapshot,
                ref recentlyUpdatedCount,
                ref usedVerificationCount,
                ref defaultedCount);

            driver.StatusKind = finalStatus;
        }

        LastDecisionTotalCount = currentDrivers.Count;
        LastDecisionUsedVerificationCount = usedVerificationCount;
        LastDecisionDefaultCount = defaultedCount;

        AppLogger.Info(
            $"Comparison applied. isRescan={isRescan}, total={currentDrivers.Count}, recentlyUpdated={recentlyUpdatedCount}, usedVerification={usedVerificationCount}, defaultedStatus={defaultedCount}.");
        AppLogger.Info(
            $"Verification decision aggregate. totalDrivers={LastDecisionTotalCount}, usedVerification={LastDecisionUsedVerificationCount}, defaultedStatus={LastDecisionDefaultCount}.");
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
        ref int defaultedCount)
    {
        if (IsRecentlyUpdated(driver, isRescan, previousSnapshot))
        {
            recentlyUpdatedCount++;
            return DriverHealthStatus.RecentlyUpdated;
        }

        if (driver.CategoryKind == DriverCategory.DeviceRecommendation)
        {
            return DriverHealthStatus.Recommendation;
        }

        try
        {
            var normalizedVerificationStatus = GetVerificationDerivedStatus(driver.VerificationStatus);
            if (normalizedVerificationStatus == null)
            {
                defaultedCount++;
                return GetDefaultStatus();
            }

            usedVerificationCount++;
            return MapNormalizedVerificationStatus(normalizedVerificationStatus.Value);
        }
        catch (Exception ex)
        {
            defaultedCount++;
            AppLogger.Error("Не удалось применить verification-only decision layer.", ex);
            return GetDefaultStatus();
        }
    }

    private static NormalizedVerificationStatus? GetVerificationDerivedStatus(
        DriverVerificationStatus? verificationStatus)
    {
        if (verificationStatus == null)
            return null;

        return VerificationStatusNormalization.TryNormalize(verificationStatus.Value, out var normalizedVerificationStatus)
            ? normalizedVerificationStatus
            : null;
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

    private static DriverHealthStatus GetDefaultStatus()
    {
        return DriverHealthStatus.NeedsReview;
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
