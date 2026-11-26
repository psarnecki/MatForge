namespace MatForge.Application.Features.Analytics.Rules;

public class DominantFinisherTechniqueRule : IInsightRule
{
    public IEnumerable<InsightResult> Evaluate(InsightInput input) =>
        input.Sides.SelectMany(side =>
            FinishSelection.Top(side.For.Total, side.For.Techniques, side.Thresholds, side.Thresholds.TechniqueShare, limit: 3)
                .Select(technique => new InsightResult(side.Section,
                    new InsightDto("DominantFinisherTechnique", InsightType.Strength, technique))));
}