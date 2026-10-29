using System.ComponentModel.DataAnnotations;

namespace MatForge.Application.Features.Profile;

public record ProfileDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    List<ProfileClubDto> Clubs,
    List<ProfileBeltDto> Belts);

public record ProfileClubDto(int Id, string Name);

public record ProfileBeltDto(int DisciplineId, int BeltId, int Degree);

public class UpdateProfileRequest
{
    [StringLength(100)]
    public string? FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }
}
