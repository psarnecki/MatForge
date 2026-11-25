using MatForge.Data.Enums;

namespace MatForge.Application.Features.Analytics.Calculators;

// Discipline, format and character are null for "Other sessions" and club is null for "Other club"
public record SessionRow(
    DateOnly Date,
    SessionKind Kind,
    int DurationMinutes,
    int Feeling,
    int? DisciplineId,
    string? DisciplineName,
    int? FormatId,
    string? FormatName,
    int? CharacterId,
    string? CharacterName,
    int? ClubId,
    string? ClubName);
