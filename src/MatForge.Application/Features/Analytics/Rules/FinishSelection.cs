namespace MatForge.Application.Features.Analytics.Rules;

public static class FinishSelection
{
    // Compares letters first and uses accents and case only to break ties
    private static readonly StringComparer Names = StringComparer.InvariantCulture;

    // Highest share first, then highest count, then name
    public static List<FinishGroupDto> Top(int total, List<FinishGroupDto> candidates, RuleThresholds thresholds,
        decimal minShare, int limit) =>
        total < thresholds.MinFinishes
            ? []
            : candidates
                .Where(c => c.Count >= thresholds.MinCount && c.Share >= minShare)
                .OrderByDescending(c => c.Share).ThenByDescending(c => c.Count).ThenBy(c => c.Name, Names).ThenBy(c => c.Id)
                .Take(limit)
                .ToList();
}
