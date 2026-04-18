using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DriverHealthChecker.App;

internal interface IScanReportWriter
{
    string? TryWrite(IReadOnlyCollection<DriverItem> drivers, bool isRescan, string? deviceKind = null);
}

internal sealed class ScanReportWriter : IScanReportWriter
{
    private readonly Func<string> _baseDirProvider;

    public ScanReportWriter()
        : this(() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DriverHealthChecker",
            "scan-history"))
    {
    }

    internal ScanReportWriter(Func<string> baseDirProvider)
    {
        _baseDirProvider = baseDirProvider;
    }

    public string? TryWrite(IReadOnlyCollection<DriverItem> drivers, bool isRescan, string? deviceKind = null)
    {
        try
        {
            var baseDir = _baseDirProvider();
            AppLogger.Info($"Scan report write started. baseDir={baseDir}, drivers={drivers.Count}, isRescan={isRescan}.");

            Directory.CreateDirectory(baseDir);

            var fileName = $"scan-{DateTime.Now:yyyyMMdd-HHmmssfff}-{(isRescan ? "rescan" : "scan")}.json";
            var fullPath = Path.Combine(baseDir, fileName);

            var payload = new ScanReportPayload
            {
                GeneratedAt = DateTime.Now,
                IsRescan = isRescan,
                DeviceKind = deviceKind ?? "Unknown",
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                Total = drivers.Count,
                CategorySummary = drivers
                    .GroupBy(d => d.CategoryDisplay)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase),
                StatusSummary = drivers
                    .GroupBy(d => d.Status)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase),
                Items = drivers
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(fullPath, json);
            AppLogger.Info($"Scan report write completed. path={fullPath}.");
            return fullPath;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Не удалось сохранить JSON-отчет сканирования.", ex);
            return null;
        }
    }

    private sealed class ScanReportPayload
    {
        public DateTime GeneratedAt { get; init; }
        public bool IsRescan { get; init; }
        public string DeviceKind { get; init; } = "Unknown";
        public string AppVersion { get; init; } = "Unknown";
        public int Total { get; init; }
        public Dictionary<string, int> CategorySummary { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> StatusSummary { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyCollection<DriverItem> Items { get; init; } = Array.Empty<DriverItem>();
    }
}
