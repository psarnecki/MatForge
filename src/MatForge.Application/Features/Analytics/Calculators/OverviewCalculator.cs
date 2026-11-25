using MatForge.Data.Enums;

namespace MatForge.Application.Features.Analytics.Calculators;

public static class OverviewCalculator
{
    // Consistent ordering across machines, ignoring case and accents
    private static readonly StringComparer Names = StringComparer.InvariantCulture;

    public static OverviewDto Calculate(DateRange period, IReadOnlyCollection<SessionRow> rows)
    {
        var sessions = rows.Where(s => period.Contains(s.Date)).ToList();
        var grappling = sessions.Where(s => s.Kind == SessionKind.Grappling).ToList();

        var totals = new SessionTotalsDto(sessions.Count, sessions.Sum(s => s.DurationMinutes),
            sessions.Count == 0 ? null : RoundToOneDecimal((decimal)sessions.Sum(s => s.Feeling) / sessions.Count));

        // Both kinds are always listed, even without sessions
        var bySection = Enum.GetValues<SessionKind>()
            .Select(kind => new SectionDto(kind,
                sessions.Count(s => s.Kind == kind),
                sessions.Where(s => s.Kind == kind).Sum(s => s.DurationMinutes)))
            .OrderByDescending(s => s.Count).ThenBy(s => s.Kind.ToString(), Names)
            .ToList();

        var byDiscipline = Groups(grappling, s => s.DisciplineId!.Value, s => s.DisciplineName!);
        var byCharacter = Groups(grappling, s => s.CharacterId!.Value, s => s.CharacterName!);

        var byFormat = grappling
            .GroupBy(s => s.FormatId!.Value)
            .Select(g => new FormatGroupDto(g.Key, g.First().FormatName!, g.First().DisciplineId!.Value, g.Count(),
                g.Sum(s => s.DurationMinutes), AnalyticsMath.Share(g.Count(), grappling.Count)))
            .OrderByDescending(f => f.Count).ThenBy(f => f.Name, Names).ThenBy(f => f.Id)
            .ToList();

        // "Other club" has no name and comes after named clubs with the same count
        var byClub = grappling
            .GroupBy(s => s.ClubId)
            .Select(g => new ClubGroupDto(g.Key, g.First().ClubName, g.Count(), g.Sum(s => s.DurationMinutes),
                AnalyticsMath.Share(g.Count(), grappling.Count)))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Id == null).ThenBy(c => c.Name, Names).ThenBy(c => c.Id)
            .ToList();

        var sessionsByWeek = sessions
            .GroupBy(s => DateBuckets.Start(s.Date, Bucket.Week))
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Minutes: g.Sum(s => s.DurationMinutes)));
        var weekly = DateBuckets.Starts(period, Bucket.Week)
            .Select(start => sessionsByWeek.TryGetValue(start, out var week)
                ? new WeekDto(start, week.Count, week.Minutes)
                : new WeekDto(start, 0, 0))
            .ToList();

        return new OverviewDto(
            new PeriodDto(period.From, period.To, period.Days),
            totals,
            bySection,
            byDiscipline,
            byFormat,
            byClub,
            byCharacter,
            weekly,
            RoundToOneDecimal(sessions.Count * 7m / period.Days),
            AnalyticsMath.Share(weekly.Count(w => w.Count > 0), weekly.Count));
    }

    // Shares are counted out of all grappling sessions
    private static List<SessionGroupDto> Groups(List<SessionRow> grappling, Func<SessionRow, int> id,
        Func<SessionRow, string> name) =>
        grappling
            .GroupBy(id)
            .Select(g => new SessionGroupDto(g.Key, name(g.First()), g.Count(), g.Sum(s => s.DurationMinutes),
                AnalyticsMath.Share(g.Count(), grappling.Count)))
            .OrderByDescending(g => g.Count).ThenBy(g => g.Name, Names).ThenBy(g => g.Id)
            .ToList();

    private static decimal RoundToOneDecimal(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
