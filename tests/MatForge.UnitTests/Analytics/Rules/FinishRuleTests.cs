using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Rules;
using static MatForge.UnitTests.Analytics.Rules.RuleInputs;

namespace MatForge.UnitTests.Analytics.Rules;

// The four technique and category rules, each tested with its data in its own block and list
public class FinishRuleTests
{
    private static readonly Dictionary<string, FinishRule> Rules = new()
    {
        ["DominantFinisherTechnique"] = new(new DominantFinisherTechniqueRule(), InsightType.Strength, 3, false, false),
        ["DominantFinisherCategory"] = new(new DominantFinisherCategoryRule(), InsightType.Strength, 1, false, true),
        ["RecurringLossTechnique"] = new(new RecurringLossTechniqueRule(), InsightType.Weakness, 3, true, false),
        ["RecurringLossCategory"] = new(new RecurringLossCategoryRule(), InsightType.Weakness, 1, true, true)
    };

    public static TheoryData<string> RuleCodes => [..Rules.Keys];

    // Profile, block total, item count, item share, fires
    private static readonly (string, int, int, decimal, bool)[] Boundaries =
    [
        ("Training", 5, 3, 0.6m, true),
        ("Training", 4, 3, 0.75m, false),
        ("Training", 6, 3, 0.5m, true),
        ("Training", 6, 2, 0.3333m, false),
        ("Training", 10, 3, 0.3m, true),
        ("Training", 11, 3, 0.2727m, false),
        // 602/2007 is 0.29995 before rounding and 601/2004 is 0.2999
        ("Training", 2007, 602, 0.3m, true),
        ("Training", 2004, 601, 0.2999m, false),
        ("Competition", 3, 2, 0.6667m, true),
        ("Competition", 2, 2, 1m, false),
        ("Competition", 6, 2, 0.3333m, true),
        ("Competition", 3, 1, 0.3333m, false),
        ("Competition", 10, 3, 0.3m, true),
        ("Competition", 7, 2, 0.2857m, false)
    ];

    public static TheoryData<string, string, int, int, decimal, bool> BoundaryCases()
    {
        var data = new TheoryData<string, string, int, int, decimal, bool>();
        foreach (var rule in Rules.Keys)
        {
            foreach (var (profile, total, count, share, fires) in Boundaries)
            {
                data.Add(rule, profile, total, count, share, fires);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void Fires_at_min_finishes_min_count_and_technique_share_but_not_one_below(string rule, string profile,
        int total, int count, decimal share, bool fires)
    {
        var item = new FinishGroupDto(1, "Rear Naked Choke", count, share);
        var section = SectionOf(profile);
        var input = section == InsightSection.Training
            ? Input(training: SideWith(rule, section, total, [item]))
            : Input(competition: SideWith(rule, section, total, [item]));

        InsightResult[] expected = fires ? [new InsightResult(section, Insight(rule, item))] : [];

        Evaluate(rule, input).Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void Reads_the_technique_share_of_the_profile(string rule)
    {
        var profile = new RuleThresholds { MinSamples = 10, MinFinishes = 5, MinCount = 3, TechniqueShare = 0.5m, TrendDelta = 0.1m };
        var below = new FinishGroupDto(1, "Rear Naked Choke", 4, 0.4m);
        var atShare = new FinishGroupDto(1, "Rear Naked Choke", 5, 0.5m);

        Evaluate(rule, Input(training: SideWith(rule, InsightSection.Training, 10, [below], profile))).Should().BeEmpty();
        Evaluate(rule, Input(training: SideWith(rule, InsightSection.Training, 10, [atShare], profile)))
            .Should().Equal(new InsightResult(InsightSection.Training, Insight(rule, atShare)));
    }

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void Reads_only_its_own_block_and_list(string rule)
    {
        var item = new FinishGroupDto(1, "Rear Naked Choke", 5, 1m);
        var own = Rules[rule];
        var otherBlock = Block(5, [item], own.Categories);
        var otherList = Block(5, [item], !own.Categories);

        var input = Input(training: own.Against
            ? Side(InsightSection.Training, finishesFor: otherBlock, finishesAgainst: otherList)
            : Side(InsightSection.Training, finishesFor: otherList, finishesAgainst: otherBlock));

        Evaluate(rule, input).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void Gives_each_side_its_own_insight(string rule)
    {
        var trainingItem = new FinishGroupDto(1, "Rear Naked Choke", 5, 1m);
        var competitionItem = new FinishGroupDto(20, "Armbar", 3, 1m);
        var input = Input(
            SideWith(rule, InsightSection.Training, 5, [trainingItem]),
            SideWith(rule, InsightSection.Competition, 3, [competitionItem]));

        Evaluate(rule, input).Should().Equal(
            new InsightResult(InsightSection.Training, Insight(rule, trainingItem)),
            new InsightResult(InsightSection.Competition, Insight(rule, competitionItem)));
    }

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void Returns_at_most_its_limit_in_name_order(string rule)
    {
        // A 0.2 share lets five items qualify, which a 0.3 share never allows
        var profile = new RuleThresholds { MinSamples = 10, MinFinishes = 5, MinCount = 3, TechniqueShare = 0.2m, TrendDelta = 0.1m };
        List<FinishGroupDto> items =
        [
            new(2, "Triangle Choke", 4, 0.2m),
            new(20, "Armbar", 4, 0.2m),
            new(25, "Kimura", 4, 0.2m),
            new(3, "Guillotine", 4, 0.2m),
            new(1, "Rear Naked Choke", 4, 0.2m)
        ];
        string[] nameOrder = ["Armbar", "Guillotine", "Kimura"];

        var results = Evaluate(rule, Input(training: SideWith(rule, InsightSection.Training, 20, items, profile)));

        results.Select(r => ((FinishGroupDto)r.Insight.Params).Name).Should().Equal(nameOrder.Take(Rules[rule].Limit));
    }

    private static IEnumerable<InsightResult> Evaluate(string rule, InsightInput input) => Rules[rule].Rule.Evaluate(input);

    private static InsightDto Insight(string rule, FinishGroupDto item) => new(rule, Rules[rule].Type, item);

    private static InsightSide SideWith(string rule, InsightSection section, int total, List<FinishGroupDto> items,
        RuleThresholds? thresholds = null)
    {
        var block = Block(total, items, Rules[rule].Categories);
        return Rules[rule].Against
            ? Side(section, thresholds, finishesAgainst: block)
            : Side(section, thresholds, finishesFor: block);
    }

    private static InsightFinishes Block(int total, List<FinishGroupDto> items, bool categories) =>
        categories ? new InsightFinishes(total, [], items, 0, []) : new InsightFinishes(total, items, [], 0, []);

    private record FinishRule(IInsightRule Rule, InsightType Type, int Limit, bool Against, bool Categories);
}
