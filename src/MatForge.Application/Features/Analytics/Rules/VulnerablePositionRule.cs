namespace MatForge.Application.Features.Analytics.Rules;

// Positions are recorded only in training
public class VulnerablePositionRule : IInsightRule
{
    public IEnumerable<InsightResult> Evaluate(InsightInput input) =>
        input.Training == null
            ? []
            : FinishSelection.Top(input.Training.Against.Recorded, input.Training.Against.Positions, input.Training.Thresholds,
                    input.PositionShare, limit: 1)
                .Select(position => new InsightResult(InsightSection.Training,
                    new InsightDto("VulnerablePosition", InsightType.Weakness, position)));
}