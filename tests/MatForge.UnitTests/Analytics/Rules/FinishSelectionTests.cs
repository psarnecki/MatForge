using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Rules;
using static MatForge.UnitTests.Analytics.Rules.RuleInputs;

namespace MatForge.UnitTests.Analytics.Rules;

public class FinishSelectionTests
{
    [Fact]
    public void Highest_share_comes_first()
    {
        List<FinishGroupDto> candidates = [new(1, "Armbar", 3, 0.3m), new(2, "Kimura", 5, 0.5m)];

        Names(FinishSelection.Top(10, candidates, TrainingProfile, 0.3m, 3)).Should().Equal("Kimura", "Armbar");
    }

    [Fact]
    public void Equal_shares_are_ordered_by_count()
    {
        // Rounded shares can be equal while counts differ when the total is large
        List<FinishGroupDto> candidates = [new(1, "Armbar", 6000, 0.3m), new(2, "Kimura", 6001, 0.3m)];

        Names(FinishSelection.Top(20003, candidates, TrainingProfile, 0.3m, 3)).Should().Equal("Kimura", "Armbar");
    }

    [Fact]
    public void Equal_shares_and_counts_are_ordered_by_name_with_letters_before_case()
    {
        // Ordinal comparison would put "Bicep Slicer" before "armbar"
        List<FinishGroupDto> candidates =
            [new(2, "Triangle Choke", 3, 0.3m), new(30, "Bicep Slicer", 3, 0.3m), new(25, "armbar", 3, 0.3m)];

        Names(FinishSelection.Top(10, candidates, TrainingProfile, 0.3m, 3)).Should().Equal("armbar", "Bicep Slicer", "Triangle Choke");
    }

    [Fact]
    public void Equal_names_are_ordered_by_id()
    {
        List<FinishGroupDto> candidates = [new(7, "Armbar", 3, 0.3m), new(4, "Armbar", 3, 0.3m)];

        FinishSelection.Top(10, candidates, TrainingProfile, 0.3m, 3).Select(c => c.Id).Should().Equal(4, 7);
    }

    [Fact]
    public void Limit_is_applied_after_the_thresholds()
    {
        List<FinishGroupDto> candidates = [new(1, "Armbar", 2, 0.6m), new(2, "Kimura", 3, 0.4m)];

        Names(FinishSelection.Top(5, candidates, TrainingProfile, 0.3m, 1)).Should().Equal("Kimura");
    }

    [Fact]
    public void Block_below_min_finishes_gives_nothing()
    {
        List<FinishGroupDto> candidates = [new(1, "Armbar", 4, 1m)];

        FinishSelection.Top(4, candidates, TrainingProfile, 0.3m, 3).Should().BeEmpty();
    }

    [Fact]
    public void No_candidates_give_nothing()
    {
        FinishSelection.Top(10, [], TrainingProfile, 0.3m, 3).Should().BeEmpty();
    }

    private static IEnumerable<string> Names(List<FinishGroupDto> selected) => selected.Select(c => c.Name);
}
