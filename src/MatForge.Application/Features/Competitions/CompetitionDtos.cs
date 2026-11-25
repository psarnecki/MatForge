using System.ComponentModel.DataAnnotations;
using MatForge.Data.Enums;

namespace MatForge.Application.Features.Competitions;

public record CompetitionDto(
    int Id,
    string Name,
    DateOnly Date,
    string? Location,
    int DisciplineId,
    int FormatId,
    string? AgeCategory,
    string? SkillLevel,
    string? WeightCategory,
    int? Place,
    string? ResultNote,
    string? Notes,
    List<FightDto> Fights);

public record FightDto(
    string? OpponentName,
    FightResult Result,
    FightMethod? Method,
    int? TechniqueId,
    int? DurationSeconds,
    string? Notes);

public record CompetitionSummaryDto(
    int Id,
    string Name,
    DateOnly Date,
    string? Location,
    int DisciplineId,
    int FormatId,
    string? AgeCategory,
    string? SkillLevel,
    string? WeightCategory,
    int? Place,
    string? ResultNote,
    int FightsCount,
    int Wins,
    int Losses,
    int Draws);

// Date, DisciplineId, FormatId and Result are nullable so that a missing value fails [Required] instead of becoming the C# default
public class CompetitionRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public DateOnly? Date { get; init; }

    [StringLength(150)]
    public string? Location { get; init; }

    [Required]
    public int? DisciplineId { get; init; }

    [Required]
    public int? FormatId { get; init; }

    [StringLength(50)]
    public string? AgeCategory { get; init; }

    [StringLength(50)]
    public string? SkillLevel { get; init; }

    [StringLength(50)]
    public string? WeightCategory { get; init; }

    [Range(1, 1000)]
    public int? Place { get; init; }

    [StringLength(100)]
    public string? ResultNote { get; init; }

    [StringLength(4000)]
    public string? Notes { get; init; }

    [MaxLength(30)]
    public List<FightRequest> Fights { get; init; } = [];
}

public class FightRequest
{
    [StringLength(100)]
    public string? OpponentName { get; init; }

    [Required]
    public FightResult? Result { get; init; }

    public FightMethod? Method { get; init; }
    public int? TechniqueId { get; init; }

    [Range(1, 3600)]
    public int? DurationSeconds { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}

public class CompetitionListQuery
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}
