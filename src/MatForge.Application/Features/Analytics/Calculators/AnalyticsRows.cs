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

// Date comes from the session, while technique, category and position are null when not recorded
public record RoundRow(
    DateOnly Date,
    RoundResult Result,
    int? TechniqueId,
    string? TechniqueName,
    int? CategoryId,
    string? CategoryName,
    int? PositionId,
    string? PositionName);

// Competitions without fights still count in competitions, medals and the timeline
public record CompetitionRow(int Id, DateOnly Date, string Name, int? Place, List<FightRow> Fights);

// Method is null for draws, while technique and category are null when not recorded
public record FightRow(
    FightResult Result,
    FightMethod? Method,
    int? TechniqueId,
    string? TechniqueName,
    int? CategoryId,
    string? CategoryName);

// Dictionary entries included in distributions but excluded from insight rules
public record OtherEntries(int TechniqueId, int CategoryId, int PositionId);
