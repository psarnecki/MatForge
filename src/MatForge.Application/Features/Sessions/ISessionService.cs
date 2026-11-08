namespace MatForge.Application.Features.Sessions;

public interface ISessionService
{
    Task<List<SessionSummaryDto>> GetAllAsync(SessionListQuery query, CancellationToken cancellationToken);
    Task<SessionDto> GetAsync(int id, CancellationToken cancellationToken);
    Task<SessionDto> CreateAsync(SessionRequest request, CancellationToken cancellationToken);
    Task<SessionDto> UpdateAsync(int id, SessionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
