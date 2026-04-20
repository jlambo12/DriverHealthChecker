using System;
using System.Collections.Generic;
using System.Linq;

namespace DriverHealthChecker.App;

internal interface IDriverSelectionService
{
    List<DriverItem> SelectBestDrivers(List<DriverItem> drivers);
}

internal sealed class DriverSelectionService : IDriverSelectionService
{
    private readonly IDriverVersionComparer _driverVersionComparer;

    public DriverSelectionService(IDriverVersionComparer driverVersionComparer)
    {
        _driverVersionComparer = driverVersionComparer;
    }

    public List<DriverItem> SelectBestDrivers(List<DriverItem> drivers)
    {
        var result = new List<DriverItem>();

        result.AddRange(
            drivers.Where(d => d.CategoryKind == DriverCategory.Gpu)
                .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(SelectBestByDateThenVersion)
                .Take(3));

        result.AddRange(
            drivers.Where(d => d.CategoryKind == DriverCategory.Network)
                .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(SelectBestByDateThenVersion)
                .OrderByDescending(ParseDateSafe)
                .Take(5));

        result.AddRange(
            drivers.Where(d => d.CategoryKind == DriverCategory.Storage)
                .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(SelectBestByDateThenVersion)
                .OrderByDescending(ParseDateSafe)
                .Take(3));

        var mainAudio = drivers.Where(d => d.CategoryKind == DriverCategory.AudioMain)
            .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .Select(SelectBestByDateThenVersion)
            .OrderByDescending(ParseDateSafe)
            .FirstOrDefault();

        if (mainAudio != null)
            result.Add(mainAudio);

        result.AddRange(
            drivers.Where(d => d.CategoryKind == DriverCategory.AudioExternal)
                .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(SelectBestByDateThenVersion)
                .OrderByDescending(ParseDateSafe));

        return result
            .GroupBy(d => $"{d.Category}|{d.Name}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private DriverItem SelectBestByDateThenVersion(IGrouping<string, DriverItem> group)
    {
        return group.OrderByDescending(ParseDateSafe)
            .ThenByDescending(d => d.Version, Comparer<string>.Create((x, y) => _driverVersionComparer.Compare(x, y)))
            .First();
    }

    private static DateTime ParseDateSafe(DriverItem driver)
    {
        if (DateTime.TryParse(driver.Date, out var parsed))
            return parsed;

        return DateTime.MinValue;
    }
}
