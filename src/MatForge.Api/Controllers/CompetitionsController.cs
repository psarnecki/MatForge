using MatForge.Application.Features.Competitions;
using Microsoft.AspNetCore.Mvc;

namespace MatForge.Api.Controllers;

[ApiController]
[Route("api/competitions")]
public class CompetitionsController(ICompetitionService competitionService) : ControllerBase
{
    [HttpGet]
    public Task<List<CompetitionSummaryDto>> GetAll([FromQuery] CompetitionListQuery query, CancellationToken cancellationToken) =>
        competitionService.GetAllAsync(query, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<CompetitionDto> Get(int id, CancellationToken cancellationToken) =>
        competitionService.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<CompetitionDto>> Create(CompetitionRequest request, CancellationToken cancellationToken)
    {
        var competition = await competitionService.CreateAsync(request, cancellationToken);
        return Created($"/api/competitions/{competition.Id}", competition);
    }

    [HttpPut("{id:int}")]
    public Task<CompetitionDto> Update(int id, CompetitionRequest request, CancellationToken cancellationToken) =>
        competitionService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await competitionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
