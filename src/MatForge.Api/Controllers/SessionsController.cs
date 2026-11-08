using MatForge.Application.Features.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace MatForge.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController(ISessionService sessionService) : ControllerBase
{
    [HttpGet]
    public Task<List<SessionSummaryDto>> GetAll([FromQuery] SessionListQuery query, CancellationToken cancellationToken) =>
        sessionService.GetAllAsync(query, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<SessionDto> Get(int id, CancellationToken cancellationToken) =>
        sessionService.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<SessionDto>> Create(SessionRequest request, CancellationToken cancellationToken)
    {
        var session = await sessionService.CreateAsync(request, cancellationToken);
        return Created($"/api/sessions/{session.Id}", session);
    }

    [HttpPut("{id:int}")]
    public Task<SessionDto> Update(int id, SessionRequest request, CancellationToken cancellationToken) =>
        sessionService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sessionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
