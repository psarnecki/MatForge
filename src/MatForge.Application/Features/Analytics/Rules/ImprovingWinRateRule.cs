namespace MatForge.Application.Features.Analytics.Rules;

// The change is null when either period has too few rounds or fights, so the rule does not fire
public class ImprovingWinRateRule : IInsightRule
{
    public IEnumerable<InsightResult> Evaluate(InsightInput input) =>
        input.Sides
            .Where(side => side.WinRateDelta >= side.Thresholds.TrendDelta)
            .Select(side => new InsightResult(side.Section, new InsightDto("ImprovingWinRate", InsightType.Trend,
                new WinRateChangeDto(side.WinRate, side.PreviousWinRate!.Value, side.WinRateDelta!.Value))));
}
