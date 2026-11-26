using System.Globalization;
using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Data.Enums;
using static MatForge.UnitTests.Analytics.Rules.RuleInputs;

namespace MatForge.UnitTests.Analytics;

public class InsightsCalculatorTests
{
    private const string Day = "2026-09-15";
    private const string PreviousDay = "2026-08-15";

    private static readonly (int Id, string Name) Choke = (1, "Choke");
    private static readonly (int Id, string Name) ArmLock = (2, "Arm Lock");
    private static readonly (int Id, string Name) ShoulderLock = (3, "Shoulder Lock");
    private static readonly (int Id, string Name) LegLock = (4, "Leg Lock");
    private static readonly (int Id, string Name) OtherCategory = (11, "Other");

    private static readonly TechniqueEntry Rnc = new(1, "Rear Naked Choke", Choke);
    private static readonly TechniqueEntry Triangle = new(2, "Triangle Choke", Choke);
    private static readonly TechniqueEntry Guillotine = new(3, "Guillotine", Choke);
    private static readonly TechniqueEntry Armbar = new(20, "Armbar", ArmLock);
    private static readonly TechniqueEntry Kimura = new(25, "Kimura", ShoulderLock);
    private static readonly TechniqueEntry HeelHook = new(30, "Inside Heel Hook", LegLock);
    private static readonly TechniqueEntry OtherFinish = new(61, "Other Finish", OtherCategory);

    private static readonly (int Id, string Name) ClosedGuard = (6, "Closed Guard");
    private static readonly (int Id, string Name) BackControl = (15, "Back Control");
    private static readonly (int Id, string Name) Saddle = (19, "Saddle / Cross Ashi");
    private static readonly (int Id, string Name) OtherPosition = (24, "Other");

    private static readonly OtherEntries Other = new(OtherFinish.Id, OtherCategory.Id, OtherPosition.Id);

    // The previous period of September is 2026-08-02 to 2026-08-31
    private static readonly DateRange September = new(Date("2026-09-01"), Date("2026-09-30"));

    [Fact]
    public void GT01_training_insights_match_the_reference_numbers()
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

        var insights = Calculate(rounds, []);

        ExpectSide(insights.Training, InsightStatus.Ok, 20, 10,
            Insight("DominantFinisherTechnique", InsightType.Strength, Item(Rnc, 4, 0.5m)),
            Insight("DominantFinisherCategory", InsightType.Strength, Item(Choke, 6, 0.75m)),
            Insight("StrongPosition", InsightType.Strength, Item(BackControl, 3, 0.6m)));
        ExpectSide(insights.Competition, InsightStatus.InsufficientData, 0, 5);
        insights.Comparison.Should().BeEmpty();
    }

    [Theory]
    [InlineData(9, InsightStatus.InsufficientData, 0)]
    [InlineData(10, InsightStatus.Ok, 1)]
    public void GT02_fewer_rounds_than_min_samples_give_insufficient_data(int rounds, InsightStatus status, int items)
    {
        // Every round is a Rear Naked Choke win, which would be a dominant technique with enough rounds
        var training = Calculate([..Times(rounds, Win(Rnc))], []).Training;

        training.Status.Should().Be(status);
        training.Samples.Should().Be(rounds);
        training.MinSamples.Should().Be(10);
        training.Items.Where(i => i.Rule == "DominantFinisherTechnique").Should().HaveCount(items);
    }

    [Fact]
    public void GT02_insufficient_data_has_no_items_at_all()
    {
        List<RoundRow> rounds = [..Times(5, Win(Rnc, BackControl)), ..Times(4, Loss(HeelHook, Saddle))];

        ExpectSide(Calculate(rounds, []).Training, InsightStatus.InsufficientData, 9, 10);
    }

    [Fact]
    public void GT03_win_rate_delta_gives_an_improving_trend()
    {
        List<RoundRow> rounds = [..Rounds(12, 8, Day), ..Rounds(7, 8, PreviousDay)];

        var items = Calculate(rounds, []).Training.Items;

        items.Should().Equal(Insight("ImprovingWinRate", InsightType.Trend, new WinRateChangeDto(0.6m, 0.4667m, 0.1333m)));
    }

    [Fact]
    public void GT03_previous_period_below_min_samples_gives_no_trend_rule()
    {
        List<RoundRow> rounds = [..Rounds(12, 8, Day), ..Rounds(3, 5, PreviousDay)];

        var training = Calculate(rounds, []).Training;

        training.Status.Should().Be(InsightStatus.Ok);
        training.Items.Should().BeEmpty();
    }

    [Fact]
    public void Trend_fires_when_the_returned_delta_reaches_the_threshold()
    {
        // 25/41 - 26/51 is 0.09995 before rounding
        List<RoundRow> rounds = [..Rounds(25, 16, Day), ..Rounds(26, 25, PreviousDay)];

        var items = Calculate(rounds, []).Training.Items;

        items.Should().Equal(Insight("ImprovingWinRate", InsightType.Trend, new WinRateChangeDto(0.6098m, 0.5098m, 0.1m)));
    }

    [Fact]
    public void Rounded_share_at_the_threshold_fires()
    {
        // 602 of 2007 finishes is 0.29995 before rounding, and the other finishes are spread over many techniques
        var spread = Enumerable.Range(0, 281)
            .Select(i => new TechniqueEntry(100 + i, $"Technique {i:000}", (100 + i, $"Category {i:000}")))
            .SelectMany(technique => Times(5, Win(technique)));
        List<RoundRow> rounds = [..Times(602, Win(Rnc)), ..spread];

        var items = Calculate(rounds, []).Training.Items;

        items.Should().Equal(
            Insight("DominantFinisherTechnique", InsightType.Strength, Item(Rnc, 602, 0.3m)),
            Insight("DominantFinisherCategory", InsightType.Strength, Item(Choke, 602, 0.3m)));
    }

    [Fact]
    public void Each_side_uses_its_own_profile()
    {
        // Three finishes are enough for competitions but not for training
        List<RoundRow> rounds = [..Times(2, Win(Rnc)), Win(Armbar), ..Times(2, Win()), ..Times(5, Loss())];
        List<CompetitionRow> competitions =
            [Competition(Day, FightWin(Rnc), FightWin(Rnc), FightWin(Armbar), FightLoss(), FightLoss(), FightLoss())];

        var insights = Calculate(rounds, competitions);

        ExpectSide(insights.Training, InsightStatus.Ok, 10, 10);
        ExpectSide(insights.Competition, InsightStatus.Ok, 6, 5,
            Insight("DominantFinisherTechnique", InsightType.Strength, Item(Rnc, 2, 0.6667m)),
            Insight("DominantFinisherCategory", InsightType.Strength, Item(Choke, 2, 0.6667m)));
        insights.Comparison.Should().BeEmpty();
    }

    [Theory]
    [InlineData(4, InsightStatus.InsufficientData)]
    [InlineData(5, InsightStatus.Ok)]
    public void Fewer_fights_than_min_samples_give_insufficient_data(int fights, InsightStatus status)
    {
        var competition = Calculate([], [Competition(Day, [..Enumerable.Repeat(FightWin(Rnc), fights)])]).Competition;

        competition.Status.Should().Be(status);
        competition.Samples.Should().Be(fights);
        competition.MinSamples.Should().Be(5);
        competition.Items.Should().HaveCount(status == InsightStatus.Ok ? 2 : 0);
    }

    [Fact]
    public void Other_technique_and_category_with_the_highest_share_are_skipped()
    {
        List<RoundRow> rounds = [..Times(4, Win(OtherFinish)), ..Times(3, Win(Rnc)), ..Times(3, Loss())];

        var items = Calculate(rounds, []).Training.Items;

        items.Should().Equal(
            Insight("DominantFinisherTechnique", InsightType.Strength, Item(Rnc, 3, 0.4286m)),
            Insight("DominantFinisherCategory", InsightType.Strength, Item(Choke, 3, 0.4286m)));
    }

    [Fact]
    public void Other_entries_still_count_in_the_block_total()
    {
        // Two other finishes and three chokes reach the five finishes of the training profile
        List<RoundRow> rounds = [..Times(2, Win(OtherFinish)), ..Times(3, Win(Rnc)), ..Times(5, Loss())];

        var items = Calculate(rounds, []).Training.Items;

        items.Should().Equal(
            Insight("DominantFinisherTechnique", InsightType.Strength, Item(Rnc, 3, 0.6m)),
            Insight("DominantFinisherCategory", InsightType.Strength, Item(Choke, 3, 0.6m)));
    }

    [Fact]
    public void Other_position_is_skipped_but_counts_as_recorded()
    {
        List<RoundRow> rounds =
            [..Times(4, Loss(Guillotine, OtherPosition)), ..Times(3, Loss(HeelHook, BackControl)), ..Times(3, Win())];

        var items = Calculate(rounds, []).Training.Items;

        items.Should().ContainSingle(i => i.Rule == "VulnerablePosition")
            .Which.Params.Should().Be(Item(BackControl, 3, 0.4286m));
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 1)]
    public void Comparison_needs_both_sides_with_enough_data(int fights, int comparisons)
    {
        List<RoundRow> rounds = Rounds(12, 8, Day).ToList();
        List<CompetitionRow> competitions = [Competition(Day, [FightWin(), ..Enumerable.Repeat(FightLoss(), fights - 1)])];

        var comparison = Calculate(rounds, competitions).Comparison;

        comparison.Should().HaveCount(comparisons);
        if (comparisons == 1)
        {
            comparison[0].Should().Be(new InsightDto("CompetitionVsTrainingGap", InsightType.Info,
                new WinRateGapDto(0.6m, 0.2m, -0.4m)));
        }
    }

    [Fact]
    public void No_data_gives_insufficient_data_on_both_sides()
    {
        var insights = Calculate([], []);

        ExpectSide(insights.Training, InsightStatus.InsufficientData, 0, 10);
        ExpectSide(insights.Competition, InsightStatus.InsufficientData, 0, 5);
        insights.Comparison.Should().BeEmpty();
    }

    private static InsightsDto Calculate(List<RoundRow> rounds, List<CompetitionRow> competitions) =>
        InsightsCalculator.Calculate(
            SparringCalculator.Calculate(September, rounds, SpecOptions.Training.MinSamples),
            CompetitionCalculator.Calculate(September, competitions, SpecOptions.Competition.MinSamples),
            Other, SpecOptions, AllRules);

    private static void ExpectSide(SideInsightsDto side, InsightStatus status, int samples, int minSamples,
        params InsightDto[] items)
    {
        (side.Status, side.Samples, side.MinSamples).Should().Be((status, samples, minSamples));
        side.Items.Should().Equal(items);
    }

    private static InsightDto Insight(string rule, InsightType type, object parameters) => new(rule, type, parameters);

    private static RoundRow Win(TechniqueEntry? technique = null, (int Id, string Name)? position = null, string date = Day) =>
        Round(RoundResult.Win, technique, position, date);

    private static RoundRow Loss(TechniqueEntry? technique = null, (int Id, string Name)? position = null, string date = Day) =>
        Round(RoundResult.Loss, technique, position, date);

    private static RoundRow Draw() => Round(RoundResult.Draw, null, null, Day);

    private static RoundRow Round(RoundResult result, TechniqueEntry? technique, (int Id, string Name)? position, string date) =>
        new(Date(date), result, technique?.Id, technique?.Name, technique?.Category.Id, technique?.Category.Name,
            position?.Id, position?.Name);

    private static IEnumerable<RoundRow> Rounds(int wins, int losses, string date) =>
        [..Times(wins, Win(date: date)), ..Times(losses, Loss(date: date))];

    private static IEnumerable<RoundRow> Times(int count, RoundRow round) => Enumerable.Repeat(round, count);

    private static CompetitionRow Competition(string date, params FightRow[] fights) =>
        new(1, Date(date), "Test Open", null, [..fights]);

    private static FightRow FightWin(TechniqueEntry? technique = null) =>
        new(FightResult.Win, technique == null ? FightMethod.Points : FightMethod.Submission,
            technique?.Id, technique?.Name, technique?.Category.Id, technique?.Category.Name);

    private static FightRow FightLoss() => new(FightResult.Loss, FightMethod.Points, null, null, null, null);

    private static FinishGroupDto Item(TechniqueEntry technique, int count, decimal share) =>
        new(technique.Id, technique.Name, count, share);

    private static FinishGroupDto Item((int Id, string Name) entry, int count, decimal share) =>
        new(entry.Id, entry.Name, count, share);

    private static DateOnly Date(string value) => DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private record TechniqueEntry(int Id, string Name, (int Id, string Name) Category);
}
