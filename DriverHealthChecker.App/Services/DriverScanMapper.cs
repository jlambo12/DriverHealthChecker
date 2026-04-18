using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace DriverHealthChecker.App;

internal interface IDriverScanMapper
{
    DriverScanBuildResult Build(IReadOnlyList<ScannedDriverRecord> records, DeviceProfile? profile);
}

internal sealed class DriverScanMapper : IDriverScanMapper
{
    private readonly IDriverClassifier _driverClassifier;
    private readonly IOfficialActionResolver _officialActionResolver;
    private readonly IDriverSelectionService _driverSelectionService;

    public DriverScanMapper(
        IDriverClassifier driverClassifier,
        IOfficialActionResolver officialActionResolver,
        IDriverSelectionService driverSelectionService)
    {
        _driverClassifier = driverClassifier;
        _officialActionResolver = officialActionResolver;
        _driverSelectionService = driverSelectionService;
    }

    public DriverScanBuildResult Build(IReadOnlyList<ScannedDriverRecord> records, DeviceProfile? profile)
    {
        var allDrivers = new List<DriverItem>();
        var hiddenDrivers = new List<DriverItem>();
        var mappedCount = 0;
        var skippedCount = 0;

        foreach (var record in records)
        {
            try
            {
                if (!_driverClassifier.TryClassify(record.Name, record.Manufacturer, out var category, out var reason))
                {
                    if (!string.IsNullOrWhiteSpace(reason))
                        hiddenDrivers.Add(BuildHiddenItem(record, reason));
                    skippedCount++;

                    continue;
                }

                var action = _officialActionResolver.Resolve(
                    record.Name,
                    record.Manufacturer,
                    category,
                    profile?.Manufacturer,
                    profile?.IsLaptop == true);

                allDrivers.Add(new DriverItem
                {
                    Name = CleanDeviceName(record.Name),
                    Manufacturer = CleanManufacturer(record.Manufacturer),
                    Version = string.IsNullOrWhiteSpace(record.Version) ? "-" : record.Version,
                    Date = FormatDate(record.RawDate),
                    Category = category,
                    CategoryDisplay = GetCategoryDisplay(category),
                    Status = "Стоит проверить",
                    OfficialAction = action,
                    ButtonText = action.ButtonText,
                    DetectionReason = reason,
                    ButtonTooltip = $"{action.Tooltip} · Причина: {reason}"
                });
                mappedCount++;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Не удалось сопоставить драйвер в DriverScanMapper.", ex);
            }
        }

        var selected = _driverSelectionService.SelectBestDrivers(allDrivers);
        AppLogger.Info($"DriverScanMapper completed. source={records.Count}, mapped={mappedCount}, skipped={skippedCount}, selected={selected.Count}, hidden={hiddenDrivers.Count}.");

        return new DriverScanBuildResult
        {
            SelectedDrivers = selected,
            HiddenDrivers = hiddenDrivers.OrderBy(d => d.Name).ToList()
        };
    }

    private static DriverItem BuildHiddenItem(ScannedDriverRecord record, string reason)
    {
        return new DriverItem
        {
            Name = CleanDeviceName(record.Name),
            Manufacturer = CleanManufacturer(record.Manufacturer),
            Version = string.IsNullOrWhiteSpace(record.Version) ? "-" : record.Version,
            Date = FormatDate(record.RawDate),
            Category = "HiddenSystem",
            CategoryDisplay = GetCategoryDisplay("HiddenSystem"),
            Status = "Скрыт",
            DetectionReason = reason,
            OfficialAction = OfficialAction.ForMessage(
                "Почему скрыто",
                "Это устройство скрыто из основного списка, чтобы уменьшить шум.",
                reason),
            ButtonText = "Почему скрыто",
            ButtonTooltip = reason
        };
    }

    private static string GetCategoryDisplay(string category)
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

    private static string CleanDeviceName(string name) => name.Trim();

    private static string CleanManufacturer(string? manufacturer)
    {
        if (string.IsNullOrWhiteSpace(manufacturer))
            return "-";

        return manufacturer.Replace("Corporation", string.Empty)
            .Replace("(Standard system devices)", string.Empty)
            .Trim();
    }

    private static string FormatDate(string? rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate))
            return "-";

        try
        {
            var date = ManagementDateTimeConverter.ToDateTime(rawDate);
            return date.ToString("yyyy-MM-dd");
        }
        catch
        {
            return rawDate;
        }
    }
}
