using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Rules;
using static MatForge.UnitTests.Analytics.Rules.RuleInputs;

namespace MatForge.UnitTests.Analytics.Rules;

public class CompetitionVsTrainingGapRuleTests
{
    private static readonly CompetitionVsTrainingGapRule Rule = new();

    [Theory]
    [InlineData(12, 20, 2, 5, true)]
    [InlineData(4, 10, 3, 5, true)]
    [InlineData(6, 10, 4001, 10000, false)]
    [InlineData(4, 10, 5999, 10000, false)]
    // 3999/20000 is a gap of exactly 0.19995 that rounds to 0.2, while 3998/20000 gives 0.1999
    [InlineData(0, 10, 3999, 20000, true)]
    [InlineData(0, 10, 3998, 20000, false)]
    public void Fires_when_the_gap_reaches_the_gap_delta_in_either_direction(int trainingWins, int rounds,
        int competitionWins, int fights, bool fires)
    {
        var input = Input(
            Side(InsightSection.Training, samples: rounds, wins: trainingWins),
            Side(InsightSection.Competition, samples: fights, wins: competitionWins));

        Rule.Evaluate(input).Should().HaveCount(fires ? 1 : 0);
    }

    [Fact]
    public void Returns_the_rates_and_the_competition_minus_training_delta()
    {
        var input = Input(
            Side(InsightSection.Training, samples: 20, wins: 12),
            Side(InsightSection.Competition, samples: 5, wins: 2));

        Rule.Evaluate(input).Should().Equal(new InsightResult(InsightSection.Comparison,
            new InsightDto("CompetitionVsTrainingGap", InsightType.Info, new WinRateGapDto(0.6m, 0.4m, -0.2m))));
    }

    [Fact]
    public void Delta_is_computed_from_the_unrounded_rates()
    {
        // The rounded rates 0.4333 and 0.6667 would give -0.2334
        var input = Input(
            Side(InsightSection.Training, samples: 3, wins: 2),
            Side(InsightSection.Competition, samples: 30, wins: 13));

        Rule.Evaluate(input).Single().Insight.Params.Should().Be(new WinRateGapDto(0.6667m, 0.4333m, -0.2333m));
    }

    [Fact]
    public void Reads_the_gap_delta_from_the_input()
    {
        var training = Side(InsightSection.Training, samples: 20, wins: 12);
        var competition = Side(InsightSection.Competition, samples: 5, wins: 2);

        Rule.Evaluate(Input(training, competition, gapDelta: 0.3m)).Should().BeEmpty();
    }

    [Fact]
    public void Needs_both_sides_with_enough_data()
    {
        var training = Side(InsightSection.Training, samples: 20, wins: 20);
        var competition = Side(InsightSection.Competition, samples: 5, wins: 0);

        Rule.Evaluate(Input(training: training)).Should().BeEmpty();
        Rule.Evaluate(Input(competition: competition)).Should().BeEmpty();
        Rule.Evaluate(Input()).Should().BeEmpty();
    }
}
