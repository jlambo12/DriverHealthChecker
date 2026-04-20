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
    private static readonly JsonSerializerOptions FixtureJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void FixtureKpi_ByCategory_ShouldMeetRoadmapThresholds()
    {
        var fixtures = LoadFixtures();

        var thresholds = new Dictionary<DriverCategory, double>
        {
            [DriverCategory.Gpu] = 0.99,
            [DriverCategory.Network] = 0.95,
            [DriverCategory.Storage] = 0.95,
            [DriverCategory.AudioMain] = 0.95,
            [DriverCategory.AudioExternal] = 0.90
        };

        foreach (var pair in thresholds)
        {
            var category = pair.Key;
            var requiredAccuracy = pair.Value;

            var categoryCases = fixtures
                .Where(x => x.ShouldClassify && DriverTextMapper.ParseCategoryCode(x.ExpectedCategory) == category)
                .ToList();

            Assert.NotEmpty(categoryCases);

            var passed = categoryCases.Count(c =>
                _classifier.TryClassify(c.Name, c.Manufacturer, out var predictedCategory, out _) &&
                predictedCategory == DriverTextMapper.ParseCategoryCode(c.ExpectedCategory));

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
        return JsonSerializer.Deserialize<List<DriverFixtureCase>>(json, FixtureJsonOptions) ?? new List<DriverFixtureCase>();
    }
}
