using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace DriverHealthChecker.App;

internal interface IWmiDriverScanner
{
    OperationResult<List<ScannedDriverRecord>> ScanSignedDrivers();
}

internal sealed class WmiDriverScanner : IWmiDriverScanner
{
    public OperationResult<List<ScannedDriverRecord>> ScanSignedDrivers()
    {
        var result = new List<ScannedDriverRecord>();
        AppLogger.Info("WMI scan started (Win32_PnPSignedDriver).");

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceName, Manufacturer, DriverVersion, DriverDate, DeviceID, HardWareID, CompatID, DriverProviderName, InfName, Signer, DeviceClass, ClassGuid FROM Win32_PnPSignedDriver");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = NormalizeDisplayText(obj["DeviceName"]?.ToString());
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                result.Add(new ScannedDriverRecord
                {
                    Name = name,
                    Manufacturer = NormalizeDisplayText(obj["Manufacturer"]?.ToString()),
                    Version = obj["DriverVersion"]?.ToString(),
                    RawDate = obj["DriverDate"]?.ToString(),
                    PnpDeviceId = NormalizeOptionalValue(obj["DeviceID"]?.ToString()),
                    HardwareIds = ExtractStringList(obj["HardWareID"]),
                    CompatibleIds = ExtractStringList(obj["CompatID"]),
                    DriverProviderName = NormalizeOptionalValue(obj["DriverProviderName"]?.ToString()),
                    DriverInfName = NormalizeOptionalValue(obj["InfName"]?.ToString()),
                    DriverSignerName = NormalizeOptionalValue(obj["Signer"]?.ToString()),
                    DriverClass = NormalizeOptionalValue(obj["DeviceClass"]?.ToString()),
                    ClassGuid = NormalizeOptionalValue(obj["ClassGuid"]?.ToString())
                });
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Ошибка во время WMI-сканирования Win32_PnPSignedDriver.", ex);
            return OperationResult<List<ScannedDriverRecord>>.Failure(ex.Message);
        }

        AppLogger.Info($"WMI scan completed. records={result.Count}.");
        return OperationResult<List<ScannedDriverRecord>>.Success(result);
    }

    private static string NormalizeDisplayText(string? value)
    {
        return NormalizeOptionalValue(value) ?? string.Empty;
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static List<string> ExtractStringList(object? rawValue)
    {
        if (rawValue == null)
            return new List<string>();

        if (rawValue is string[] values)
        {
            return values
                .Select(NormalizeOptionalValue)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
        }

        var singleValue = NormalizeOptionalValue(rawValue.ToString());
        return string.IsNullOrWhiteSpace(singleValue)
            ? new List<string>()
            : new List<string> { singleValue };
    }
}
