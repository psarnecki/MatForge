using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Data.Enums;

namespace MatForge.Application.Features.Analytics;

// Both dates are optional, the service fills in any missing dates
public class AnalyticsQuery
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

// Both filters are optional and limit the data to matching grappling sessions and competitions
public class AnalyticsFilterQuery : AnalyticsQuery
{
    public int? DisciplineId { get; init; }
    public int? FormatId { get; init; }
}

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

public record SparringDto(RoundTotalsDto Totals, FinishesDto FinishesFor, FinishesDto FinishesAgainst, TrendDto Trend);

public record RoundTotalsDto(int Rounds, int Wins, int Losses, int Draws, decimal? WinRate, int Balance);

public record FinishesDto(
    int Total,
    List<FinishGroupDto> ByTechnique,
    List<FinishGroupDto> ByCategory,
    PositionFinishesDto ByPosition);

public record FinishGroupDto(int Id, string Name, int Count, decimal Share);

// Only finishes with a recorded position
public record PositionFinishesDto(int Recorded, List<FinishGroupDto> Items);

public record TrendDto(Bucket Bucket, List<TrendPointDto> Points, PreviousPeriodDto Previous, decimal? WinRateDelta);

public record TrendPointDto(DateOnly Start, int Rounds, int Wins, int Losses, int Draws, decimal? WinRate);

public record PreviousPeriodDto(int Rounds, decimal? WinRate);

public record CompetitionAnalyticsDto(
    FightTotalsDto Totals,
    MethodsDto Methods,
    FightFinishesDto FinishesFor,
    FightFinishesDto FinishesAgainst,
    List<TimelineItemDto> Timeline,
    PreviousFightsDto Previous,
    decimal? WinRateDelta);

public record FightTotalsDto(
    int Competitions,
    int Fights,
    int Wins,
    int Losses,
    int Draws,
    decimal? WinRate,
    int Gold,
    int Silver,
    int Bronze);

public record MethodsDto(List<MethodGroupDto> Wins, List<MethodGroupDto> Losses);

public record MethodGroupDto(FightMethod Method, int Count, decimal Share);

public record FightFinishesDto(int Total, List<FinishGroupDto> ByTechnique, List<FinishGroupDto> ByCategory);

public record TimelineItemDto(int CompetitionId, DateOnly Date, string Name, int? Place, int Fights, int Wins, int Losses);

public record PreviousFightsDto(int Fights, decimal? WinRate);
