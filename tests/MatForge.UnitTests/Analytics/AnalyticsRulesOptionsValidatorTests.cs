using FluentAssertions;
using MatForge.Application.Features.Analytics;
using Microsoft.Extensions.Configuration;

namespace MatForge.UnitTests.Analytics;

public class AnalyticsRulesOptionsValidatorTests
{
    private static readonly Dictionary<string, string?> SpecValues = new()
    {
        ["Training:MinSamples"] = "10",
        ["Training:MinFinishes"] = "5",
        ["Training:MinCount"] = "3",
        ["Training:TechniqueShare"] = "0.30",
        ["Training:PositionShare"] = "0.40",
        ["Training:TrendDelta"] = "0.10",
        ["Competition:MinSamples"] = "5",
        ["Competition:MinFinishes"] = "3",
        ["Competition:MinCount"] = "2",
        ["Competition:TechniqueShare"] = "0.30",
        ["Competition:TrendDelta"] = "0.10",
        ["GapDelta"] = "0.20"
    };

    public static TheoryData<string> CountKeys =>
    [
        "Training:MinSamples", "Training:MinFinishes", "Training:MinCount",
        "Competition:MinSamples", "Competition:MinFinishes", "Competition:MinCount"
    ];

    public static TheoryData<string> FractionKeys =>
    [
        "Training:TechniqueShare", "Training:PositionShare", "Training:TrendDelta",
        "Competition:TechniqueShare", "Competition:TrendDelta", "GapDelta"
    ];

    [Fact]
    public void Spec_values_are_valid()
    {
        Validate(Bind([])).Should().BeEmpty();
    }

    [Fact]
    public void Missing_values_fail_for_every_threshold()
    {
        var errors = Validate(new AnalyticsRulesOptions());

        errors.Should().HaveCount(12);
    }

    [Theory]
    [MemberData(nameof(CountKeys))]
    public void Count_must_be_at_least_1(string key)
    {
        Validate(Bind(new() { [key] = "1" })).Should().BeEmpty();
        Validate(Bind(new() { [key] = "0" })).Should().Equal($"Analytics:Rules:{key} must be at least 1.");
    }

    [Theory]
    [MemberData(nameof(FractionKeys))]
    public void Share_or_delta_must_be_above_0_and_at_most_1(string key)
    {
        var message = $"Analytics:Rules:{key} must be above 0 and at most 1.";

        Validate(Bind(new() { [key] = "0.0001" })).Should().BeEmpty();
        Validate(Bind(new() { [key] = "1" })).Should().BeEmpty();
        Validate(Bind(new() { [key] = "0" })).Should().Equal(message);
        Validate(Bind(new() { [key] = "1.0001" })).Should().Equal(message);
    }

    [Fact]
    public void Shares_bind_as_exact_decimals()
    {
        var options = Bind([]);

        options.Training.TechniqueShare.Should().Be(0.3m);
        options.Training.PositionShare.Should().Be(0.4m);
        options.GapDelta.Should().Be(0.2m);
    }

    // Binds the spec values with the given keys replaced, just like the app binds appsettings.json
    private static AnalyticsRulesOptions Bind(Dictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>(SpecValues);
        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        var options = new AnalyticsRulesOptions();
        new ConfigurationBuilder().AddInMemoryCollection(values).Build().Bind(options);
        return options;
    }

    private static IEnumerable<string> Validate(AnalyticsRulesOptions options)
    {
        var result = new AnalyticsRulesOptionsValidator().Validate(null, options);
        return result.Failed ? result.Failures! : [];
    }
}
