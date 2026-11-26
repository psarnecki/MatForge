using MatForge.Application.Features.Analytics.Rules;

namespace MatForge.Application.Features.Analytics.Calculators;

public static class InsightsCalculator
{
    // Every rule gets the same input, and each insight goes to the section its rule chose
    public static InsightsDto Calculate(SparringDto sparring, CompetitionAnalyticsDto competitions, OtherEntries other,
        AnalyticsRulesOptions options, IEnumerable<IInsightRule> rules)
    {
        var training = sparring.Totals.Rounds < options.Training.MinSamples
            ? null
            : new InsightSide(InsightSection.Training, options.Training,
                sparring.Totals.Rounds, sparring.Totals.Wins, sparring.Totals.WinRate!.Value,
                sparring.Trend.Previous.WinRate, sparring.Trend.WinRateDelta,
                Finishes(sparring.FinishesFor, other), Finishes(sparring.FinishesAgainst, other));

        var competition = competitions.Totals.Fights < options.Competition.MinSamples
            ? null
            : new InsightSide(InsightSection.Competition, options.Competition,
                competitions.Totals.Fights, competitions.Totals.Wins, competitions.Totals.WinRate!.Value,
                competitions.Previous.WinRate, competitions.WinRateDelta,
                Finishes(competitions.FinishesFor, other), Finishes(competitions.FinishesAgainst, other));

        var input = new InsightInput(training, competition, options.Training.PositionShare, options.GapDelta);
        var results = rules.SelectMany(rule => rule.Evaluate(input)).ToList();

        return new InsightsDto(
            Side(sparring.Totals.Rounds, options.Training.MinSamples, results, InsightSection.Training),
            Side(competitions.Totals.Fights, options.Competition.MinSamples, results, InsightSection.Competition),
            Items(results, InsightSection.Comparison));
    }

    // "Other" entries leave the lists before any rule runs, while the totals still count them
    private static InsightFinishes Finishes(FinishesDto finishes, OtherEntries other) =>
        new(finishes.Total,
            Without(finishes.ByTechnique, other.TechniqueId),
            Without(finishes.ByCategory, other.CategoryId),
            finishes.ByPosition.Recorded,
            Without(finishes.ByPosition.Items, other.PositionId));

    // Fight finishes have no position data
    private static InsightFinishes Finishes(FightFinishesDto finishes, OtherEntries other) =>
        new(finishes.Total,
            Without(finishes.ByTechnique, other.TechniqueId),
            Without(finishes.ByCategory, other.CategoryId),
            0,
            []);

    private static List<FinishGroupDto> Without(List<FinishGroupDto> groups, int otherId) =>
        groups.Where(g => g.Id != otherId).ToList();

    private static SideInsightsDto Side(int samples, int minSamples, List<InsightResult> results, InsightSection section) =>
        new(samples < minSamples ? InsightStatus.InsufficientData : InsightStatus.Ok, samples, minSamples,
            Items(results, section));

    private static List<InsightDto> Items(List<InsightResult> results, InsightSection section) =>
        results.Where(r => r.Section == section).Select(r => r.Insight).ToList();
}
