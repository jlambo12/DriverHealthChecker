using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverClassifierKpiTests
{
    private readonly IDriverClassifier _classifier = new DriverClassifier();

    [Fact]
    public void FixtureKpi_ByCategory_ShouldMeetRoadmapThresholds()
    {
        var fixtures = LoadFixtures();

        var thresholds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["GPU"] = 0.99,
            ["Network"] = 0.95,
            ["Storage"] = 0.95,
            ["AudioMain"] = 0.95,
            ["AudioExternal"] = 0.90
        };

        foreach (var pair in thresholds)
        {
            var category = pair.Key;
            var requiredAccuracy = pair.Value;

            var categoryCases = fixtures
                .Where(x => x.ShouldClassify && string.Equals(x.ExpectedCategory, category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.NotEmpty(categoryCases);

            var passed = categoryCases.Count(c =>
                _classifier.TryClassify(c.Name, c.Manufacturer, out var predictedCategory, out _) &&
                string.Equals(predictedCategory, c.ExpectedCategory, StringComparison.OrdinalIgnoreCase));

            var accuracy = (double)passed / categoryCases.Count;
            Assert.True(
                accuracy >= requiredAccuracy,
                $"Категория '{category}' не прошла KPI: {accuracy:P2}, требование: {requiredAccuracy:P2}");
        }
    }

    private static List<DriverFixtureCase> LoadFixtures()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "driver-classification-fixtures.json");
        var json = File.ReadAllText(fixturePath);
        return JsonSerializer.Deserialize<List<DriverFixtureCase>>(json) ?? new List<DriverFixtureCase>();
    }
}
