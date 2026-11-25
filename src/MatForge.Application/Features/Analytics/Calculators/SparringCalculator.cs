using MatForge.Data.Enums;

namespace MatForge.Application.Features.Analytics.Calculators;

public static class SparringCalculator
{
    // Compares letters first and uses accents and case only to break ties
    private static readonly StringComparer Names = StringComparer.InvariantCulture;

    // Rows may also cover the previous period, but rows outside both periods are ignored
    public static SparringDto Calculate(DateRange period, IReadOnlyCollection<RoundRow> rows, int minSamples)
    {
        var previousPeriod = period.Previous;
        var rounds = rows.Where(r => period.Contains(r.Date)).ToList();
        var previousRounds = rows.Where(r => previousPeriod.Contains(r.Date)).ToList();

        var totals = Totals(rounds);
        var previous = Totals(previousRounds);
        var bucket = DateBuckets.For(period);

        return new SparringDto(
            totals,
            Finishes(rounds, RoundResult.Win),
            Finishes(rounds, RoundResult.Loss),
            new TrendDto(
                bucket,
                Points(period, bucket, rounds),
                new PreviousPeriodDto(previous.Rounds, previous.WinRate),
                WinRateDelta(totals, previous, minSamples)));
    }

    private static RoundTotalsDto Totals(List<RoundRow> rounds)
    {
        var wins = rounds.Count(r => r.Result == RoundResult.Win);
        var losses = rounds.Count(r => r.Result == RoundResult.Loss);
        var draws = rounds.Count(r => r.Result == RoundResult.Draw);
        return new RoundTotalsDto(rounds.Count, wins, losses, draws, AnalyticsMath.WinRate(wins, rounds.Count),
            wins - losses);
    }

    // A finish is a won or lost round with a technique
    private static FinishesDto Finishes(List<RoundRow> rounds, RoundResult result)
    {
        var finishes = rounds.Where(r => r.Result == result && r.TechniqueId != null).ToList();

        // Finishes without a position are excluded from position shares
        var withPosition = finishes.Where(r => r.PositionId != null).ToList();

        return new FinishesDto(
            finishes.Count,
            Groups(finishes, r => r.TechniqueId!.Value, r => r.TechniqueName!),
            Groups(finishes, r => r.CategoryId!.Value, r => r.CategoryName!),
            new PositionFinishesDto(withPosition.Count,
                Groups(withPosition, r => r.PositionId!.Value, r => r.PositionName!)));
    }

    // Each share is counted out of all rows passed in
    private static List<FinishGroupDto> Groups(List<RoundRow> rows, Func<RoundRow, int> id, Func<RoundRow, string> name) =>
        rows
            .GroupBy(id)
            .Select(g => new FinishGroupDto(g.Key, name(g.First()), g.Count(), AnalyticsMath.Share(g.Count(), rows.Count)))
            .OrderByDescending(g => g.Count).ThenBy(g => g.Name, Names).ThenBy(g => g.Id)
            .ToList();

    // Only rounds within the period are grouped, so an earlier-starting first bucket excludes previous rounds
    private static List<TrendPointDto> Points(DateRange period, Bucket bucket, List<RoundRow> rounds)
    {
        var roundsByBucket = rounds.ToLookup(r => DateBuckets.Start(r.Date, bucket));
        return DateBuckets.Starts(period, bucket)
            .Select(start => Point(start, roundsByBucket[start].ToList()))
            .ToList();
    }

    private static TrendPointDto Point(DateOnly start, List<RoundRow> rounds)
    {
        var totals = Totals(rounds);
        return new TrendPointDto(start, totals.Rounds, totals.Wins, totals.Losses, totals.Draws, totals.WinRate);
    }

    // Calculated from the unrounded rates and rounded once, null when either period has too few rounds
    private static decimal? WinRateDelta(RoundTotalsDto current, RoundTotalsDto previous, int minSamples) =>
        current.Rounds < minSamples || previous.Rounds < minSamples
            ? null
            : AnalyticsMath.RoundFraction(
                (decimal)current.Wins / current.Rounds - (decimal)previous.Wins / previous.Rounds);
}
