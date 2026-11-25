using Microsoft.Extensions.Options;

namespace MatForge.Application.Features.Analytics;

// Analytics thresholds bound from the Analytics:Rules configuration section
public class AnalyticsRulesOptions
{
    public const string Section = "Analytics:Rules";

    public TrainingThresholds Training { get; init; } = new();
    public RuleThresholds Competition { get; init; } = new();

    // Win rate gap between training and competitions, so it belongs to neither profile
    public decimal GapDelta { get; init; }
}

public class RuleThresholds
{
    // Rounds needed in both periods for a win rate change
    public int MinSamples { get; init; }

    // Finishes in the block for technique and category rules, recorded positions for position rules
    public int MinFinishes { get; init; }

    public int MinCount { get; init; }
    public decimal TechniqueShare { get; init; }
    public decimal TrendDelta { get; init; }
}

// Positions are recorded only in training
public class TrainingThresholds : RuleThresholds
{
    public decimal PositionShare { get; init; }
}

// Counts are at least 1, while shares and deltas are greater than 0 and at most 1
public class AnalyticsRulesOptionsValidator : IValidateOptions<AnalyticsRulesOptions>
{
    public ValidateOptionsResult Validate(string? name, AnalyticsRulesOptions options)
    {
        List<string> errors = [];
        CheckProfile("Training", options.Training, errors);
        CheckFraction("Training:PositionShare", options.Training.PositionShare, errors);
        CheckProfile("Competition", options.Competition, errors);
        CheckFraction("GapDelta", options.GapDelta, errors);

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void CheckProfile(string profile, RuleThresholds thresholds, List<string> errors)
    {
        CheckCount($"{profile}:MinSamples", thresholds.MinSamples, errors);
        CheckCount($"{profile}:MinFinishes", thresholds.MinFinishes, errors);
        CheckCount($"{profile}:MinCount", thresholds.MinCount, errors);
        CheckFraction($"{profile}:TechniqueShare", thresholds.TechniqueShare, errors);
        CheckFraction($"{profile}:TrendDelta", thresholds.TrendDelta, errors);
    }

    private static void CheckCount(string key, int value, List<string> errors)
    {
        if (value < 1)
        {
            errors.Add($"{AnalyticsRulesOptions.Section}:{key} must be at least 1.");
        }
    }

    private static void CheckFraction(string key, decimal value, List<string> errors)
    {
        if (value <= 0 || value > 1)
        {
            errors.Add($"{AnalyticsRulesOptions.Section}:{key} must be above 0 and at most 1.");
        }
    }
}
