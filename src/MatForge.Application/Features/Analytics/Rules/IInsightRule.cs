namespace MatForge.Application.Features.Analytics.Rules;

// One rule of the catalog, which returns nothing when it does not fire
public interface IInsightRule
{
    IEnumerable<InsightResult> Evaluate(InsightInput input);
}

public enum InsightSection
{
    Training,
    Competition,
    Comparison
}

public record InsightResult(InsightSection Section, InsightDto Insight);
