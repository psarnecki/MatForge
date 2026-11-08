using MatForge.Application.Features.Clubs;
using Microsoft.AspNetCore.Mvc;

namespace MatForge.Api.Controllers;

[ApiController]
[Route("api/clubs")]
public class ClubsController(IClubService clubService) : ControllerBase
{
    [HttpGet]
    public Task<List<ClubDto>> GetAll(CancellationToken cancellationToken) =>
        clubService.GetAllAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ClubDto>> Create(ClubRequest request, CancellationToken cancellationToken)
    {
        var club = await clubService.CreateAsync(request, cancellationToken);
        return Created($"/api/clubs/{club.Id}", club);
    }

    [HttpPut("{id:int}")]
    public Task<ClubDto> Update(int id, ClubRequest request, CancellationToken cancellationToken) =>
        clubService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await clubService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
