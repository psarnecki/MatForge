using System.Globalization;
using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Rules;
using static MatForge.UnitTests.Analytics.Rules.RuleInputs;

namespace MatForge.UnitTests.Analytics.Rules;

public class WinRateTrendRuleTests
{
    private static readonly IInsightRule[] Rules = [new ImprovingWinRateRule(), new DecliningWinRateRule()];

    [Theory]
    [InlineData("Training", "0.1", "ImprovingWinRate")]
    [InlineData("Training", "0.0999", null)]
    [InlineData("Training", "-0.1", "DecliningWinRate")]
    [InlineData("Training", "-0.0999", null)]
    [InlineData("Training", "0.5", "ImprovingWinRate")]
    [InlineData("Training", null, null)]
    [InlineData("Competition", "0.1", "ImprovingWinRate")]
    [InlineData("Competition", "0.0999", null)]
    [InlineData("Competition", "-0.1", "DecliningWinRate")]
    [InlineData("Competition", "-0.0999", null)]
    [InlineData("Competition", null, null)]
    public void Fires_when_the_change_reaches_the_trend_delta(string profile, string? delta, string? firedRule)
    {
        var side = Side(SectionOf(profile), previousWinRate: 0.5m, winRateDelta: Number(delta));
        var input = profile == "Training" ? Input(training: side) : Input(competition: side);

        string[] expected = firedRule == null ? [] : [firedRule];

        Fired(input).Should().Equal(expected);
    }

    [Theory]
    [InlineData("0.15", null)]
    [InlineData("0.2", "ImprovingWinRate")]
    [InlineData("-0.15", null)]
    [InlineData("-0.2", "DecliningWinRate")]
    public void Reads_the_trend_delta_of_the_profile(string delta, string? firedRule)
    {
        var profile = new RuleThresholds { MinSamples = 10, MinFinishes = 5, MinCount = 3, TechniqueShare = 0.3m, TrendDelta = 0.2m };

        var input = Input(training: Side(InsightSection.Training, profile, previousWinRate: 0.5m, winRateDelta: Number(delta)));

        string[] expected = firedRule == null ? [] : [firedRule];

        Fired(input).Should().Equal(expected);
    }

    [Fact]
    public void Gives_each_side_its_own_change()
    {
        var input = Input(
            Side(InsightSection.Training, samples: 20, wins: 12, previousWinRate: 0.4667m, winRateDelta: 0.1333m),
            Side(InsightSection.Competition, samples: 5, wins: 1, previousWinRate: 0.6m, winRateDelta: -0.4m));

        Rules.SelectMany(rule => rule.Evaluate(input)).Should().Equal(
            new InsightResult(InsightSection.Training,
                new InsightDto("ImprovingWinRate", InsightType.Trend, new WinRateChangeDto(0.6m, 0.4667m, 0.1333m))),
            new InsightResult(InsightSection.Competition,
                new InsightDto("DecliningWinRate", InsightType.Trend, new WinRateChangeDto(0.2m, 0.6m, -0.4m))));
    }

    private static List<string> Fired(InsightInput input) =>
        Rules.SelectMany(rule => rule.Evaluate(input)).Select(r => r.Insight.Rule).ToList();

    private static decimal? Number(string? value) => value == null ? null : decimal.Parse(value, CultureInfo.InvariantCulture);
}
