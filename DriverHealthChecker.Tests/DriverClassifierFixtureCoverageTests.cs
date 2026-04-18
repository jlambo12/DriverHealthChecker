using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverClassifierFixtureCoverageTests
{
    private static readonly JsonSerializerOptions FixtureJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Fixtures_ShouldCoverRequiredHardwareProfiles()
    {
        var fixtures = LoadFixtures();

        var requiredProfiles = new[]
        {
            "intel_nvidia",
            "amd_amd",
            "intel_igpu",
            "oem_laptop",
            "external_audio_pc"
        };

        foreach (var profile in requiredProfiles)
        {
            Assert.Contains(fixtures, f => string.Equals(f.Profile, profile, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Fixtures_ShouldContainAtLeastFiveNegativeCases()
    {
        var fixtures = LoadFixtures();
        var negatives = fixtures.Count(f => !f.ShouldClassify);

        Assert.True(negatives >= 5, $"Недостаточно негативных кейсов: {negatives}");
    }

    private static List<DriverFixtureCase> LoadFixtures()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "driver-classification-fixtures.json");
        var json = File.ReadAllText(fixturePath);
        return JsonSerializer.Deserialize<List<DriverFixtureCase>>(json, FixtureJsonOptions) ?? new List<DriverFixtureCase>();
    }
}
