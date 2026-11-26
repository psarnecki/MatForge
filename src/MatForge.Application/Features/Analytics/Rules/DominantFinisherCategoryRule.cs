namespace MatForge.Application.Features.Analytics.Rules;

public class DominantFinisherCategoryRule : IInsightRule
{
    public IEnumerable<InsightResult> Evaluate(InsightInput input) =>
        input.Sides.SelectMany(side =>
            FinishSelection.Top(side.For.Total, side.For.Categories, side.Thresholds, side.Thresholds.TechniqueShare, limit: 1)
                .Select(category => new InsightResult(side.Section,
                    new InsightDto("DominantFinisherCategory", InsightType.Strength, category))));
}