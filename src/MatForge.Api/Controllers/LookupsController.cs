using MatForge.Application.Features.Lookups;
using Microsoft.AspNetCore.Mvc;

namespace MatForge.Api.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController(ILookupsService lookupsService) : ControllerBase
{
    // Browser may keep it for an hour (dictionaries change only with a new release)
    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public Task<LookupsDto> Get(CancellationToken cancellationToken) =>
        lookupsService.GetAsync(cancellationToken);
}
