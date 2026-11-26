using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Application.Features.Analytics.Rules;

namespace MatForge.UnitTests.Analytics.Rules;

// Builds rule inputs with the thresholds of the spec unless a test passes its own
public static class RuleInputs
{
    public static readonly TrainingThresholds TrainingProfile = new()
    {
        MinSamples = 10, MinFinishes = 5, MinCount = 3, TechniqueShare = 0.30m, PositionShare = 0.40m, TrendDelta = 0.10m
    };

    public static readonly RuleThresholds CompetitionProfile = new()
    {
        MinSamples = 5, MinFinishes = 3, MinCount = 2, TechniqueShare = 0.30m, TrendDelta = 0.10m
    };

    public static readonly AnalyticsRulesOptions SpecOptions = new()
    {
        Training = TrainingProfile, Competition = CompetitionProfile, GapDelta = 0.20m
    };

    // Catalog order, as registered in the app
    public static readonly IInsightRule[] AllRules =
    [
        new DominantFinisherTechniqueRule(),
        new DominantFinisherCategoryRule(),
        new RecurringLossTechniqueRule(),
        new RecurringLossCategoryRule(),
        new StrongPositionRule(),
        new VulnerablePositionRule(),
        new ImprovingWinRateRule(),
        new DecliningWinRateRule(),
        new CompetitionVsTrainingGapRule()
    ];

    public static InsightFinishes NoFinishes => new(0, [], [], 0, []);

    public static InsightSide Side(InsightSection section, RuleThresholds? thresholds = null, int samples = 10, int wins = 5,
        decimal? previousWinRate = null, decimal? winRateDelta = null, InsightFinishes? finishesFor = null,
        InsightFinishes? finishesAgainst = null) =>
        new(section,
            thresholds ?? (section == InsightSection.Training ? TrainingProfile : CompetitionProfile),
            samples, wins, AnalyticsMath.Share(wins, samples),
            previousWinRate, winRateDelta,
            finishesFor ?? NoFinishes, finishesAgainst ?? NoFinishes);

    public static InsightInput Input(InsightSide? training = null, InsightSide? competition = null,
        decimal positionShare = 0.40m, decimal gapDelta = 0.20m) =>
        new(training, competition, positionShare, gapDelta);

    public static InsightSection SectionOf(string profile) =>
        profile == "Training" ? InsightSection.Training : InsightSection.Competition;
}
