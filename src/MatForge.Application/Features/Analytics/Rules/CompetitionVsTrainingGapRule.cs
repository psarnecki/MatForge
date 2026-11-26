using MatForge.Application.Features.Analytics.Calculators;

namespace MatForge.Application.Features.Analytics.Rules;

// Compares only when both sides have enough data, with the delta as competition minus training
public class CompetitionVsTrainingGapRule : IInsightRule
{
    public IEnumerable<InsightResult> Evaluate(InsightInput input)
    {
        if (input.Training == null || input.Competition == null)
        {
            return [];
        }

        var delta = AnalyticsMath.RateDifference(input.Competition.Wins, input.Competition.Samples,
            input.Training.Wins, input.Training.Samples);

        return Math.Abs(delta) < input.GapDelta
            ? []
            : [new InsightResult(InsightSection.Comparison, new InsightDto("CompetitionVsTrainingGap", InsightType.Info,
                new WinRateGapDto(input.Training.WinRate, input.Competition.WinRate, delta)))];
    }
}
