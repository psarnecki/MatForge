namespace MatForge.Application.Features.Analytics;

// Analytics thresholds bound from the Analytics:Rules configuration section
public class AnalyticsRulesOptions
{
    public const string Section = "Analytics:Rules";

    public RuleThresholds Training { get; init; } = new();
}

public class RuleThresholds
{
    // Rounds needed in both periods for a win rate change
    public int MinSamples { get; init; }
}
