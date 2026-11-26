namespace MatForge.Application.Features.Analytics;

public interface IAnalyticsService
{
    Task<OverviewDto> GetOverviewAsync(AnalyticsQuery query, CancellationToken cancellationToken);
    Task<SparringDto> GetSparringAsync(AnalyticsFilterQuery query, CancellationToken cancellationToken);
    Task<CompetitionAnalyticsDto> GetCompetitionsAsync(AnalyticsFilterQuery query, CancellationToken cancellationToken);
    Task<InsightsDto> GetInsightsAsync(AnalyticsFilterQuery query, CancellationToken cancellationToken);
}
