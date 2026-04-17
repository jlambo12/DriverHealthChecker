using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class ScanReportWriterTests
{
    [Fact]
    public void TryWrite_ShouldCreateJsonReportFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "dhc-tests", Guid.NewGuid().ToString("N"));
        var writer = new ScanReportWriter(() => tempDir);

        var drivers = new List<DriverItem>
        {
            new()
            {
                Name = "Intel(R) Wi-Fi 6E AX211",
                Manufacturer = "Intel",
                Category = "Network",
                CategoryDisplay = "Сеть",
                Status = "Стоит проверить",
                DetectionReason = "Сеть: ключевое слово 'wi-fi'",
                OfficialAction = OfficialAction.ForMessage("Открыть", "msg", "tip")
            }
        };

        var reportPath = writer.TryWrite(drivers, isRescan: false, deviceKind: "Laptop");

        Assert.False(string.IsNullOrWhiteSpace(reportPath));
        Assert.True(File.Exists(reportPath));

        var json = File.ReadAllText(reportPath!);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("GeneratedAt", out _));
        Assert.Equal(1, doc.RootElement.GetProperty("Total").GetInt32());
        Assert.False(doc.RootElement.GetProperty("IsRescan").GetBoolean());
        Assert.Equal("Laptop", doc.RootElement.GetProperty("DeviceKind").GetString());
        Assert.True(doc.RootElement.TryGetProperty("AppVersion", out _));
        Assert.Equal(1, doc.RootElement.GetProperty("CategorySummary").GetProperty("Сеть").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("StatusSummary").GetProperty("Стоит проверить").GetInt32());
        Assert.Equal("Intel(R) Wi-Fi 6E AX211", doc.RootElement.GetProperty("Items")[0].GetProperty("Name").GetString());
    }
}
