using System;
using System.Collections.Generic;
using System.Linq;

namespace DriverHealthChecker.App;

internal interface IDriverFilteringService
{
    List<string> BuildCategoryItems(IReadOnlyCollection<DriverItem> currentDrivers, IReadOnlyCollection<DriverItem> hiddenDrivers, bool showHidden);
    List<DriverItem> ApplyFilters(IReadOnlyCollection<DriverItem> currentDrivers, IReadOnlyCollection<DriverItem> hiddenDrivers, DriverFilterState filterState);
}

internal sealed class DriverFilteringService : IDriverFilteringService
{
    public List<string> BuildCategoryItems(IReadOnlyCollection<DriverItem> currentDrivers, IReadOnlyCollection<DriverItem> hiddenDrivers, bool showHidden)
    {
        var source = currentDrivers.AsEnumerable();
        if (showHidden)
            source = source.Concat(hiddenDrivers);

        return new[] { "Все" }
            .Concat(source.Select(d => d.CategoryDisplay).Distinct().OrderBy(c => c))
            .ToList();
    }

    public List<DriverItem> ApplyFilters(IReadOnlyCollection<DriverItem> currentDrivers, IReadOnlyCollection<DriverItem> hiddenDrivers, DriverFilterState filterState)
    {
        var source = currentDrivers.AsEnumerable();
        if (filterState.ShowHidden)
            source = source.Concat(hiddenDrivers);

        var query = source;

        if (!string.Equals(filterState.SelectedCategory, "Все", StringComparison.OrdinalIgnoreCase))
            query = query.Where(d => string.Equals(d.CategoryDisplay, filterState.SelectedCategory, StringComparison.OrdinalIgnoreCase));

        if (!string.Equals(filterState.SelectedStatus, "Все", StringComparison.OrdinalIgnoreCase))
            query = query.Where(d => string.Equals(d.Status, filterState.SelectedStatus, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filterState.Search))
        {
            query = query.Where(d =>
                d.Name.Contains(filterState.Search, StringComparison.OrdinalIgnoreCase) ||
                d.Manufacturer.Contains(filterState.Search, StringComparison.OrdinalIgnoreCase) ||
                d.DetectionReason.Contains(filterState.Search, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }
}
