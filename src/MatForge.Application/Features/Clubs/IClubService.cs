namespace MatForge.Application.Features.Clubs;

public interface IClubService
{
    Task<List<ClubDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ClubDto> CreateAsync(ClubRequest request, CancellationToken cancellationToken);
    Task<ClubDto> UpdateAsync(int id, ClubRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
