namespace MatForge.Application.Features.Profile;

public interface IProfileService
{
    Task<ProfileDto> GetAsync(CancellationToken cancellationToken);
    Task<ProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(CancellationToken cancellationToken);
}
