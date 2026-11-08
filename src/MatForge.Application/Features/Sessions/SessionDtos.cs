using System.ComponentModel.DataAnnotations;
using MatForge.Data.Enums;

namespace MatForge.Application.Features.Sessions;

public record SessionDto(
    int Id,
    SessionKind Kind,
    DateOnly Date,
    int DurationMinutes,
    int Feeling,
    string? Notes,
    int? DisciplineId,
    int? FormatId,
    int? CharacterId,
    int? ClubId,
    int? ActivityTypeId,
    List<RoundDto> Rounds);

public record RoundDto(RoundResult Result, int? TechniqueId, int? PositionId, string? Notes);

public record SessionSummaryDto(
    int Id,
    SessionKind Kind,
    DateOnly Date,
    int DurationMinutes,
    int Feeling,
    int? DisciplineId,
    int? FormatId,
    int? CharacterId,
    int? ClubId,
    int? ActivityTypeId,
    int RoundsCount,
    int Wins,
    int Losses,
    int Draws);

// Kind, Date and Result are nullable so that a missing value fails [Required] instead of becoming the C# default
public class SessionRequest
{
    [Required]
    public SessionKind? Kind { get; init; }

    [Required]
    public DateOnly? Date { get; init; }

    [Range(1, 600)]
    public int DurationMinutes { get; init; }

    [Range(1, 5)]
    public int Feeling { get; init; }

    [StringLength(4000)]
    public string? Notes { get; init; }

    public int? DisciplineId { get; init; }
    public int? FormatId { get; init; }
    public int? CharacterId { get; init; }
    public int? ClubId { get; init; }
    public int? ActivityTypeId { get; init; }

    [MaxLength(50)]
    public List<RoundRequest> Rounds { get; init; } = [];
}

public class RoundRequest
{
    [Required]
    public RoundResult? Result { get; init; }

    public int? TechniqueId { get; init; }
    public int? PositionId { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}
