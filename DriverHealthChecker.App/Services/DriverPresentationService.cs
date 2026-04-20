namespace DriverHealthChecker.App;

internal interface IDriverPresentationService
{
    int GetCategoryOrder(DriverCategory category);
    DriverItem? BuildDeviceRecommendationItem();
}

internal sealed class DriverPresentationService : IDriverPresentationService
{
    private readonly IDeviceProfileDetector _deviceProfileDetector;
    private readonly ILaptopOemRecommendationResolver _laptopOemRecommendationResolver;

    public DriverPresentationService(
        IDeviceProfileDetector deviceProfileDetector,
        ILaptopOemRecommendationResolver laptopOemRecommendationResolver)
    {
        _deviceProfileDetector = deviceProfileDetector;
        _laptopOemRecommendationResolver = laptopOemRecommendationResolver;
    }

    public int GetCategoryOrder(DriverCategory category)
    {
        return category switch
        {
            DriverCategory.Gpu => 1,
            DriverCategory.Network => 2,
            DriverCategory.Storage => 3,
            DriverCategory.AudioMain => 4,
            DriverCategory.AudioExternal => 5,
            DriverCategory.DeviceRecommendation => 6,
            DriverCategory.HiddenSystem => 98,
            _ => 99
        };
    }

    public DriverItem? BuildDeviceRecommendationItem()
    {
        var profile = _deviceProfileDetector.TryGetDeviceProfile();
        if (profile == null || !profile.IsLaptop)
            return null;

        var action = _laptopOemRecommendationResolver.Resolve(profile.Manufacturer, profile.Model);

        return new DriverItem
        {
            Name = "Оптимизация ноутбука (рекомендация)",
            Manufacturer = string.IsNullOrWhiteSpace(profile.Manufacturer) ? "OEM" : profile.Manufacturer,
            Version = "-",
            Date = "-",
            CategoryKind = DriverCategory.DeviceRecommendation,
            StatusKind = DriverHealthStatus.Recommendation,
            OfficialAction = action,
            DetectionReason = $"Ноутбук: {profile.Manufacturer} {profile.Model}".Trim(),
            ButtonText = action.ButtonText,
            ButtonTooltip = $"{action.Tooltip} · Причина: ноутбук {profile.Manufacturer} {profile.Model}".Trim()
        };
    }
}
