using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatForge.Application.Features.Analytics;

public class AnalyticsService(AppDbContext db, IOptions<AnalyticsRulesOptions> options) : IAnalyticsService
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

        var rows = await rounds
            .Select(r => new RoundRow(r.TrainingSession.Date, r.Result,
                r.TechniqueId, r.Technique!.Name,
                (int?)r.Technique.CategoryId, r.Technique.Category.Name,
                r.PositionId, r.Position!.Name))
            .ToListAsync(cancellationToken);

        return SparringCalculator.Calculate(period, rows, options.Value.Training.MinSamples);
    }

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
