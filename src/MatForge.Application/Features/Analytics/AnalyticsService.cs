using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Application.Features.Analytics.Rules;
using MatForge.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatForge.Application.Features.Analytics;

public class AnalyticsService(AppDbContext db, IOptions<AnalyticsRulesOptions> options, IEnumerable<IInsightRule> rules)
    : IAnalyticsService
{
    // For GET /api/analytics/overview (one query loads the sessions, the calculator does the rest)
    public async Task<OverviewDto> GetOverviewAsync(AnalyticsQuery query, CancellationToken cancellationToken)
    {
        var period = await ResolvePeriodAsync(query, cancellationToken);

        var sessions = await db.TrainingSessions
            .Where(s => s.Date >= period.From && s.Date <= period.To)
            .Select(s => new SessionRow(s.Date, s.Kind, s.DurationMinutes, s.Feeling,
                s.DisciplineId, s.Discipline!.Name,
                s.FormatId, s.Format!.Name,
                s.CharacterId, s.Character!.Name,
                s.ClubId, s.Club!.Name))
            .ToListAsync(cancellationToken);

        return OverviewCalculator.Calculate(period, sessions);
    }

    // For GET /api/analytics/sparring (one query loads the rounds of this and the previous period)
    public async Task<SparringDto> GetSparringAsync(AnalyticsFilterQuery query, CancellationToken cancellationToken)
    {
        var period = await ResolvePeriodAsync(query, cancellationToken);
        var rounds = await LoadRoundsAsync(period, query, cancellationToken);
        return SparringCalculator.Calculate(period, rounds, options.Value.Training.MinSamples);
    }

    // For GET /api/analytics/competitions (one query loads competitions from the current and previous periods, including their fights)
    public async Task<CompetitionAnalyticsDto> GetCompetitionsAsync(AnalyticsFilterQuery query,
        CancellationToken cancellationToken)
    {
        var period = await ResolvePeriodAsync(query, cancellationToken);
        var competitions = await LoadCompetitionsAsync(period, query, cancellationToken);
        return CompetitionCalculator.Calculate(period, competitions, options.Value.Competition.MinSamples);
    }

    // Filters apply to the competition's own discipline and format, and competitions without fights are loaded too
    private Task<List<CompetitionRow>> LoadCompetitionsAsync(DateRange period, AnalyticsFilterQuery query,
        CancellationToken cancellationToken)
    {
        var start = period.Previous.From;
        var end = period.To;

        var competitions = db.Competitions.Where(c => c.Date >= start && c.Date <= end);
        if (query.DisciplineId != null)
        {
            competitions = competitions.Where(c => c.DisciplineId == query.DisciplineId);
        }

        if (query.FormatId != null)
        {
            competitions = competitions.Where(c => c.FormatId == query.FormatId);
        }

        return competitions
            .Select(c => new CompetitionRow(c.Id, c.Date, c.Name, c.Place,
                c.Fights.Select(f => new FightRow(f.Result, f.Method,
                    f.TechniqueId, f.Technique!.Name,
                    (int?)f.Technique.CategoryId, f.Technique.Category.Name)).ToList()))
            .ToListAsync(cancellationToken);
    }

    // For GET /api/analytics/insights (the sparring and competition loaders plus one query for the "Other" entries)
    public async Task<InsightsDto> GetInsightsAsync(AnalyticsFilterQuery query, CancellationToken cancellationToken)
    {
        var period = await ResolvePeriodAsync(query, cancellationToken);
        var rounds = await LoadRoundsAsync(period, query, cancellationToken);
        var competitions = await LoadCompetitionsAsync(period, query, cancellationToken);
        var other = await LoadOtherEntriesAsync(cancellationToken);

        return InsightsCalculator.Calculate(
            SparringCalculator.Calculate(period, rounds, options.Value.Training.MinSamples),
            CompetitionCalculator.Calculate(period, competitions, options.Value.Competition.MinSamples),
            other, options.Value, rules);
    }

    // Filters apply to the round's session
    private Task<List<RoundRow>> LoadRoundsAsync(DateRange period, AnalyticsFilterQuery query,
        CancellationToken cancellationToken)
    {
        var start = period.Previous.From;
        var end = period.To;

        var rounds = db.SparringRounds.Where(r => r.TrainingSession.Date >= start && r.TrainingSession.Date <= end);
        if (query.DisciplineId != null)
        {
            rounds = rounds.Where(r => r.TrainingSession.DisciplineId == query.DisciplineId);
        }

        if (query.FormatId != null)
        {
            rounds = rounds.Where(r => r.TrainingSession.FormatId == query.FormatId);
        }

        return rounds
            .Select(r => new RoundRow(r.TrainingSession.Date, r.Result,
                r.TechniqueId, r.Technique!.Name,
                (int?)r.Technique.CategoryId, r.Technique.Category.Name,
                r.PositionId, r.Position!.Name))
            .ToListAsync(cancellationToken);
    }

    // "Other" entries are identified by their codes, which never change
    private Task<OtherEntries> LoadOtherEntriesAsync(CancellationToken cancellationToken) =>
        db.Techniques
            .Where(t => t.Code == "other_finish")
            .Select(t => new OtherEntries(t.Id,
                db.TechniqueCategories.Where(c => c.Code == "other").Select(c => c.Id).First(),
                db.Positions.Where(p => p.Code == "other_position").Select(p => p.Id).First()))
            .SingleAsync(cancellationToken);

    // An omitted start requires the date of the earliest session or competition (one extra query)
    private async Task<DateRange> ResolvePeriodAsync(AnalyticsQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var earliestData = query.From == null
            ? await db.TrainingSessions.Select(s => s.Date)
                .Concat(db.Competitions.Select(c => c.Date))
                .MinAsync(date => (DateOnly?)date, cancellationToken)
            : null;

        return DateRange.Resolve(query.From, query.To, earliestData, today);
    }
}
