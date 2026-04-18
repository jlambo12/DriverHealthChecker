using System;
using System.Linq;
using System.Management;

namespace DriverHealthChecker.App;

internal sealed record DeviceProfile(string Manufacturer, string Model, bool IsLaptop);

internal interface IDeviceProfileDetector
{
    DeviceProfile? TryGetDeviceProfile();
}

internal sealed class DeviceProfileDetector : IDeviceProfileDetector
{
    public DeviceProfile? TryGetDeviceProfile()
    {
        try
        {
            var manufacturer = string.Empty;
            var model = string.Empty;

            using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model, PCSystemType FROM Win32_ComputerSystem"))
            {
                var first = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (first == null)
                    return null;

                manufacturer = first["Manufacturer"]?.ToString() ?? string.Empty;
                model = first["Model"]?.ToString() ?? string.Empty;

                if (int.TryParse(first["PCSystemType"]?.ToString(), out var pcSystemType))
                {
                    if (pcSystemType == 2)
                        return new DeviceProfile(manufacturer, model, true);
                }
            }

            using var enclosureSearcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure");
            foreach (ManagementObject obj in enclosureSearcher.Get())
            {
                if (obj["ChassisTypes"] is not ushort[] chassisTypes)
                    continue;

                if (chassisTypes.Any(IsLaptopChassisType))
                    return new DeviceProfile(manufacturer, model, true);
            }

            return new DeviceProfile(manufacturer, model, false);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Не удалось определить тип устройства (ноутбук/ПК).", ex);
            return null;
        }
    }

    private static bool IsLaptopChassisType(ushort chassisType)
    {
        return chassisType is 8 or 9 or 10 or 14;
    }
}

internal interface ILaptopOemRecommendationResolver
{
    OfficialAction Resolve(string manufacturer, string model);
}

internal sealed class LaptopOemRecommendationResolver : ILaptopOemRecommendationResolver
{
    public OfficialAction Resolve(string manufacturer, string model)
    {
        var m = (manufacturer ?? string.Empty).ToLowerInvariant();
        var displayModel = string.IsNullOrWhiteSpace(model) ? string.Empty : $" ({model})";
        var matchedRule = DriverRules.LaptopOemRules
            .FirstOrDefault(rule => rule.ManufacturerKeywords.Any(m.Contains));

        if (matchedRule != null)
        {
            return OfficialAction.ForUrl(
                matchedRule.Url,
                matchedRule.ButtonText,
                $"Рекомендуется для ноутбуков {matchedRule.DisplayVendor}{displayModel}");
        }

        return OfficialAction.ForSearch(
            $"{manufacturer} {model} laptop support utility",
            "Найти OEM-софт",
            "Открыть поиск официального OEM-приложения для ноутбука");
    }
}
