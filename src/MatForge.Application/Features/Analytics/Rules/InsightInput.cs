namespace MatForge.Application.Features.Analytics.Rules;

// A side with too few rounds or fights is null, so no rule sees it
public record InsightInput(InsightSide? Training, InsightSide? Competition, decimal PositionShare, decimal GapDelta)
{
    public IEnumerable<InsightSide> Sides => new[] { Training, Competition }.OfType<InsightSide>();
}

// Samples are the rounds or fights in the period, compared with the thresholds of the side's own profile
public record InsightSide(
    InsightSection Section,
    RuleThresholds Thresholds,
    int Samples,
    int Wins,
    decimal WinRate,
    decimal? PreviousWinRate,
    decimal? WinRateDelta,
    InsightFinishes For,
    InsightFinishes Against);

// The lists leave out "Other" entries, while Total and Recorded still count them
public record InsightFinishes(
    int Total,
    List<FinishGroupDto> Techniques,
    List<FinishGroupDto> Categories,
    int Recorded,
    List<FinishGroupDto> Positions);
