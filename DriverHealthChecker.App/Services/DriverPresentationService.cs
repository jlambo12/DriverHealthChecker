namespace DriverHealthChecker.App;

internal interface IDriverPresentationService
{
    int GetCategoryOrder(string category);
    string GetCategoryDisplay(string category);
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

    public int GetCategoryOrder(string category)
    {
        return category switch
        {
            "GPU" => 1,
            "Network" => 2,
            "Storage" => 3,
            "AudioMain" => 4,
            "AudioExternal" => 5,
            "DeviceRecommendation" => 6,
            "HiddenSystem" => 98,
            _ => 99
        };
    }

    public string GetCategoryDisplay(string category)
    {
        return category switch
        {
            "GPU" => "GPU",
            "Network" => "Сеть",
            "Storage" => "Хранение",
            "AudioMain" => "Аудио",
            "AudioExternal" => "Аудиокарта",
            "DeviceRecommendation" => "Рекомендация",
            "HiddenSystem" => "Скрытые",
            _ => category
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
            Category = "DeviceRecommendation",
            CategoryDisplay = GetCategoryDisplay("DeviceRecommendation"),
            Status = "Рекомендация",
            OfficialAction = action,
            DetectionReason = $"Ноутбук: {profile.Manufacturer} {profile.Model}".Trim(),
            ButtonText = action.ButtonText,
            ButtonTooltip = $"{action.Tooltip} · Причина: ноутбук {profile.Manufacturer} {profile.Model}".Trim()
        };
    }
}
