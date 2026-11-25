using MatForge.Data.Enums;

namespace MatForge.Application.Features.Analytics;

public record OverviewDto(
    PeriodDto Period,
    SessionTotalsDto Sessions,
    List<SectionDto> BySection,
    List<SessionGroupDto> ByDiscipline,
    List<FormatGroupDto> ByFormat,
    List<ClubGroupDto> ByClub,
    List<SessionGroupDto> ByCharacter,
    List<WeekDto> Weekly,
    decimal AvgSessionsPerWeek,
    decimal ActiveWeeksShare);

public record PeriodDto(DateOnly From, DateOnly To, int Days);

public record SessionTotalsDto(int Count, int TotalMinutes, decimal? AvgFeeling);

public record SectionDto(SessionKind Kind, int Count, int Minutes);

public record SessionGroupDto(int Id, string Name, int Count, int Minutes, decimal Share);

public record FormatGroupDto(int Id, string Name, int DisciplineId, int Count, int Minutes, decimal Share);

// Id and Name are null for "Other club"
public record ClubGroupDto(int? Id, string? Name, int Count, int Minutes, decimal Share);

public record WeekDto(DateOnly WeekStart, int Count, int Minutes);
