using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Rules;
using static MatForge.UnitTests.Analytics.Rules.RuleInputs;

namespace MatForge.UnitTests.Analytics.Rules;

public class PositionRuleTests
{
    private static readonly Dictionary<string, PositionRule> Rules = new()
    {
        ["StrongPosition"] = new(new StrongPositionRule(), InsightType.Strength, false),
        ["VulnerablePosition"] = new(new VulnerablePositionRule(), InsightType.Weakness, true)
    };

    public static TheoryData<string> RuleCodes => [..Rules.Keys];

    // Block total, recorded positions, item count, item share, fires
    private static readonly (int, int, int, decimal, bool)[] Boundaries =
    [
        (8, 5, 3, 0.6m, true),
        (8, 4, 3, 0.75m, false),
        (8, 5, 2, 0.4m, false),
        (10, 10, 4, 0.4m, true),
        (8, 8, 3, 0.375m, false),
        // 1601/4003 is 0.39995 before rounding
        (4003, 4003, 1601, 0.4m, true),
        (4008, 4008, 1602, 0.3997m, false)
    ];

    public static TheoryData<string, int, int, int, decimal, bool> BoundaryCases()
    {
        var data = new TheoryData<string, int, int, int, decimal, bool>();
        foreach (var rule in Rules.Keys)
        {
            foreach (var (total, recorded, count, share, fires) in Boundaries)
            {
                data.Add(rule, total, recorded, count, share, fires);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void Fires_at_min_finishes_of_recorded_positions_min_count_and_position_share_but_not_one_below(string rule,
        int total, int recorded, int count, decimal share, bool fires)
    {
        var item = new FinishGroupDto(15, "Back Control", count, share);

        InsightResult[] expected = fires ? [new InsightResult(InsightSection.Training, Insight(rule, item))] : [];

        Evaluate(rule, Input(training: SideWith(rule, InsightSection.Training, total, recorded, [item]))).Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void Uses_the_position_share_and_not_the_technique_share(string rule)
    {
        var item = new FinishGroupDto(15, "Back Control", 3, 0.6m);
        var training = SideWith(rule, InsightSection.Training, 5, 5, [item]);

        Evaluate(rule, Input(training: training, positionShare: 0.7m)).Should().BeEmpty();

        var strictTechniqueShare = new TrainingThresholds
        {
            MinSamples = 10, MinFinishes = 5, MinCount = 3, TechniqueShare = 0.9m, PositionShare = 0.4m, TrendDelta = 0.1m
        };
        Evaluate(rule, Input(training: SideWith(rule, InsightSection.Training, 5, 5, [item], strictTechniqueShare)))
            .Should().Equal(new InsightResult(InsightSection.Training, Insight(rule, item)));
    }

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void Never_fires_on_the_competition_side(string rule)
    {
        var item = new FinishGroupDto(15, "Back Control", 5, 1m);

        Evaluate(rule, Input(competition: SideWith(rule, InsightSection.Competition, 5, 5, [item]))).Should().BeEmpty();
        Evaluate(rule, Input(
                SideWith(rule, InsightSection.Training, 5, 5, [item]),
                SideWith(rule, InsightSection.Competition, 5, 5, [item])))
            .Should().Equal(new InsightResult(InsightSection.Training, Insight(rule, item)));
    }

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void Reads_only_its_own_block(string rule)
    {
        var block = new InsightFinishes(5, [], [], 5, [new FinishGroupDto(15, "Back Control", 5, 1m)]);
        var training = Rules[rule].Against
            ? Side(InsightSection.Training, finishesFor: block)
            : Side(InsightSection.Training, finishesAgainst: block);

        Evaluate(rule, Input(training: training)).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void Returns_one_position_in_name_order(string rule)
    {
        List<FinishGroupDto> items = [new(15, "Mount", 4, 0.5m), new(9, "Back Control", 4, 0.5m)];

        var results = Evaluate(rule, Input(training: SideWith(rule, InsightSection.Training, 8, 8, items)));

        results.Should().Equal(new InsightResult(InsightSection.Training, Insight(rule, items[1])));
    }

    private static IEnumerable<InsightResult> Evaluate(string rule, InsightInput input) => Rules[rule].Rule.Evaluate(input);

    private static InsightDto Insight(string rule, FinishGroupDto item) => new(rule, Rules[rule].Type, item);

    private static InsightSide SideWith(string rule, InsightSection section, int total, int recorded,
        List<FinishGroupDto> items, RuleThresholds? thresholds = null)
    {
        var block = new InsightFinishes(total, [], [], recorded, items);
        return Rules[rule].Against
            ? Side(section, thresholds, finishesAgainst: block)
            : Side(section, thresholds, finishesFor: block);
    }

    private record PositionRule(IInsightRule Rule, InsightType Type, bool Against);
}
