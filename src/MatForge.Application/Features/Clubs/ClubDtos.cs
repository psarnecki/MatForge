using System.ComponentModel.DataAnnotations;

namespace MatForge.Application.Features.Clubs;

public record ClubDto(int Id, string Name);

public class ClubRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;
}
