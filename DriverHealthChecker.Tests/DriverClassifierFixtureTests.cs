using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverClassifierFixtureTests
{
    private readonly IDriverClassifier _classifier = new DriverClassifier();
    private static readonly JsonSerializerOptions FixtureJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static TheoryData<DriverFixtureCase> Cases
    {
        get
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "driver-classification-fixtures.json");
            var json = File.ReadAllText(fixturePath);
            var parsed = JsonSerializer.Deserialize<List<DriverFixtureCase>>(json, FixtureJsonOptions) ?? new List<DriverFixtureCase>();

            var data = new TheoryData<DriverFixtureCase>();
            foreach (var item in parsed)
                data.Add(item);

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TryClassify_ShouldMatchFixture(DriverFixtureCase fixture)
    {
        var result = _classifier.TryClassify(fixture.Name, fixture.Manufacturer, out var category, out var reason);

        Assert.Equal(fixture.ShouldClassify, result);

        if (!fixture.ShouldClassify)
        {
            Assert.Equal(DriverCategory.Unknown, category);
            return;
        }

        Assert.Equal(DriverTextMapper.ParseCategoryCode(fixture.ExpectedCategory), category);
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void Fixture_ShouldContainAtLeastFifteenCases()
    {
        Assert.True(Cases.Count >= 15);
    }
}

public sealed class DriverFixtureCase
{
    public string Profile { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ExpectedCategory { get; set; } = string.Empty;
    public bool ShouldClassify { get; set; }
}
