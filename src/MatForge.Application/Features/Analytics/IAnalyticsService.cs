namespace MatForge.Application.Features.Analytics;

public interface IAnalyticsService
{
    Task<OverviewDto> GetOverviewAsync(AnalyticsQuery query, CancellationToken cancellationToken);
}
