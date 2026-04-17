using System;
using System.IO;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class ScanReportFixtureExporterTests
{
    [Fact]
    public void Export_WhenDirectoryMissing_ReturnsZero()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "dhc-fixture-export", Guid.NewGuid().ToString("N"));
        var reportsDir = Path.Combine(tempRoot, "missing");
        var outputFile = Path.Combine(tempRoot, "driver-fixtures.generated.json");

        var exporter = new ScanReportFixtureExporter(new FixtureTemplateBuilder());
        var exportedCount = exporter.Export(reportsDir, outputFile);

        Assert.Equal(0, exportedCount);
        Assert.False(File.Exists(outputFile));
    }

    [Fact]
    public void Export_WhenNoReports_ReturnsZero()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "dhc-fixture-export", Guid.NewGuid().ToString("N"));
        var reportsDir = Path.Combine(tempRoot, "scan-history");
        var outputFile = Path.Combine(tempRoot, "driver-fixtures.generated.json");
        Directory.CreateDirectory(reportsDir);

        var exporter = new ScanReportFixtureExporter(new FixtureTemplateBuilder());
        var exportedCount = exporter.Export(reportsDir, outputFile);

        Assert.Equal(0, exportedCount);
        Assert.False(File.Exists(outputFile));
    }

    [Fact]
    public void Export_WhenReportsExist_WritesFixtureTemplateFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "dhc-fixture-export", Guid.NewGuid().ToString("N"));
        var reportsDir = Path.Combine(tempRoot, "scan-history");
        var outputFile = Path.Combine(tempRoot, "driver-fixtures.generated.json");
        Directory.CreateDirectory(reportsDir);

        var reportJson = """
        {
          "GeneratedAt": "2026-01-01T10:00:00",
          "IsRescan": false,
          "Total": 2,
          "Items": [
            {
              "Name": "Intel(R) Wi-Fi 6E AX211",
              "Category": "Network",
              "Manufacturer": "Intel"
            },
            {
              "Name": "PCI Express Root Port",
              "Category": "HiddenSystem",
              "Manufacturer": "Intel"
            }
          ]
        }
        """;

        File.WriteAllText(Path.Combine(reportsDir, "scan-20260101-100000-scan.json"), reportJson);

        var exporter = new ScanReportFixtureExporter(new FixtureTemplateBuilder());
        var exportedCount = exporter.Export(reportsDir, outputFile);

        Assert.Equal(2, exportedCount);
        Assert.True(File.Exists(outputFile));

        var output = File.ReadAllText(outputFile);
        Assert.Contains("Intel(R) Wi-Fi 6E AX211", output);
        Assert.Contains("\"ShouldClassify\": false", output);
    }
}
