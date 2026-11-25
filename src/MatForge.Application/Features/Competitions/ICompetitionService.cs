namespace MatForge.Application.Features.Competitions;

public interface ICompetitionService
{
    Task<List<CompetitionSummaryDto>> GetAllAsync(CompetitionListQuery query, CancellationToken cancellationToken);
    Task<CompetitionDto> GetAsync(int id, CancellationToken cancellationToken);
    Task<CompetitionDto> CreateAsync(CompetitionRequest request, CancellationToken cancellationToken);
    Task<CompetitionDto> UpdateAsync(int id, CompetitionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
