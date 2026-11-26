using MatForge.Application.Features.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace MatForge.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("overview")]
    public Task<OverviewDto> GetOverview([FromQuery] AnalyticsQuery query, CancellationToken cancellationToken) =>
        analyticsService.GetOverviewAsync(query, cancellationToken);

    [HttpGet("sparring")]
    public Task<SparringDto> GetSparring([FromQuery] AnalyticsFilterQuery query, CancellationToken cancellationToken) =>
        analyticsService.GetSparringAsync(query, cancellationToken);

    [HttpGet("competitions")]
    public Task<CompetitionAnalyticsDto> GetCompetitions([FromQuery] AnalyticsFilterQuery query,
        CancellationToken cancellationToken) =>
        analyticsService.GetCompetitionsAsync(query, cancellationToken);
}
