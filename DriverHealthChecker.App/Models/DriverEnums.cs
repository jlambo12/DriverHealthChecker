namespace DriverHealthChecker.App;

public enum DriverCategory
{
    Unknown = 0,
    Gpu,
    Network,
    Storage,
    AudioMain,
    AudioExternal,
    DeviceRecommendation,
    HiddenSystem
}

public enum DriverHealthStatus
{
    Unknown = 0,
    UpToDate,
    NeedsReview,
    NeedsAttention,
    RecentlyUpdated,
    Hidden,
    Recommendation
}

public static class DriverTextMapper
{
    public static string ToCategoryCode(DriverCategory category)
    {
        return category switch
        {
            DriverCategory.Gpu => "GPU",
            DriverCategory.Network => "Network",
            DriverCategory.Storage => "Storage",
            DriverCategory.AudioMain => "AudioMain",
            DriverCategory.AudioExternal => "AudioExternal",
            DriverCategory.DeviceRecommendation => "DeviceRecommendation",
            DriverCategory.HiddenSystem => "HiddenSystem",
            _ => string.Empty
        };
    }

    public static string ToCategoryDisplay(DriverCategory category)
    {
        return category switch
        {
            DriverCategory.Gpu => "GPU",
            DriverCategory.Network => "Сеть",
            DriverCategory.Storage => "Хранение",
            DriverCategory.AudioMain => "Аудио",
            DriverCategory.AudioExternal => "Аудиокарта",
            DriverCategory.DeviceRecommendation => "Рекомендация",
            DriverCategory.HiddenSystem => "Скрытые",
            _ => string.Empty
        };
    }

    public static DriverCategory ParseCategoryCode(string? category)
    {
        return category switch
        {
            "GPU" => DriverCategory.Gpu,
            "Network" => DriverCategory.Network,
            "Storage" => DriverCategory.Storage,
            "AudioMain" => DriverCategory.AudioMain,
            "AudioExternal" => DriverCategory.AudioExternal,
            "DeviceRecommendation" => DriverCategory.DeviceRecommendation,
            "HiddenSystem" => DriverCategory.HiddenSystem,
            _ => DriverCategory.Unknown
        };
    }

    public static string ToStatusDisplay(DriverHealthStatus status)
    {
        return status switch
        {
            DriverHealthStatus.UpToDate => "Актуален",
            DriverHealthStatus.NeedsReview => "Стоит проверить",
            DriverHealthStatus.NeedsAttention => "Требует внимания",
            DriverHealthStatus.RecentlyUpdated => "Недавно обновлён",
            DriverHealthStatus.Hidden => "Скрыт",
            DriverHealthStatus.Recommendation => "Рекомендация",
            _ => string.Empty
        };
    }

    public static DriverHealthStatus ParseStatusDisplay(string? status)
    {
        return status switch
        {
            "Актуален" => DriverHealthStatus.UpToDate,
            "Стоит проверить" => DriverHealthStatus.NeedsReview,
            "Требует внимания" => DriverHealthStatus.NeedsAttention,
            "Недавно обновлён" => DriverHealthStatus.RecentlyUpdated,
            "Скрыт" => DriverHealthStatus.Hidden,
            "Рекомендация" => DriverHealthStatus.Recommendation,
            _ => DriverHealthStatus.Unknown
        };
    }
}
