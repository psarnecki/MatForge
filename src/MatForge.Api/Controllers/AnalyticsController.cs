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
}
