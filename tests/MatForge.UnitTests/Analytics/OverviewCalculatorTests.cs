using System.Globalization;
using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Data.Enums;

namespace MatForge.UnitTests.Analytics;

public class OverviewCalculatorTests
{
    private static readonly (int Id, string Name) Bjj = (1, "Brazilian Jiu-Jitsu");
    private static readonly (int Id, string Name) Gi = (1, "Gi");
    private static readonly (int Id, string Name) NoGi = (2, "No-Gi");
    private static readonly (int Id, string Name) Technique = (1, "Technique");
    private static readonly (int Id, string Name) Sparring = (4, "Sparring");
    private static readonly (int Id, string Name) ClubA = (3, "Club A");
    private static readonly (int Id, string Name) ClubB = (5, "Club B");

    // 2026-09-01 is a Tuesday and 2026-09-28 a Monday, so both the first and last weeks are partial
    private static readonly DateRange September = Range("2026-09-01", "2026-09-28");

    [Fact]
    public void GT04_overview_matches_the_reference_numbers()
    {
        List<SessionRow> sessions =
        [
            Grappling("2026-09-01", Gi, ClubA, Sparring, feeling: 3),
            Grappling("2026-09-02", Gi, ClubA, Technique),
            Grappling("2026-09-04", Gi, ClubA, Technique),
            Grappling("2026-09-08", Gi, ClubB, Sparring),
            Other("2026-09-10"),
            Grappling("2026-09-15", Gi, ClubA, Technique),
            Grappling("2026-09-17", NoGi, ClubB, Technique),
            Grappling("2026-09-22", NoGi, null, Sparring),
            Other("2026-09-24"),
            Grappling("2026-09-28", Gi, ClubA, Technique)
        ];

        var overview = OverviewCalculator.Calculate(September, sessions);

        overview.Period.Should().Be(new PeriodDto(Date("2026-09-01"), Date("2026-09-28"), 28));
        overview.Sessions.Should().Be(new SessionTotalsDto(10, 840, 3.9m));
        overview.BySection.Should().Equal(
            new SectionDto(SessionKind.Grappling, 8, 720),
            new SectionDto(SessionKind.Other, 2, 120));
        overview.ByDiscipline.Should().Equal(new SessionGroupDto(Bjj.Id, Bjj.Name, 8, 720, 1m));
        overview.ByFormat.Should().Equal(
            new FormatGroupDto(Gi.Id, Gi.Name, Bjj.Id, 6, 540, 0.75m),
            new FormatGroupDto(NoGi.Id, NoGi.Name, Bjj.Id, 2, 180, 0.25m));
        overview.ByClub.Should().Equal(
            new ClubGroupDto(ClubA.Id, ClubA.Name, 5, 450, 0.625m),
            new ClubGroupDto(ClubB.Id, ClubB.Name, 2, 180, 0.25m),
            new ClubGroupDto(null, null, 1, 90, 0.125m));
        overview.ByCharacter.Should().Equal(
            new SessionGroupDto(Technique.Id, Technique.Name, 5, 450, 0.625m),
            new SessionGroupDto(Sparring.Id, Sparring.Name, 3, 270, 0.375m));
        overview.Weekly.Should().Equal(
            new WeekDto(Date("2026-08-31"), 3, 270),
            new WeekDto(Date("2026-09-07"), 2, 150),
            new WeekDto(Date("2026-09-14"), 2, 180),
            new WeekDto(Date("2026-09-21"), 2, 150),
            new WeekDto(Date("2026-09-28"), 1, 90));
        overview.AvgSessionsPerWeek.Should().Be(2.5m);
        overview.ActiveWeeksShare.Should().Be(1m);
    }

    [Fact]
    public void No_sessions_give_zeros_nulls_and_empty_weeks()
    {
        var overview = OverviewCalculator.Calculate(September, []);

        overview.Sessions.Should().Be(new SessionTotalsDto(0, 0, null));
        overview.BySection.Should().Equal(
            new SectionDto(SessionKind.Grappling, 0, 0),
            new SectionDto(SessionKind.Other, 0, 0));
        overview.ByDiscipline.Should().BeEmpty();
        overview.ByFormat.Should().BeEmpty();
        overview.ByClub.Should().BeEmpty();
        overview.ByCharacter.Should().BeEmpty();
        overview.Weekly.Should().HaveCount(5).And.OnlyContain(w => w.Count == 0 && w.Minutes == 0);
        overview.AvgSessionsPerWeek.Should().Be(0m);
        overview.ActiveWeeksShare.Should().Be(0m);
    }

    [Fact]
    public void Only_other_sessions_leave_the_grappling_lists_empty()
    {
        var overview = OverviewCalculator.Calculate(September, [Other("2026-09-03"), Other("2026-09-10", feeling: 2)]);

        overview.Sessions.Should().Be(new SessionTotalsDto(2, 120, 3m));
        overview.BySection.Should().Equal(
            new SectionDto(SessionKind.Other, 2, 120),
            new SectionDto(SessionKind.Grappling, 0, 0));
        overview.ByDiscipline.Should().BeEmpty();
        overview.ByFormat.Should().BeEmpty();
        overview.ByClub.Should().BeEmpty();
        overview.ByCharacter.Should().BeEmpty();
    }

    [Fact]
    public void Sessions_outside_the_period_are_ignored()
    {
        // The day before and the day after fall into the first and the last listed week
        List<SessionRow> sessions =
        [
            Grappling("2026-08-31", Gi, ClubA, Sparring),
            Grappling("2026-09-01", Gi, ClubA, Sparring),
            Other("2026-09-28"),
            Grappling("2026-09-29", Gi, ClubA, Sparring)
        ];

        var overview = OverviewCalculator.Calculate(September, sessions);

        overview.Sessions.Should().Be(new SessionTotalsDto(2, 150, 4m));
        overview.ByClub.Should().Equal(new ClubGroupDto(ClubA.Id, ClubA.Name, 1, 90, 1m));
        overview.Weekly[0].Should().Be(new WeekDto(Date("2026-08-31"), 1, 90));
        overview.Weekly[^1].Should().Be(new WeekDto(Date("2026-09-28"), 1, 60));
    }

    [Fact]
    public void Weekly_lists_every_overlapping_week_with_zeros()
    {
        // From Sunday to Monday
        var period = Range("2026-09-06", "2026-09-21");

        var overview = OverviewCalculator.Calculate(period, [Other("2026-09-06"), Other("2026-09-21")]);

        overview.Weekly.Should().Equal(
            new WeekDto(Date("2026-08-31"), 1, 60),
            new WeekDto(Date("2026-09-07"), 0, 0),
            new WeekDto(Date("2026-09-14"), 0, 0),
            new WeekDto(Date("2026-09-21"), 1, 60));
    }

    [Fact]
    public void Active_weeks_share_is_weeks_with_sessions_out_of_all_weeks()
    {
        var period = Range("2026-09-06", "2026-09-14");

        var overview = OverviewCalculator.Calculate(period, [Other("2026-09-06"), Other("2026-09-14")]);

        overview.Weekly.Should().HaveCount(3);
        overview.ActiveWeeksShare.Should().Be(0.6667m);
    }

    [Fact]
    public void Avg_feeling_is_over_all_sessions_and_rounds_half_away_from_zero()
    {
        List<SessionRow> sessions =
        [
            Grappling("2026-09-01", Gi, null, Sparring, feeling: 3),
            Grappling("2026-09-02", Gi, null, Sparring, feeling: 3),
            Grappling("2026-09-03", Gi, null, Sparring, feeling: 3),
            Other("2026-09-04", feeling: 4)
        ];

        OverviewCalculator.Calculate(September, sessions).Sessions.AvgFeeling.Should().Be(3.3m);
    }

    [Fact]
    public void Avg_sessions_per_week_rounds_half_away_from_zero()
    {
        // 1 session * 7 / 28 days = 0.25
        OverviewCalculator.Calculate(September, [Other("2026-09-10")]).AvgSessionsPerWeek.Should().Be(0.3m);
    }

    [Fact]
    public void One_day_period_has_one_week()
    {
        var overview = OverviewCalculator.Calculate(Range("2026-09-06", "2026-09-06"), [Other("2026-09-06")]);

        overview.Period.Days.Should().Be(1);
        overview.Weekly.Should().Equal(new WeekDto(Date("2026-08-31"), 1, 60));
        overview.AvgSessionsPerWeek.Should().Be(7m);
        overview.ActiveWeeksShare.Should().Be(1m);
    }

    [Fact]
    public void Lists_are_sorted_by_count_then_name()
    {
        // IDs don't follow name order, so sorting by ID would give a different result
        List<SessionRow> sessions =
        [
            Grappling("2026-09-01", Gi, (9, "club a"), (6, "Technique")),
            Grappling("2026-09-02", Gi, (8, "Club B"), (6, "Technique")),
            Grappling("2026-09-03", Gi, (7, "Łódź Gym"), (5, "Sparring")),
            Grappling("2026-09-04", Gi, (6, "Mat Club"), (4, "Open Mat"))
        ];

        var overview = OverviewCalculator.Calculate(September, sessions);

        overview.ByCharacter.Select(c => c.Name).Should().Equal("Technique", "Open Mat", "Sparring");
        overview.ByClub.Select(c => c.Name).Should().Equal("club a", "Club B", "Łódź Gym", "Mat Club");
    }

    [Fact]
    public void Other_club_comes_after_named_clubs_with_the_same_count()
    {
        List<SessionRow> sessions =
        [
            Grappling("2026-09-01", Gi, null, Sparring),
            Grappling("2026-09-02", Gi, null, Sparring),
            Grappling("2026-09-03", Gi, (2, "Alpha Gym"), Sparring),
            Grappling("2026-09-04", Gi, (3, "Zen Dojo"), Sparring),
            Grappling("2026-09-05", Gi, (3, "Zen Dojo"), Sparring)
        ];

        var overview = OverviewCalculator.Calculate(September, sessions);

        overview.ByClub.Select(c => (c.Id, c.Count)).Should().Equal((3, 2), (null, 2), (2, 1));
    }

    private static SessionRow Grappling(string date, (int Id, string Name) format, (int Id, string Name)? club,
        (int Id, string Name) character, int feeling = 4) =>
        new(Date(date), SessionKind.Grappling, 90, feeling, Bjj.Id, Bjj.Name, format.Id, format.Name,
            character.Id, character.Name, club?.Id, club?.Name);

    private static SessionRow Other(string date, int feeling = 4) =>
        new(Date(date), SessionKind.Other, 60, feeling, null, null, null, null, null, null, null, null);

    private static DateOnly Date(string value) => DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private static DateRange Range(string from, string to) => new(Date(from), Date(to));
}
