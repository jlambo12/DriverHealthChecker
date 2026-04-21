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
    private readonly IDriverIdentityTokenExtractor _driverIdentityTokenExtractor;
    private int _validationTotal;
    private int _validationMatch;
    private int _validationMismatch;

    internal int ValidationTotalCount => _validationTotal;
    internal int ValidationMatchCount => _validationMatch;
    internal int ValidationMismatchCount => _validationMismatch;

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
        _driverIdentityTokenExtractor = new DriverIdentityTokenExtractor();
    }

    public DriverScanBuildResult Build(IReadOnlyList<ScannedDriverRecord> records, DeviceProfile? profile)
    {
        _validationTotal = 0;
        _validationMatch = 0;
        _validationMismatch = 0;

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

                var driverItem = new DriverItem
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
                };

                RunShadowVerification(record, driverItem, normalizedName, normalizedManufacturer, verificationObservations);

                allDrivers.Add(driverItem);
                mappedCount++;
            }
            catch (Exception ex)
            {
                SafeLogError("Не удалось сопоставить драйвер в DriverScanMapper.", ex);
            }
        }

        var selected = _driverSelectionService.SelectBestDrivers(allDrivers);
        SafeLogInfo($"DriverScanMapper completed. source={records.Count}, mapped={mappedCount}, skipped={skippedCount}, selected={selected.Count}, hidden={hiddenDrivers.Count}.");
        LogVerificationAggregate(allDrivers.Count, verificationObservations.Count);

        return new DriverScanBuildResult
        {
            SelectedDrivers = selected,
            HiddenDrivers = hiddenDrivers.OrderBy(d => d.Name).ToList(),
            VerificationObservations = verificationObservations
        };
    }

    private void RunShadowVerification(
        ScannedDriverRecord record,
        DriverItem driverItem,
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
            _driverIdentityTokenExtractor.TryExtract(identity, out var tokens);
            var observation = new DriverVerificationObservation
            {
                DriverKey = BuildDriverKey(driverItem),
                DriverName = identity.DisplayName,
                Manufacturer = identity.Manufacturer,
                VendorId = string.IsNullOrWhiteSpace(tokens.VendorId) ? null : tokens.VendorId,
                DeviceId = string.IsNullOrWhiteSpace(tokens.DeviceId) ? null : tokens.DeviceId,
                Result = verificationResult,
                LegacyStatus = driverItem.StatusKind,
                VerificationStatus = verificationResult.Status
            };

            ValidateObservation(observation);
            verificationObservations.Add(observation);
        }
        catch (Exception ex)
        {
            SafeLogError(
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

    private static string BuildDriverKey(DriverItem driverItem)
    {
        return $"{DriverTextMapper.ToCategoryCode(driverItem.CategoryKind)}|{driverItem.Name}";
    }

    private void ValidateObservation(DriverVerificationObservation observation)
    {
        try
        {
            _validationTotal++;
            observation.IsMatch = IsLogicalMatch(observation.LegacyStatus, observation.VerificationStatus);
            if (observation.IsMatch)
            {
                _validationMatch++;
                return;
            }

            _validationMismatch++;
            SafeLogInfo(
                $"Verification mismatch. vendorId={observation.VendorId ?? "-"}, deviceId={observation.DeviceId ?? "-"}, legacyStatus={observation.LegacyStatus}, verificationStatus={observation.VerificationStatus}.");
        }
        catch (Exception ex)
        {
            SafeLogError("Shadow verification validation failed.", ex);
        }
    }

    private void LogVerificationAggregate(int totalDrivers, int withVerification)
    {
        SafeLogInfo(
            $"Verification aggregate. totalDrivers={totalDrivers}, withVerification={withVerification}, mismatches={_validationMismatch}.");
    }

    private void SafeLogInfo(string message)
    {
        try
        {
            AppLogger.Info(message);
        }
        catch
        {
        }
    }

    private void SafeLogError(string message, Exception? ex = null)
    {
        try
        {
            AppLogger.Error(message, ex);
        }
        catch
        {
        }
    }

    private static bool IsLogicalMatch(DriverHealthStatus legacyStatus, DriverVerificationStatus verificationStatus)
    {
        return legacyStatus switch
        {
            DriverHealthStatus.UpToDate => verificationStatus == DriverVerificationStatus.UpToDate,
            DriverHealthStatus.NeedsAttention => verificationStatus == DriverVerificationStatus.UpdateAvailable,
            DriverHealthStatus.NeedsReview => verificationStatus == DriverVerificationStatus.UnableToVerifyReliably,
            _ => false
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
