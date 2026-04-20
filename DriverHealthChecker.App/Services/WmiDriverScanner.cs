using System;
using System.Collections.Generic;
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
            using var searcher = new ManagementObjectSearcher("SELECT DeviceName, Manufacturer, DriverVersion, DriverDate FROM Win32_PnPSignedDriver");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["DeviceName"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                result.Add(new ScannedDriverRecord
                {
                    Name = name,
                    Manufacturer = obj["Manufacturer"]?.ToString(),
                    Version = obj["DriverVersion"]?.ToString(),
                    RawDate = obj["DriverDate"]?.ToString()
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
}
