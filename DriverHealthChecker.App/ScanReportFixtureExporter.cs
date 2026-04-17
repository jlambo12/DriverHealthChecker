using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DriverHealthChecker.App;

internal interface IScanReportFixtureExporter
{
    int Export(string reportsDirectory, string outputFilePath);
}

internal sealed class ScanReportFixtureExporter : IScanReportFixtureExporter
{
    private readonly IFixtureTemplateBuilder _fixtureTemplateBuilder;

    public ScanReportFixtureExporter(IFixtureTemplateBuilder fixtureTemplateBuilder)
    {
        _fixtureTemplateBuilder = fixtureTemplateBuilder;
    }

    public int Export(string reportsDirectory, string outputFilePath)
    {
        if (!Directory.Exists(reportsDirectory))
            return 0;

        var reportFiles = Directory.GetFiles(reportsDirectory, "scan-*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();

        if (!reportFiles.Any())
            return 0;

        var allItems = new List<DriverItem>();

        foreach (var file in reportFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var payload = JsonSerializer.Deserialize<ScanReportPayload>(json);
                if (payload?.Items != null)
                    allItems.AddRange(payload.Items);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Не удалось прочитать scan-report файл: {file}", ex);
            }
        }

        var fixtureTemplate = _fixtureTemplateBuilder.Build(allItems);
        if (!fixtureTemplate.Any())
            return 0;

        var outputDir = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrWhiteSpace(outputDir))
            Directory.CreateDirectory(outputDir);

        try
        {
            var outputJson = JsonSerializer.Serialize(fixtureTemplate, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputFilePath, outputJson);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Не удалось записать fixture-output файл: {outputFilePath}", ex);
            return 0;
        }

        return fixtureTemplate.Count;
    }

    private sealed class ScanReportPayload
    {
        public IReadOnlyCollection<DriverItem>? Items { get; init; }
    }
}
