namespace MatForge.Application.Features.Analytics.Rules;

public class RecurringLossCategoryRule : IInsightRule
{
    public IEnumerable<InsightResult> Evaluate(InsightInput input) =>
        input.Sides.SelectMany(side =>
            FinishSelection.Top(side.Against.Total, side.Against.Categories, side.Thresholds, side.Thresholds.TechniqueShare, limit: 1)
                .Select(category => new InsightResult(side.Section,
                    new InsightDto("RecurringLossCategory", InsightType.Weakness, category))));
}