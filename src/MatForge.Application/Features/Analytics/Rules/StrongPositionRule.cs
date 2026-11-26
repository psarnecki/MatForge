namespace MatForge.Application.Features.Analytics.Rules;

// Positions are recorded only in training
public class StrongPositionRule : IInsightRule
{
    public IEnumerable<InsightResult> Evaluate(InsightInput input) =>
        input.Training == null
            ? []
            : FinishSelection.Top(input.Training.For.Recorded, input.Training.For.Positions, input.Training.Thresholds,
                    input.PositionShare, limit: 1)
                .Select(position => new InsightResult(InsightSection.Training,
                    new InsightDto("StrongPosition", InsightType.Strength, position)));
}