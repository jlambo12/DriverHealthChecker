using System.Collections.Generic;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverFilteringServiceTests
{
    private readonly IDriverFilteringService _service = new DriverFilteringService();

    [Fact]
    public void BuildCategoryItems_IncludesHiddenCategories_WhenShowHiddenTrue()
    {
        var current = new List<DriverItem>
        {
            new() { CategoryDisplay = "GPU" }
        };
        var hidden = new List<DriverItem>
        {
            new() { CategoryDisplay = "Скрытые" }
        };

        var items = _service.BuildCategoryItems(current, hidden, showHidden: true);

        Assert.Contains("GPU", items);
        Assert.Contains("Скрытые", items);
    }

    [Fact]
    public void ApplyFilters_FiltersByStatusAndSearch()
    {
        var current = new List<DriverItem>
        {
            new() { Name = "Intel Wi-Fi", Manufacturer = "Intel", DetectionReason = "network", Status = "Стоит проверить", CategoryDisplay = "Сеть" },
            new() { Name = "Realtek Audio", Manufacturer = "Realtek", DetectionReason = "audio", Status = "Актуален", CategoryDisplay = "Аудио" }
        };

        var filtered = _service.ApplyFilters(current, new List<DriverItem>(), new DriverFilterState
        {
            SelectedCategory = "Все",
            SelectedStatus = "Стоит проверить",
            Search = "intel",
            ShowHidden = false
        });

        Assert.Single(filtered);
        Assert.Equal("Intel Wi-Fi", filtered[0].Name);
    }
}
