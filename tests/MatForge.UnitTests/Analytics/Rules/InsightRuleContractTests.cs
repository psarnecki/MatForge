using FluentAssertions;
using MatForge.Application.Features.Analytics.Rules;
using static MatForge.UnitTests.Analytics.Rules.RuleInputs;

namespace MatForge.UnitTests.Analytics.Rules;

// Every rule can be called the same way and returns no results for empty input instead of throwing
public class InsightRuleContractTests
{
    public static TheoryData<string> RuleNames => [..AllRules.Select(r => r.GetType().Name)];

    [Theory]
    [MemberData(nameof(RuleNames))]
    public void Rule_returns_nothing_without_sides(string ruleName)
    {
        Rule(ruleName).Evaluate(Input()).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(RuleNames))]
    public void Rule_returns_nothing_for_sides_without_finishes_or_changes(string ruleName)
    {
        var input = Input(Side(InsightSection.Training), Side(InsightSection.Competition, samples: 5, wins: 2));

        Rule(ruleName).Evaluate(input).Should().BeEmpty();
    }

    private static IInsightRule Rule(string name) => AllRules.Single(r => r.GetType().Name == name);
}
