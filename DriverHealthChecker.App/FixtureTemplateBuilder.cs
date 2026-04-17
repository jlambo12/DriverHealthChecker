using System;
using System.Collections.Generic;
using System.Linq;

namespace DriverHealthChecker.App;

internal interface IFixtureTemplateBuilder
{
    IReadOnlyCollection<FixtureTemplateItem> Build(IEnumerable<DriverItem> drivers);
}

internal sealed class FixtureTemplateBuilder : IFixtureTemplateBuilder
{
    public IReadOnlyCollection<FixtureTemplateItem> Build(IEnumerable<DriverItem> drivers)
    {
        return drivers
            .Where(d => !string.Equals(d.Category, "DeviceRecommendation", StringComparison.OrdinalIgnoreCase))
            .GroupBy(d => $"{d.Name}|{d.Manufacturer}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .Select(Map)
            .ToList();
    }

    private static FixtureTemplateItem Map(DriverItem driver)
    {
        if (string.Equals(driver.Category, "HiddenSystem", StringComparison.OrdinalIgnoreCase))
        {
            return new FixtureTemplateItem
            {
                Name = driver.Name,
                Manufacturer = driver.Manufacturer,
                ExpectedCategory = string.Empty,
                ShouldClassify = false,
                Note = "Скрыто системным/служебным фильтром"
            };
        }

        return new FixtureTemplateItem
        {
            Name = driver.Name,
            Manufacturer = driver.Manufacturer,
            ExpectedCategory = driver.Category,
            ShouldClassify = true,
            Note = "Автогенерация из scan-history, проверь категорию вручную"
        };
    }
}

internal sealed class FixtureTemplateItem
{
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ExpectedCategory { get; set; } = string.Empty;
    public bool ShouldClassify { get; set; }
    public string Note { get; set; } = string.Empty;
}
