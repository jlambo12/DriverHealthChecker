using System;

namespace DriverHealthChecker.App;

internal interface IOfficialActionResolver
{
    OfficialAction Resolve(string name, string? manufacturer, DriverCategory category, string? oemManufacturer = null, bool isLaptop = false);
}

internal sealed class OfficialActionResolver : IOfficialActionResolver
{
    private readonly INvidiaAppLocator _nvidiaAppLocator;

    public OfficialActionResolver()
        : this(new NvidiaAppLocator())
    {
    }

    internal OfficialActionResolver(INvidiaAppLocator nvidiaAppLocator)
    {
        _nvidiaAppLocator = nvidiaAppLocator;
    }

    public OfficialAction Resolve(string name, string? manufacturer, DriverCategory category, string? oemManufacturer = null, bool isLaptop = false)
    {
        var n = name.ToLowerInvariant();
        var m = (manufacturer ?? string.Empty).ToLowerInvariant();
        var oem = (oemManufacturer ?? string.Empty).ToLowerInvariant();

        var action = ResolveGpuAction(category, n)
                     ?? ResolveNetworkAction(category, n, m)
                     ?? ResolveStorageAction(category, n, m, oem, isLaptop)
                     ?? ResolveAudioAction(category, n, m, oem, isLaptop)
                     ?? ResolveDeviceRecommendationAction(category, oem);

        if (action != null)
            return action;

        AppLogger.Info($"Official action fallback used. category={DriverTextMapper.ToCategoryCode(category)}, name={name}, manufacturer={manufacturer ?? "-"}.");
        return OfficialAction.ForMessage(
            "OEM/вендор",
            "Для этого устройства точный официальный источник пока не определён. Используйте OEM-поддержку устройства или сайт производителя устройства.",
            "Показать безопасную рекомендацию");
    }

    private OfficialAction? ResolveGpuAction(DriverCategory category, string normalizedName)
    {
        if (category == DriverCategory.Gpu && (normalizedName.Contains("nvidia") || normalizedName.Contains("geforce")))
        {
            var appPath = _nvidiaAppLocator.FindInstalledAppPath();
            if (!string.IsNullOrWhiteSpace(appPath))
                return OfficialAction.ForLocalApp(appPath, "NVIDIA App", "Открыть установленное приложение NVIDIA");

            return OfficialAction.ForUrl(
                DriverRules.NvidiaAppUrl,
                "Скачать NVIDIA App",
                "Открыть официальный сайт для установки NVIDIA App");
        }

        if (category == DriverCategory.Gpu && normalizedName.Contains("radeon"))
        {
            return OfficialAction.ForUrl(
                DriverRules.AmdDriversUrl,
                "Сайт AMD",
                "Открыть официальный сайт AMD");
        }

        return null;
    }

    private static OfficialAction? ResolveNetworkAction(DriverCategory category, string normalizedName, string normalizedManufacturer)
    {
        if (category == DriverCategory.Network && normalizedManufacturer.Contains("intel"))
        {
            if (normalizedName.Contains("bluetooth"))
                return OfficialAction.ForUrl(DriverRules.IntelBluetoothDriversUrl, "Intel Bluetooth", "Открыть официальный драйвер Intel Bluetooth");

            if (normalizedName.Contains("wi-fi") || normalizedName.Contains("wireless") || normalizedName.Contains("wlan"))
                return OfficialAction.ForUrl(DriverRules.IntelWirelessDriversUrl, "Intel Wi-Fi", "Открыть официальный драйвер Intel Wi-Fi");

            return OfficialAction.ForUrl(
                DriverRules.IntelSupportAssistantUrl,
                "Intel Tool",
                "Открыть Intel Driver & Support Assistant");
        }

        if (category == DriverCategory.Network && (normalizedManufacturer.Contains("realtek") || normalizedName.Contains("realtek")))
        {
            return OfficialAction.ForUrl(
                DriverRules.RealtekDownloadsUrl,
                "Realtek Downloads",
                "Открыть официальный центр загрузок Realtek");
        }

        return null;
    }

    private static OfficialAction? ResolveStorageAction(DriverCategory category, string normalizedName, string normalizedManufacturer, string normalizedOemManufacturer, bool isLaptop)
    {
        if (category != DriverCategory.Storage)
            return null;

        if (normalizedManufacturer.Contains("intel") || normalizedName.Contains("intel") || normalizedName.Contains("rst") || normalizedName.Contains("vmd"))
        {
            return OfficialAction.ForUrl(
                DriverRules.IntelRstDriversUrl,
                "Intel RST",
                "Открыть официальный драйвер Intel Rapid Storage Technology");
        }

        if (IsOemLaptopAudioOrSupportComponent(normalizedName, normalizedOemManufacturer, isLaptop))
        {
            return OfficialAction.ForUrl(
                DriverRules.HuaweiPcManagerUrl,
                "Huawei PC Manager",
                "Для OEM-ноутбука безопаснее использовать Huawei PC Manager");
        }

        return OfficialAction.ForMessage(
            "OEM/вендор",
            "Для этого контроллера надёжнее использовать поддержку OEM устройства или официальную страницу производителя контроллера.",
            "Показать безопасную рекомендацию без универсального перенаправления");
    }

    private static OfficialAction? ResolveAudioAction(DriverCategory category, string normalizedName, string normalizedManufacturer, string normalizedOemManufacturer, bool isLaptop)
    {
        if (category != DriverCategory.AudioMain && category != DriverCategory.AudioExternal)
            return null;

        if (IsOemLaptopAudioOrSupportComponent(normalizedName, normalizedOemManufacturer, isLaptop))
        {
            return OfficialAction.ForUrl(
                DriverRules.HuaweiPcManagerUrl,
                "Huawei PC Manager",
                "Для OEM-зависимого аудио используйте инструмент производителя ноутбука");
        }

        if (normalizedManufacturer.Contains("realtek") || normalizedName.Contains("realtek"))
        {
            return OfficialAction.ForUrl(
                DriverRules.RealtekDownloadsUrl,
                "Realtek Downloads",
                "Открыть официальный центр загрузок Realtek");
        }

        return OfficialAction.ForMessage(
            "OEM/вендор",
            "Для аудио-драйверов используйте OEM-поддержку устройства или официальный сайт производителя аудиочипа.",
            "Показать безопасную рекомендацию по обновлению");
    }

    private static OfficialAction? ResolveDeviceRecommendationAction(DriverCategory category, string normalizedOemManufacturer)
    {
        if (category == DriverCategory.DeviceRecommendation && (normalizedOemManufacturer.Contains("huawei") || normalizedOemManufacturer.Contains("honor")))
            return OfficialAction.ForUrl(DriverRules.HuaweiPcManagerUrl, "Huawei PC Manager", "Открыть официальный инструмент Huawei");

        return null;
    }

    private static bool IsOemLaptopAudioOrSupportComponent(string name, string oemManufacturer, bool isLaptop)
    {
        if (!isLaptop)
            return false;

        var isHuawei = oemManufacturer.Contains("huawei") || oemManufacturer.Contains("honor");
        if (!isHuawei)
            return false;

        return name.Contains("huawei audio service")
               || name.Contains("hwve")
               || name.Contains("nahimic")
               || name.Contains("elan")
               || name.Contains("smbus");
    }

}
