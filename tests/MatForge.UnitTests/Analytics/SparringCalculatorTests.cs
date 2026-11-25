using System.Globalization;
using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Data.Enums;

namespace MatForge.UnitTests.Analytics;

public class SparringCalculatorTests
{
    private const int MinSamples = 10;
    private const string Day = "2026-09-15";

    private static readonly (int Id, string Name) Choke = (1, "Choke");
    private static readonly (int Id, string Name) ShoulderLock = (3, "Shoulder Lock");
    private static readonly (int Id, string Name) LegLock = (4, "Leg Lock");
    private static readonly (int Id, string Name) OtherCategory = (11, "Other");

    // IDs do not follow name order, so sorting ties by ID would give a different result
    private static readonly TechniqueEntry Rnc = new(1, "Rear Naked Choke", Choke);
    private static readonly TechniqueEntry Triangle = new(2, "Triangle Choke", Choke);
    private static readonly TechniqueEntry Guillotine = new(3, "Guillotine", Choke);
    private static readonly TechniqueEntry Kimura = new(25, "Kimura", ShoulderLock);
    private static readonly TechniqueEntry HeelHook = new(30, "Inside Heel Hook", LegLock);
    private static readonly TechniqueEntry OtherFinish = new(61, "Other Finish", OtherCategory);

    private static readonly (int Id, string Name) ClosedGuard = (6, "Closed Guard");
    private static readonly (int Id, string Name) BackControl = (15, "Back Control");
    private static readonly (int Id, string Name) Saddle = (19, "Saddle / Cross Ashi");
    private static readonly (int Id, string Name) OtherPosition = (24, "Other");

    // The previous period of September is 2026-08-02 to 2026-08-31
    private static readonly DateRange September = Range("2026-09-01", "2026-09-30");

    [Fact]
    public void GT01_distribution_matches_the_reference_numbers()
    {
        List<RoundRow> rounds =
        [
            ..Times(3, Win(Rnc, BackControl)),
            Win(Rnc),
            ..Times(2, Win(Triangle, ClosedGuard)),
            ..Times(2, Win(Kimura)),
            ..Times(4, Win()),
            ..Times(2, Loss(HeelHook, Saddle)),
            Loss(Guillotine),
            Loss(Rnc, BackControl),
            ..Times(2, Loss()),
            ..Times(2, Draw())
        ];

        var sparring = Calculate(rounds);

        sparring.Totals.Should().Be(new RoundTotalsDto(20, 12, 6, 2, 0.6m, 6));

        sparring.FinishesFor.Total.Should().Be(8);
        sparring.FinishesFor.ByTechnique.Should().Equal(Item(Rnc, 4, 0.5m), Item(Kimura, 2, 0.25m), Item(Triangle, 2, 0.25m));
        sparring.FinishesFor.ByCategory.Should().Equal(Item(Choke, 6, 0.75m), Item(ShoulderLock, 2, 0.25m));
        sparring.FinishesFor.ByPosition.Recorded.Should().Be(5);
        sparring.FinishesFor.ByPosition.Items.Should().Equal(Item(BackControl, 3, 0.6m), Item(ClosedGuard, 2, 0.4m));

        sparring.FinishesAgainst.Total.Should().Be(4);
        sparring.FinishesAgainst.ByTechnique.Should().Equal(
            Item(HeelHook, 2, 0.5m), Item(Guillotine, 1, 0.25m), Item(Rnc, 1, 0.25m));
        sparring.FinishesAgainst.ByCategory.Should().Equal(Item(Choke, 2, 0.5m), Item(LegLock, 2, 0.5m));
        sparring.FinishesAgainst.ByPosition.Recorded.Should().Be(3);
        sparring.FinishesAgainst.ByPosition.Items.Should().Equal(Item(Saddle, 2, 0.6667m), Item(BackControl, 1, 0.3333m));
    }

    [Fact]
    public void GT03_win_rate_delta_matches_the_reference_numbers()
    {
        List<RoundRow> rounds = [..Rounds(12, 8, Day), ..Rounds(7, 8, "2026-08-15")];

        var sparring = Calculate(rounds);

        sparring.Totals.WinRate.Should().Be(0.6m);
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(15, 0.4667m));
        sparring.Trend.WinRateDelta.Should().Be(0.1333m);
    }

    [Fact]
    public void GT03_previous_period_below_min_samples_has_no_delta()
    {
        List<RoundRow> rounds = [..Rounds(12, 8, Day), ..Rounds(3, 5, "2026-08-15")];

        var trend = Calculate(rounds).Trend;

        trend.Previous.Should().Be(new PreviousPeriodDto(8, 0.375m));
        trend.WinRateDelta.Should().BeNull();
    }

    [Fact]
    public void Delta_is_computed_from_the_unrounded_rates()
    {
        // Subtracting the rounded rates would give 0.6667 - 0.3333 = 0.3334
        List<RoundRow> rounds = [..Rounds(10, 5, Day), ..Rounds(5, 10, "2026-08-15")];

        var sparring = Calculate(rounds);

        sparring.Totals.WinRate.Should().Be(0.6667m);
        sparring.Trend.Previous.WinRate.Should().Be(0.3333m);
        sparring.Trend.WinRateDelta.Should().Be(0.3333m);
    }

    [Theory]
    [InlineData(7, 15, 12, 20, "-0.1333")]
    [InlineData(10, 20, 15, 32, "0.0313")]
    [InlineData(15, 32, 10, 20, "-0.0313")]
    public void Delta_is_current_minus_previous_rounded_half_away_from_zero(int wins, int rounds, int previousWins,
        int previousRounds, string delta)
    {
        List<RoundRow> rows = [..Rounds(wins, rounds - wins, Day), ..Rounds(previousWins, previousRounds - previousWins, "2026-08-15")];

        Calculate(rows).Trend.WinRateDelta.Should().Be(Number(delta));
    }

    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(9, 10, false)]
    [InlineData(10, 9, false)]
    public void Delta_needs_min_samples_rounds_in_both_periods(int rounds, int previousRounds, bool hasDelta)
    {
        List<RoundRow> rows = [..Rounds(rounds, 0, Day), ..Rounds(0, previousRounds, "2026-08-15")];

        Calculate(rows).Trend.WinRateDelta.Should().Be(hasDelta ? 1m : null);
    }

    [Fact]
    public void No_rounds_give_zeros_and_nulls()
    {
        var sparring = Calculate([]);

        sparring.Totals.Should().Be(new RoundTotalsDto(0, 0, 0, 0, null, 0));
        ExpectNoFinishes(sparring.FinishesFor);
        ExpectNoFinishes(sparring.FinishesAgainst);
        sparring.Trend.Bucket.Should().Be(Bucket.Week);
        sparring.Trend.Points.Should().HaveCount(5).And.OnlyContain(p => p.Rounds == 0 && p.WinRate == null);
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(0, null));
        sparring.Trend.WinRateDelta.Should().BeNull();
    }

    [Fact]
    public void Wins_and_losses_without_a_technique_are_not_finishes()
    {
        var sparring = Calculate([..Times(3, Win()), ..Times(2, Loss())]);

        sparring.Totals.Should().Be(new RoundTotalsDto(5, 3, 2, 0, 0.6m, 1));
        ExpectNoFinishes(sparring.FinishesFor);
        ExpectNoFinishes(sparring.FinishesAgainst);
    }

    [Fact]
    public void Finish_without_a_position_counts_everywhere_but_in_positions()
    {
        var finishes = Calculate([Win(Rnc), Win(Rnc, BackControl), Win(Kimura)]).FinishesFor;

        finishes.Total.Should().Be(3);
        finishes.ByTechnique.Should().Equal(Item(Rnc, 2, 0.6667m), Item(Kimura, 1, 0.3333m));
        finishes.ByCategory.Should().Equal(Item(Choke, 2, 0.6667m), Item(ShoulderLock, 1, 0.3333m));
        finishes.ByPosition.Recorded.Should().Be(1);
        finishes.ByPosition.Items.Should().Equal(Item(BackControl, 1, 1m));
    }

    [Fact]
    public void Draws_stay_in_the_win_rate_denominator()
    {
        var sparring = Calculate([Win(), Draw()]);

        sparring.Totals.Should().Be(new RoundTotalsDto(2, 1, 0, 1, 0.5m, 1));
        sparring.Trend.Points.Single(p => p.Rounds > 0).WinRate.Should().Be(0.5m);
    }

    [Fact]
    public void Other_entries_are_counted()
    {
        var finishes = Calculate([..Times(2, Win(OtherFinish, OtherPosition)), Win(Rnc, BackControl)]).FinishesFor;

        finishes.ByTechnique.Should().Equal(Item(OtherFinish, 2, 0.6667m), Item(Rnc, 1, 0.3333m));
        finishes.ByCategory.Should().Equal(Item(OtherCategory, 2, 0.6667m), Item(Choke, 1, 0.3333m));
        finishes.ByPosition.Items.Should().Equal(Item(OtherPosition, 2, 0.6667m), Item(BackControl, 1, 0.3333m));
    }

    [Theory]
    [InlineData("2026-04-02", Bucket.Week, 14, "2025-12-29", "2026-03-30")]
    [InlineData("2026-04-03", Bucket.Month, 4, "2026-01-01", "2026-04-01")]
    public void Bucket_is_week_up_to_92_days_and_month_from_93(string to, Bucket bucket, int points, string firstStart,
        string lastStart)
    {
        var trend = SparringCalculator.Calculate(Range("2026-01-01", to), [], MinSamples).Trend;

        trend.Bucket.Should().Be(bucket);
        trend.Points.Should().HaveCount(points);
        trend.Points[0].Start.Should().Be(Date(firstStart));
        trend.Points[^1].Start.Should().Be(Date(lastStart));
    }

    [Fact]
    public void Trend_includes_empty_buckets_with_a_null_win_rate()
    {
        var points = Calculate([Win(date: "2026-09-08"), Win(date: "2026-09-09"), Loss(date: "2026-09-10"), Draw("2026-09-11")])
            .Trend.Points;

        points.Should().Equal(
            new TrendPointDto(Date("2026-08-31"), 0, 0, 0, 0, null),
            new TrendPointDto(Date("2026-09-07"), 4, 2, 1, 1, 0.5m),
            new TrendPointDto(Date("2026-09-14"), 0, 0, 0, 0, null),
            new TrendPointDto(Date("2026-09-21"), 0, 0, 0, 0, null),
            new TrendPointDto(Date("2026-09-28"), 0, 0, 0, 0, null));
    }

    [Fact]
    public void Month_bucket_counts_only_rounds_inside_the_period()
    {
        // The 137-day period has a previous period of 2025-08-31 to 2026-01-14, with the January bucket starting inside it
        var period = Range("2026-01-15", "2026-05-31");
        List<RoundRow> rounds = [Loss(date: "2026-01-10"), Win(date: "2026-01-15"), Win(date: "2026-05-31"), Win(date: "2026-06-01")];

        var sparring = SparringCalculator.Calculate(period, rounds, MinSamples);

        sparring.Totals.Rounds.Should().Be(2);
        sparring.Trend.Bucket.Should().Be(Bucket.Month);
        sparring.Trend.Points.Should().Equal(
            new TrendPointDto(Date("2026-01-01"), 1, 1, 0, 0, 1m),
            new TrendPointDto(Date("2026-02-01"), 0, 0, 0, 0, null),
            new TrendPointDto(Date("2026-03-01"), 0, 0, 0, 0, null),
            new TrendPointDto(Date("2026-04-01"), 0, 0, 0, 0, null),
            new TrendPointDto(Date("2026-05-01"), 1, 1, 0, 0, 1m));
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(1, 0m));
    }

    [Fact]
    public void Week_bucket_counts_only_rounds_inside_the_period()
    {
        // From Sunday to Monday, the previous period is 2026-08-28 to 2026-09-05
        var period = Range("2026-09-06", "2026-09-14");
        List<RoundRow> rounds = [Win(date: "2026-09-05"), Loss(date: "2026-09-06"), Win(date: "2026-09-14"), Win(date: "2026-09-15")];

        var sparring = SparringCalculator.Calculate(period, rounds, MinSamples);

        sparring.Trend.Points.Should().Equal(
            new TrendPointDto(Date("2026-08-31"), 1, 0, 1, 0, 0m),
            new TrendPointDto(Date("2026-09-07"), 0, 0, 0, 0, null),
            new TrendPointDto(Date("2026-09-14"), 1, 1, 0, 0, 1m));
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(1, 1m));
    }

    [Fact]
    public void Each_round_counts_only_in_its_own_period()
    {
        List<RoundRow> rounds =
        [
            Win(date: "2026-08-01"),
            Loss(date: "2026-08-02"),
            Loss(date: "2026-08-31"),
            Win(date: "2026-09-01"),
            Draw("2026-09-30"),
            Win(date: "2026-10-01")
        ];

        var sparring = Calculate(rounds);

        sparring.Totals.Should().Be(new RoundTotalsDto(2, 1, 0, 1, 0.5m, 1));
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(2, 0m));
    }

    [Fact]
    public void One_day_period_has_one_point_and_the_day_before_as_previous()
    {
        var period = Range("2026-09-15", "2026-09-15");
        List<RoundRow> rounds = [Win(date: "2026-09-13"), Loss(date: "2026-09-14"), Win(date: "2026-09-15")];

        var sparring = SparringCalculator.Calculate(period, rounds, MinSamples);

        sparring.Totals.Rounds.Should().Be(1);
        sparring.Trend.Points.Should().Equal(new TrendPointDto(Date("2026-09-14"), 1, 1, 0, 0, 1m));
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(1, 0m));
    }

    private static SparringDto Calculate(List<RoundRow> rounds) => SparringCalculator.Calculate(September, rounds, MinSamples);

    private static void ExpectNoFinishes(FinishesDto finishes)
    {
        finishes.Total.Should().Be(0);
        finishes.ByTechnique.Should().BeEmpty();
        finishes.ByCategory.Should().BeEmpty();
        finishes.ByPosition.Recorded.Should().Be(0);
        finishes.ByPosition.Items.Should().BeEmpty();
    }

    private static RoundRow Win(TechniqueEntry? technique = null, (int Id, string Name)? position = null, string date = Day) =>
        Round(RoundResult.Win, technique, position, date);

    private static RoundRow Loss(TechniqueEntry? technique = null, (int Id, string Name)? position = null, string date = Day) =>
        Round(RoundResult.Loss, technique, position, date);

    private static RoundRow Draw(string date = Day) => Round(RoundResult.Draw, null, null, date);

    private static RoundRow Round(RoundResult result, TechniqueEntry? technique, (int Id, string Name)? position, string date) =>
        new(Date(date), result, technique?.Id, technique?.Name, technique?.Category.Id, technique?.Category.Name,
            position?.Id, position?.Name);

    private static IEnumerable<RoundRow> Rounds(int wins, int losses, string date) =>
        [..Times(wins, Win(date: date)), ..Times(losses, Loss(date: date))];

    private static IEnumerable<RoundRow> Times(int count, RoundRow round) => Enumerable.Repeat(round, count);

    private static FinishGroupDto Item(TechniqueEntry technique, int count, decimal share) =>
        new(technique.Id, technique.Name, count, share);

    private static FinishGroupDto Item((int Id, string Name) entry, int count, decimal share) =>
        new(entry.Id, entry.Name, count, share);

    private static decimal Number(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static DateOnly Date(string value) => DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private static DateRange Range(string from, string to) => new(Date(from), Date(to));

    private record TechniqueEntry(int Id, string Name, (int Id, string Name) Category);
}
