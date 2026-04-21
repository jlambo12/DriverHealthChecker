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
    private readonly DriverVerifierRegistry? _driverVerifierRegistry;

    public DriverScanMapper(
        IDriverClassifier driverClassifier,
        IOfficialActionResolver officialActionResolver,
        IDriverSelectionService driverSelectionService,
        DriverVerifierRegistry? driverVerifierRegistry = null)
    {
        _driverClassifier = driverClassifier;
        _officialActionResolver = officialActionResolver;
        _driverSelectionService = driverSelectionService;
        _driverVerifierRegistry = driverVerifierRegistry;
    }

    public DriverScanBuildResult Build(IReadOnlyList<ScannedDriverRecord> records, DeviceProfile? profile)
    {
        var allDrivers = new List<DriverItem>();
        var hiddenDrivers = new List<DriverItem>();
        var verificationObservations = new List<DriverVerificationObservation>();
        var mappedCount = 0;
        var skippedCount = 0;

        foreach (var record in records)
        {
            try
            {
                var normalizedName = NormalizeDeviceName(record.Name);
                var normalizedManufacturer = NormalizeManufacturer(record.Manufacturer);

                if (!_driverClassifier.TryClassify(normalizedName, normalizedManufacturer, out var category, out var reason))
                {
                    if (!string.IsNullOrWhiteSpace(reason))
                        hiddenDrivers.Add(BuildHiddenItem(record, reason));
                    skippedCount++;

                    continue;
                }

                var action = _officialActionResolver.Resolve(
                    normalizedName,
                    normalizedManufacturer,
                    category,
                    profile?.Manufacturer,
                    profile?.IsLaptop == true);

                RunShadowVerification(record, normalizedName, normalizedManufacturer, verificationObservations);

                allDrivers.Add(new DriverItem
                {
                    Name = CleanDeviceName(normalizedName),
                    Manufacturer = CleanManufacturer(normalizedManufacturer),
                    Version = string.IsNullOrWhiteSpace(record.Version) ? "-" : record.Version,
                    Date = FormatDate(record.RawDate),
                    CategoryKind = category,
                    StatusKind = DriverHealthStatus.NeedsReview,
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
            HiddenDrivers = hiddenDrivers.OrderBy(d => d.Name).ToList(),
            VerificationObservations = verificationObservations
        };
    }

    private void RunShadowVerification(
        ScannedDriverRecord record,
        string normalizedName,
        string normalizedManufacturer,
        List<DriverVerificationObservation> verificationObservations)
    {
        if (_driverVerifierRegistry == null)
            return;

        var identity = BuildDriverIdentity(record, normalizedName, normalizedManufacturer);

        try
        {
            var verificationResult = _driverVerifierRegistry.Verify(identity);
            verificationObservations.Add(new DriverVerificationObservation
            {
                DriverName = identity.DisplayName,
                Manufacturer = identity.Manufacturer,
                Result = verificationResult
            });

            AppLogger.Info(
                $"Shadow verification completed. device={identity.DisplayName}, source={verificationResult.SourceDetails}, status={verificationResult.Status}, failure={verificationResult.FailureReasonType?.ToString() ?? "None"}.");
        }
        catch (Exception ex)
        {
            AppLogger.Error(
                $"Shadow verification failed for device={identity.DisplayName}.",
                ex);
        }
    }

    private static DriverIdentity BuildDriverIdentity(
        ScannedDriverRecord record,
        string normalizedName,
        string normalizedManufacturer)
    {
        return new DriverIdentity
        {
            DisplayName = CleanDeviceName(normalizedName),
            NormalizedName = normalizedName,
            InstalledVersion = record.Version,
            Manufacturer = CleanManufacturer(normalizedManufacturer),
            NormalizedManufacturer = normalizedManufacturer,
            PnpDeviceId = record.PnpDeviceId,
            HardwareIds = new List<string>(record.HardwareIds),
            CompatibleIds = new List<string>(record.CompatibleIds),
            DriverProviderName = record.DriverProviderName,
            DriverInfName = record.DriverInfName,
            DriverSignerName = record.DriverSignerName,
            DriverClass = record.DriverClass,
            ClassGuid = record.ClassGuid
        };
    }

    private static DriverItem BuildHiddenItem(ScannedDriverRecord record, string reason)
    {
        var normalizedName = NormalizeDeviceName(record.Name);
        var normalizedManufacturer = NormalizeManufacturer(record.Manufacturer);

        return new DriverItem
        {
            Name = CleanDeviceName(normalizedName),
            Manufacturer = CleanManufacturer(normalizedManufacturer),
            Version = string.IsNullOrWhiteSpace(record.Version) ? "-" : record.Version,
            Date = FormatDate(record.RawDate),
            CategoryKind = DriverCategory.HiddenSystem,
            StatusKind = DriverHealthStatus.Hidden,
            DetectionReason = reason,
            OfficialAction = OfficialAction.ForMessage(
                "Почему скрыто",
                "Это устройство скрыто из основного списка, чтобы уменьшить шум.",
                reason),
            ButtonText = "Почему скрыто",
            ButtonTooltip = reason
        };
    }

    private static string CleanDeviceName(string name) => NormalizeDeviceName(name);

    private static string CleanManufacturer(string? manufacturer)
    {
        var normalizedManufacturer = NormalizeManufacturer(manufacturer);
        if (string.IsNullOrWhiteSpace(normalizedManufacturer))
            return "-";

        return normalizedManufacturer.Replace("Corporation", string.Empty)
            .Replace("(Standard system devices)", string.Empty)
            .Trim();
    }

    private static string NormalizeDeviceName(string? name)
    {
        return NormalizeBasicText(name);
    }

    private static string NormalizeManufacturer(string? manufacturer)
    {
        return NormalizeBasicText(manufacturer);
    }

    private static string NormalizeBasicText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
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
