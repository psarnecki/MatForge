using System.ComponentModel.DataAnnotations;
using MatForge.Application.Features.Clubs;

namespace MatForge.Application.Features.Profile;

public record ProfileDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    List<ClubDto> Clubs,
    List<ProfileBeltDto> Belts);

public record ProfileBeltDto(int DisciplineId, int BeltId, int Degree);

public class UpdateProfileRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [StringLength(100)]
    public string? LastName { get; init; }
}

public class UpdateBeltsRequest
{
    [Required]
    public List<BeltRequest> Belts { get; init; } = [];
}

public record BeltRequest(int DisciplineId, int BeltId, int Degree);
