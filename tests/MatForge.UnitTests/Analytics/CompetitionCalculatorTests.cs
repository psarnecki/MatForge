using System.Globalization;
using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Data.Enums;

namespace MatForge.UnitTests.Analytics;

public class CompetitionCalculatorTests
{
    private const int MinSamples = 5;
    private const string Day = "2026-09-12";
    private const FightMethod Submission = FightMethod.Submission;

    private static readonly (int Id, string Name) Choke = (1, "Choke");
    private static readonly (int Id, string Name) ArmLock = (2, "Arm Lock");
    private static readonly (int Id, string Name) ShoulderLock = (3, "Shoulder Lock");
    private static readonly (int Id, string Name) OtherCategory = (11, "Other");

    // IDs do not follow name order, so sorting ties by ID would give a different result
    private static readonly TechniqueEntry Rnc = new(1, "Rear Naked Choke", Choke);
    private static readonly TechniqueEntry Triangle = new(2, "Triangle Choke", Choke);
    private static readonly TechniqueEntry Guillotine = new(3, "Guillotine", Choke);
    private static readonly TechniqueEntry Armbar = new(20, "Armbar", ArmLock);
    private static readonly TechniqueEntry Kimura = new(25, "Kimura", ShoulderLock);
    private static readonly TechniqueEntry OtherFinish = new(61, "Other Finish", OtherCategory);

    // The previous period of September is 2026-08-02 to 2026-08-31
    private static readonly DateRange September = new(Date("2026-09-01"), Date("2026-09-30"));

    [Fact]
    public void GT06_competitions_match_the_reference_numbers()
    {
        List<CompetitionRow> competitions =
        [
            Competition(1, "2026-09-05", 1, Win(Submission, Rnc), Win(Submission, Armbar), Win(FightMethod.Points)),
            Competition(2, "2026-09-12", 2, Win(Submission, Kimura), Win(FightMethod.Decision), Loss(FightMethod.Points)),
            Competition(3, "2026-09-26", 5, Loss(Submission, Guillotine), Loss(FightMethod.Points), Loss(FightMethod.Points))
        ];

        var result = Calculate(competitions);

        result.Totals.Should().Be(new FightTotalsDto(3, 9, 5, 4, 0, 0.5556m, 1, 1, 0));
        result.Methods.Wins.Should().Equal(
            new MethodGroupDto(Submission, 3, 0.6m),
            new MethodGroupDto(FightMethod.Decision, 1, 0.2m),
            new MethodGroupDto(FightMethod.Points, 1, 0.2m));
        result.Methods.Losses.Should().Equal(
            new MethodGroupDto(FightMethod.Points, 3, 0.75m),
            new MethodGroupDto(Submission, 1, 0.25m));
        result.FinishesFor.Total.Should().Be(3);
        result.FinishesAgainst.Total.Should().Be(1);
    }

    [Fact]
    public void Medals_come_from_places_1_to_3_only()
    {
        List<CompetitionRow> competitions =
        [
            Competition(1, Day, 1), Competition(2, Day, 1), Competition(3, Day, 2), Competition(4, Day, 3),
            Competition(5, Day, 4), Competition(6, Day, null)
        ];

        var totals = Calculate(competitions).Totals;

        (totals.Gold, totals.Silver, totals.Bronze).Should().Be((2, 1, 1));
    }

    [Fact]
    public void Competition_without_fights_counts_in_competitions_medals_and_timeline()
    {
        var result = Calculate([Competition(4, Day, 1)]);

        result.Totals.Should().Be(new FightTotalsDto(1, 0, 0, 0, 0, null, 1, 0, 0));
        result.Timeline.Should().Equal(new TimelineItemDto(4, Date(Day), "Open 4", 1, 0, 0, 0));
    }

    [Fact]
    public void Draws_stay_in_the_win_rate_denominator_and_have_no_method()
    {
        var result = Calculate([Competition(1, Day, null, Win(FightMethod.Points), Draw())]);

        result.Totals.Should().Be(new FightTotalsDto(1, 2, 1, 0, 1, 0.5m, 0, 0, 0));
        result.Methods.Wins.Should().Equal(new MethodGroupDto(FightMethod.Points, 1, 1m));
        result.Methods.Losses.Should().BeEmpty();
    }

    [Fact]
    public void Walkover_and_disqualification_are_methods_but_not_finishes()
    {
        var result = Calculate([Competition(1, Day, null,
            Win(FightMethod.Walkover), Win(FightMethod.Disqualification), Loss(FightMethod.Disqualification))]);

        result.Methods.Wins.Should().Equal(
            new MethodGroupDto(FightMethod.Disqualification, 1, 0.5m),
            new MethodGroupDto(FightMethod.Walkover, 1, 0.5m));
        result.Methods.Losses.Should().Equal(new MethodGroupDto(FightMethod.Disqualification, 1, 1m));
        result.FinishesFor.Should().BeEquivalentTo(new FightFinishesDto(0, [], []));
        result.FinishesAgainst.Should().BeEquivalentTo(new FightFinishesDto(0, [], []));
    }

    [Fact]
    public void Submission_without_a_technique_is_not_a_finish()
    {
        var result = Calculate([Competition(1, Day, null, Win(Submission), Win(Submission, Rnc))]);

        result.Methods.Wins.Should().Equal(new MethodGroupDto(Submission, 2, 1m));
        result.FinishesFor.Should().BeEquivalentTo(new FightFinishesDto(1, [Item(Rnc, 1, 1m)], [Item(Choke, 1, 1m)]));
    }

    [Fact]
    public void Finishes_are_grouped_by_technique_and_category_with_other_entries_counted()
    {
        var result = Calculate([Competition(1, Day, null,
            Win(Submission, Rnc), Win(Submission, Rnc), Win(Submission, Triangle), Win(Submission, Kimura),
            Win(Submission, OtherFinish), Loss(Submission, Armbar))]);

        result.FinishesFor.Total.Should().Be(5);
        result.FinishesFor.ByTechnique.Should().Equal(
            Item(Rnc, 2, 0.4m), Item(Kimura, 1, 0.2m), Item(OtherFinish, 1, 0.2m), Item(Triangle, 1, 0.2m));
        result.FinishesFor.ByCategory.Should().Equal(
            Item(Choke, 3, 0.6m), Item(OtherCategory, 1, 0.2m), Item(ShoulderLock, 1, 0.2m));
        result.FinishesAgainst.Should().BeEquivalentTo(new FightFinishesDto(1, [Item(Armbar, 1, 1m)], [Item(ArmLock, 1, 1m)]));
    }

    [Fact]
    public void Methods_are_ordered_by_count_then_name()
    {
        var result = Calculate([Competition(1, Day, null,
            Win(FightMethod.Throw), Win(FightMethod.Points), Win(Submission), Win(FightMethod.Points), Win(FightMethod.Decision))]);

        result.Methods.Wins.Should().Equal(
            new MethodGroupDto(FightMethod.Points, 2, 0.4m),
            new MethodGroupDto(FightMethod.Decision, 1, 0.2m),
            new MethodGroupDto(Submission, 1, 0.2m),
            new MethodGroupDto(FightMethod.Throw, 1, 0.2m));
    }

    [Fact]
    public void Timeline_is_ordered_by_date_then_id_with_the_record_of_each_competition()
    {
        List<CompetitionRow> competitions =
        [
            Competition(7, "2026-09-20", null, Win(FightMethod.Points)),
            Competition(9, "2026-09-05", 3, Win(FightMethod.Points), Loss(FightMethod.Points), Draw()),
            Competition(2, "2026-09-12", 1),
            Competition(3, "2026-09-05", 2, Loss(FightMethod.Decision))
        ];

        Calculate(competitions).Timeline.Should().Equal(
            new TimelineItemDto(3, Date("2026-09-05"), "Open 3", 2, 1, 0, 1),
            new TimelineItemDto(9, Date("2026-09-05"), "Open 9", 3, 3, 1, 1),
            new TimelineItemDto(2, Date("2026-09-12"), "Open 2", 1, 0, 0, 0),
            new TimelineItemDto(7, Date("2026-09-20"), "Open 7", null, 1, 1, 0));
    }

    [Fact]
    public void Each_competition_counts_only_in_its_own_period()
    {
        List<CompetitionRow> competitions =
        [
            Competition(1, "2026-08-01", 1, Win(FightMethod.Points)),
            Competition(2, "2026-08-02", null, Loss(FightMethod.Points)),
            Competition(3, "2026-08-31", null, Win(FightMethod.Points), Loss(FightMethod.Points)),
            Competition(4, "2026-09-01", 2, Win(FightMethod.Points)),
            Competition(5, "2026-09-30", null, Draw()),
            Competition(6, "2026-10-01", 3, Win(FightMethod.Points))
        ];

        var result = Calculate(competitions);

        result.Totals.Should().Be(new FightTotalsDto(2, 2, 1, 0, 1, 0.5m, 0, 1, 0));
        result.Timeline.Select(t => t.CompetitionId).Should().Equal(4, 5);
        result.Previous.Should().Be(new PreviousFightsDto(3, 0.3333m));
    }

    [Fact]
    public void Win_rate_delta_compares_with_the_previous_period()
    {
        List<CompetitionRow> competitions =
        [
            Competition(1, Day, null, Fights(3, 2)),
            Competition(2, "2026-08-15", null, Fights(1, 4))
        ];

        var result = Calculate(competitions);

        result.Totals.WinRate.Should().Be(0.6m);
        result.Previous.Should().Be(new PreviousFightsDto(5, 0.2m));
        result.WinRateDelta.Should().Be(0.4m);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(4, 5, false)]
    [InlineData(5, 4, false)]
    public void Delta_needs_min_samples_fights_in_both_periods(int fights, int previousFights, bool hasDelta)
    {
        List<CompetitionRow> competitions =
        [
            Competition(1, Day, null, Fights(fights, 0)),
            Competition(2, "2026-08-15", null, Fights(0, previousFights))
        ];

        Calculate(competitions).WinRateDelta.Should().Be(hasDelta ? 1m : null);
    }

    [Fact]
    public void Delta_is_computed_from_the_unrounded_rates()
    {
        // Subtracting the rounded rates would give 0.6667 - 0.3333 = 0.3334
        List<CompetitionRow> competitions =
        [
            Competition(1, Day, null, Fights(4, 2)),
            Competition(2, "2026-08-15", null, Fights(2, 4))
        ];

        var result = Calculate(competitions);

        result.Totals.WinRate.Should().Be(0.6667m);
        result.Previous.WinRate.Should().Be(0.3333m);
        result.WinRateDelta.Should().Be(0.3333m);
    }

    [Fact]
    public void No_competitions_give_zeros_and_nulls()
    {
        var result = Calculate([]);

        result.Totals.Should().Be(new FightTotalsDto(0, 0, 0, 0, 0, null, 0, 0, 0));
        result.Methods.Wins.Should().BeEmpty();
        result.Methods.Losses.Should().BeEmpty();
        result.FinishesFor.Should().BeEquivalentTo(new FightFinishesDto(0, [], []));
        result.FinishesAgainst.Should().BeEquivalentTo(new FightFinishesDto(0, [], []));
        result.Timeline.Should().BeEmpty();
        result.Previous.Should().Be(new PreviousFightsDto(0, null));
        result.WinRateDelta.Should().BeNull();
    }

    private static CompetitionAnalyticsDto Calculate(List<CompetitionRow> competitions) =>
        CompetitionCalculator.Calculate(September, competitions, MinSamples);

    private static CompetitionRow Competition(int id, string date, int? place, params FightRow[] fights) =>
        new(id, Date(date), $"Open {id}", place, [..fights]);

    private static FightRow Win(FightMethod method, TechniqueEntry? technique = null) =>
        Fight(FightResult.Win, method, technique);

    private static FightRow Loss(FightMethod method, TechniqueEntry? technique = null) =>
        Fight(FightResult.Loss, method, technique);

    private static FightRow Draw() => new(FightResult.Draw, null, null, null, null, null);

    private static FightRow Fight(FightResult result, FightMethod method, TechniqueEntry? technique) =>
        new(result, method, technique?.Id, technique?.Name, technique?.Category.Id, technique?.Category.Name);

    private static FightRow[] Fights(int wins, int losses) =>
        [..Enumerable.Repeat(Win(FightMethod.Points), wins), ..Enumerable.Repeat(Loss(FightMethod.Points), losses)];

    private static FinishGroupDto Item(TechniqueEntry technique, int count, decimal share) =>
        new(technique.Id, technique.Name, count, share);

    private static FinishGroupDto Item((int Id, string Name) entry, int count, decimal share) =>
        new(entry.Id, entry.Name, count, share);

    private static DateOnly Date(string value) => DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private record TechniqueEntry(int Id, string Name, (int Id, string Name) Category);
}
