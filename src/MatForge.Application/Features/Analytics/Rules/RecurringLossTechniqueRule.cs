namespace MatForge.Application.Features.Analytics.Rules;

public class RecurringLossTechniqueRule : IInsightRule
{
    public IEnumerable<InsightResult> Evaluate(InsightInput input) =>
        input.Sides.SelectMany(side =>
            FinishSelection.Top(side.Against.Total, side.Against.Techniques, side.Thresholds, side.Thresholds.TechniqueShare, limit: 3)
                .Select(technique => new InsightResult(side.Section,
                    new InsightDto("RecurringLossTechnique", InsightType.Weakness, technique))));
}