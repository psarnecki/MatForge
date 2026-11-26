using MatForge.Data.Enums;

namespace MatForge.Application.Features.Analytics.Calculators;

public static class CompetitionCalculator
{
    // Compares letters first and uses accents and case only to break ties
    private static readonly StringComparer Names = StringComparer.InvariantCulture;

    // Rows may also cover the previous period, but rows outside both periods are ignored
    public static CompetitionAnalyticsDto Calculate(DateRange period, IReadOnlyCollection<CompetitionRow> rows, int minSamples)
    {
        var competitions = rows.Where(c => period.Contains(c.Date)).ToList();
        var fights = competitions.SelectMany(c => c.Fights).ToList();
        var previousFights = rows.Where(c => period.Previous.Contains(c.Date)).SelectMany(c => c.Fights).ToList();

        var wins = Count(fights, FightResult.Win);
        var previousWins = Count(previousFights, FightResult.Win);

        // Places other than 1-3 and unknown places do not count as medals
        var totals = new FightTotalsDto(
            competitions.Count,
            fights.Count,
            wins,
            Count(fights, FightResult.Loss),
            Count(fights, FightResult.Draw),
            AnalyticsMath.WinRate(wins, fights.Count),
            competitions.Count(c => c.Place == 1),
            competitions.Count(c => c.Place == 2),
            competitions.Count(c => c.Place == 3));

        // Calculated from unrounded rates and rounded once, null if either period has too few fights
        var winRateDelta = fights.Count < minSamples || previousFights.Count < minSamples
            ? (decimal?)null
            : AnalyticsMath.RateDifference(wins, fights.Count, previousWins, previousFights.Count);

        return new CompetitionAnalyticsDto(
            totals,
            new MethodsDto(Methods(fights, FightResult.Win), Methods(fights, FightResult.Loss)),
            Finishes(fights, FightResult.Win),
            Finishes(fights, FightResult.Loss),
            Timeline(competitions),
            new PreviousFightsDto(previousFights.Count, AnalyticsMath.WinRate(previousWins, previousFights.Count)),
            winRateDelta);
    }

    private static int Count(List<FightRow> fights, FightResult result) => fights.Count(f => f.Result == result);

    // Each share is counted out of the won or lost fights, and draws have no method
    private static List<MethodGroupDto> Methods(List<FightRow> fights, FightResult result)
    {
        var decided = fights.Where(f => f.Result == result).ToList();
        return decided
            .GroupBy(f => f.Method!.Value)
            .Select(g => new MethodGroupDto(g.Key, g.Count(), AnalyticsMath.Share(g.Count(), decided.Count)))
            .OrderByDescending(m => m.Count).ThenBy(m => m.Method.ToString(), Names)
            .ToList();
    }

    // A finish is a won or lost fight with a technique, which is only available for Submission, Throw and Pin
    private static FightFinishesDto Finishes(List<FightRow> fights, FightResult result)
    {
        var finishes = fights.Where(f => f.Result == result && f.TechniqueId != null).ToList();
        return new FightFinishesDto(
            finishes.Count,
            Groups(finishes, f => f.TechniqueId!.Value, f => f.TechniqueName!),
            Groups(finishes, f => f.CategoryId!.Value, f => f.CategoryName!));
    }

    // Each share is counted out of all rows passed in
    private static List<FinishGroupDto> Groups(List<FightRow> rows, Func<FightRow, int> id, Func<FightRow, string> name) =>
        rows
            .GroupBy(id)
            .Select(g => new FinishGroupDto(g.Key, name(g.First()), g.Count(), AnalyticsMath.Share(g.Count(), rows.Count)))
            .OrderByDescending(g => g.Count).ThenBy(g => g.Name, Names).ThenBy(g => g.Id)
            .ToList();

    // Oldest first, with competitions on the same date kept in their original order
    private static List<TimelineItemDto> Timeline(List<CompetitionRow> competitions) =>
        competitions
            .OrderBy(c => c.Date).ThenBy(c => c.Id)
            .Select(c => new TimelineItemDto(c.Id, c.Date, c.Name, c.Place, c.Fights.Count,
                Count(c.Fights, FightResult.Win), Count(c.Fights, FightResult.Loss)))
            .ToList();
}
