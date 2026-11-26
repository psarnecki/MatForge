using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Application.Features.Analytics.Rules;
using MatForge.Application.Features.Clubs;
using MatForge.Data.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MatForge.IntegrationTests.Analytics;

[Collection("Database")]
public class AnalyticsTests(DatabaseFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public async Task Overview_returns_the_numbers_of_the_period()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var club = await CreateClubAsync(user, "Home Gym");
        await CreateSessionAsync(user, Grappling(ids, "2026-09-01", ids.Gi, club.Id, feeling: 4));
        await CreateSessionAsync(user, Grappling(ids, "2026-09-08", ids.NoGi, null, minutes: 60, feeling: 3));
        await CreateSessionAsync(user, Other(ids, "2026-09-10", feeling: 5));

        var overview = await GetOverviewAsync(user, "from=2026-09-01&to=2026-09-14");

        overview.Period.Should().Be(new PeriodDto(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 14), 14));
        overview.Sessions.Should().Be(new SessionTotalsDto(3, 195, 4m));
        overview.BySection.Should().Equal(
            new SectionDto(SessionKind.Grappling, 2, 150),
            new SectionDto(SessionKind.Other, 1, 45));
        overview.ByDiscipline.Should().Equal(new SessionGroupDto(ids.Bjj, "Brazilian Jiu-Jitsu", 2, 150, 1m));
        overview.ByFormat.Should().Equal(
            new FormatGroupDto(ids.Gi, "Gi", ids.Bjj, 1, 90, 0.5m),
            new FormatGroupDto(ids.NoGi, "No-Gi", ids.Bjj, 1, 60, 0.5m));
        overview.ByClub.Should().Equal(
            new ClubGroupDto(club.Id, "Home Gym", 1, 90, 0.5m),
            new ClubGroupDto(null, null, 1, 60, 0.5m));
        overview.ByCharacter.Should().Equal(new SessionGroupDto(ids.Sparring, "Sparring", 2, 150, 1m));
        overview.Weekly.Should().Equal(
            new WeekDto(new DateOnly(2026, 8, 31), 1, 90),
            new WeekDto(new DateOnly(2026, 9, 7), 2, 105),
            new WeekDto(new DateOnly(2026, 9, 14), 0, 0));
        overview.AvgSessionsPerWeek.Should().Be(1.5m);
        overview.ActiveWeeksShare.Should().Be(0.6667m);
    }

    [Fact]
    public async Task Overview_json_uses_camel_case_strings_for_enums_and_null_for_other_club()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Grappling(ids, "2026-09-01", ids.Gi, null));

        var json = await GetJsonAsync(user, "/api/analytics/overview?from=2026-09-01&to=2026-09-07");

        json["period"]!["from"]!.GetValue<string>().Should().Be("2026-09-01");
        json["bySection"]![0]!["kind"]!.GetValue<string>().Should().Be("Grappling");
        var otherClub = json["byClub"]![0]!.AsObject();
        otherClub.Select(p => p.Key).Should().Contain(["id", "name"]);
        otherClub["id"].Should().BeNull();
        otherClub["name"].Should().BeNull();
        json["weekly"]![0]!["weekStart"]!.GetValue<string>().Should().Be("2026-08-31");
        json["avgSessionsPerWeek"]!.GetValue<decimal>().Should().Be(1m);
    }

    [Fact]
    public async Task Overview_counts_sessions_on_both_boundary_dates_only()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        foreach (var date in new[] { "2026-09-09", "2026-09-10", "2026-09-20", "2026-09-21" })
        {
            await CreateSessionAsync(user, Other(ids, date));
        }

        var overview = await GetOverviewAsync(user, "from=2026-09-10&to=2026-09-20");

        overview.Sessions.Count.Should().Be(2);
        overview.Weekly.Sum(w => w.Count).Should().Be(2);
    }

    [Fact]
    public async Task Overview_without_data_returns_zeros_and_nulls()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var overview = await GetOverviewAsync(user, "from=2026-09-01&to=2026-09-28");

        overview.Sessions.Should().Be(new SessionTotalsDto(0, 0, null));
        overview.BySection.Should().Equal(
            new SectionDto(SessionKind.Grappling, 0, 0),
            new SectionDto(SessionKind.Other, 0, 0));
        overview.ByDiscipline.Should().BeEmpty();
        overview.ByFormat.Should().BeEmpty();
        overview.ByClub.Should().BeEmpty();
        overview.ByCharacter.Should().BeEmpty();
        overview.Weekly.Should().HaveCount(5).And.OnlyContain(w => w.Count == 0);
        overview.AvgSessionsPerWeek.Should().Be(0m);
        overview.ActiveWeeksShare.Should().Be(0m);
    }

    [Fact]
    public async Task Overview_without_dates_runs_from_the_earliest_session_to_today()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Other(ids, Iso(Today.AddDays(-10))));
        await CreateSessionAsync(user, Other(ids, Iso(Today.AddDays(-40))));

        var overview = await GetOverviewAsync(user, "");

        overview.Period.Should().Be(new PeriodDto(Today.AddDays(-40), Today, 41));
        overview.Sessions.Count.Should().Be(2);
    }

    [Fact]
    public async Task Overview_without_from_starts_at_an_earlier_competition()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Other(ids, Iso(Today.AddDays(-40))));
        await CreateCompetitionAsync(user, ids, Today.AddDays(-100));

        var overview = await GetOverviewAsync(user, "");

        overview.Period.From.Should().Be(Today.AddDays(-100));
        overview.Period.To.Should().Be(Today);
    }

    [Fact]
    public async Task Overview_without_from_is_never_later_than_to()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Other(ids, Iso(Today.AddDays(-10))));

        var overview = await GetOverviewAsync(user, $"to={Iso(Today.AddDays(-20))}");

        overview.Period.Should().Be(new PeriodDto(Today.AddDays(-20), Today.AddDays(-20), 1));
        overview.Sessions.Count.Should().Be(0);
    }

    [Fact]
    public async Task Overview_without_any_data_uses_today_for_both_dates()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var overview = await GetOverviewAsync(user, "");

        overview.Period.Should().Be(new PeriodDto(Today, Today, 1));
        overview.Weekly.Should().HaveCount(1);
    }

    [Fact]
    public async Task Overview_ignores_another_users_data()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var otherUser = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Other(ids, Iso(Today.AddDays(-40))));
        await CreateSessionAsync(otherUser, Other(ids, Iso(Today.AddDays(-200))));
        await CreateSessionAsync(otherUser, Grappling(ids, Iso(Today.AddDays(-20)), ids.Gi, null));
        await CreateCompetitionAsync(otherUser, ids, Today.AddDays(-300));

        var overview = await GetOverviewAsync(user, "");

        overview.Period.From.Should().Be(Today.AddDays(-40));
        overview.Sessions.Count.Should().Be(1);
        overview.ByDiscipline.Should().BeEmpty();
    }

    [Fact]
    public async Task Overview_runs_one_query_and_one_more_without_from()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var club = await CreateClubAsync(user, "Query Club");
        await CreateSessionAsync(user, Grappling(ids, "2026-09-01", ids.Gi, club.Id));
        await CreateSessionAsync(user, Other(ids, "2026-09-02"));

        var withFrom = await CountQueriesAsync(user.Id, service => service.GetOverviewAsync(
            new AnalyticsQuery { From = new DateOnly(2026, 9, 1), To = new DateOnly(2026, 9, 30) }, CancellationToken.None));
        var withoutFrom = await CountQueriesAsync(user.Id, service => service.GetOverviewAsync(
            new AnalyticsQuery(), CancellationToken.None));

        withFrom.Should().Be(1);
        withoutFrom.Should().Be(2);
    }

    [Fact]
    public async Task Sparring_returns_totals_finishes_and_trend_with_dictionary_names()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-08",
            Round("Win", ids.Armbar, ids.Mount), Round("Win", ids.Armbar), Round("Loss", ids.Rnc, ids.BackControl), Round("Draw")));

        var sparring = await GetSparringAsync(user, "from=2026-09-01&to=2026-09-30");

        sparring.Totals.Should().Be(new RoundTotalsDto(4, 2, 1, 1, 0.5m, 1));
        sparring.FinishesFor.Total.Should().Be(2);
        sparring.FinishesFor.ByTechnique.Should().Equal(new FinishGroupDto(ids.Armbar, "Armbar", 2, 1m));
        sparring.FinishesFor.ByCategory.Should().Equal(new FinishGroupDto(ids.ArmLock, "Arm Lock", 2, 1m));
        sparring.FinishesFor.ByPosition.Recorded.Should().Be(1);
        sparring.FinishesFor.ByPosition.Items.Should().Equal(new FinishGroupDto(ids.Mount, "Mount", 1, 1m));
        sparring.FinishesAgainst.Total.Should().Be(1);
        sparring.FinishesAgainst.ByTechnique.Should().Equal(new FinishGroupDto(ids.Rnc, "Rear Naked Choke", 1, 1m));
        sparring.FinishesAgainst.ByCategory.Should().Equal(new FinishGroupDto(ids.Choke, "Choke", 1, 1m));
        sparring.FinishesAgainst.ByPosition.Items.Should().Equal(new FinishGroupDto(ids.BackControl, "Back Control", 1, 1m));
        sparring.Trend.Bucket.Should().Be(Bucket.Week);
        sparring.Trend.Points.Should().HaveCount(5);
        sparring.Trend.Points[1].Should().Be(new TrendPointDto(new DateOnly(2026, 9, 7), 4, 2, 1, 1, 0.5m));
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(0, null));
        sparring.Trend.WinRateDelta.Should().BeNull();
    }

    [Fact]
    public async Task Sparring_json_uses_strings_for_the_bucket_and_null_for_empty_rates()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-08", Round("Win", ids.Armbar, ids.Mount), Round("Loss")));

        var json = await GetJsonAsync(user, "/api/analytics/sparring?from=2026-09-01&to=2026-09-30");

        json["totals"]!["winRate"]!.GetValue<decimal>().Should().Be(0.5m);
        json["finishesFor"]!["byPosition"]!["recorded"]!.GetValue<int>().Should().Be(1);
        json["trend"]!["bucket"]!.GetValue<string>().Should().Be("Week");
        json["trend"]!["points"]![0]!["start"]!.GetValue<string>().Should().Be("2026-08-31");
        json["trend"]!["points"]![0]!.AsObject().Select(p => p.Key).Should().Contain("winRate");
        json["trend"]!["points"]![0]!["winRate"].Should().BeNull();
        json["trend"]!["winRateDelta"].Should().BeNull();
    }

    [Fact]
    public async Task Sparring_without_rounds_returns_zeros_and_nulls()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-08"));
        await CreateSessionAsync(user, Other(ids, "2026-09-09"));

        var sparring = await GetSparringAsync(user, "from=2026-09-01&to=2026-09-30");

        sparring.Totals.Should().Be(new RoundTotalsDto(0, 0, 0, 0, null, 0));
        sparring.FinishesFor.Should().BeEquivalentTo(new FinishesDto(0, [], [], new PositionFinishesDto(0, [])));
        sparring.FinishesAgainst.Should().BeEquivalentTo(new FinishesDto(0, [], [], new PositionFinishesDto(0, [])));
        sparring.Trend.Points.Should().HaveCount(5).And.OnlyContain(p => p.Rounds == 0 && p.WinRate == null);
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(0, null));
        sparring.Trend.WinRateDelta.Should().BeNull();
    }

    [Fact]
    public async Task Sparring_counts_rounds_by_the_date_of_their_session()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-08-31", Round("Loss")));
        await CreateSessionAsync(user, Sparring(ids, "2026-09-30", Round("Win")));
        await CreateSessionAsync(user, Sparring(ids, "2026-10-01", Round("Loss"), Round("Loss")));

        var sparring = await GetSparringAsync(user, "from=2026-09-01&to=2026-09-30");

        sparring.Totals.Should().Be(new RoundTotalsDto(1, 1, 0, 0, 1m, 1));
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(1, 0m));
    }

    [Fact]
    public async Task Sparring_compares_with_the_previous_period()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-15", Rounds(6, 4)));
        await CreateSessionAsync(user, Sparring(ids, "2026-08-15", Rounds(4, 6)));

        var sparring = await GetSparringAsync(user, "from=2026-09-01&to=2026-09-30");

        sparring.Totals.WinRate.Should().Be(0.6m);
        sparring.Trend.Previous.Should().Be(new PreviousPeriodDto(10, 0.4m));
        sparring.Trend.WinRateDelta.Should().Be(0.2m);
    }

    [Fact]
    public async Task Sparring_filters_both_periods_by_discipline_and_format()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10", Round("Win"), Round("Win")));
        await CreateSessionAsync(user, Grappling(ids, "2026-09-11", ids.NoGi, null, rounds: [Round("Loss")]));
        await CreateSessionAsync(user, Grappling(ids, "2026-09-12", ids.Freestyle, null, rounds: [Round("Draw")],
            disciplineId: ids.Wrestling));
        await CreateSessionAsync(user, Sparring(ids, "2026-08-20", Round("Win")));
        await CreateSessionAsync(user, Grappling(ids, "2026-08-21", ids.Freestyle, null, rounds: [Round("Loss")],
            disciplineId: ids.Wrestling));

        async Task<(int Current, int Previous)> RoundsAsync(string filters)
        {
            var sparring = await GetSparringAsync(user, $"from=2026-09-01&to=2026-09-30{filters}");
            return (sparring.Totals.Rounds, sparring.Trend.Previous.Rounds);
        }

        (await RoundsAsync("")).Should().Be((4, 2));
        (await RoundsAsync($"&disciplineId={ids.Bjj}")).Should().Be((3, 1));
        (await RoundsAsync($"&disciplineId={ids.Bjj}&formatId={ids.NoGi}")).Should().Be((1, 0));
        (await RoundsAsync($"&formatId={ids.Freestyle}")).Should().Be((1, 1));
    }

    [Fact]
    public async Task Sparring_ignores_another_users_rounds()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var otherUser = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10", Round("Win")));
        await CreateSessionAsync(otherUser, Sparring(ids, "2026-09-10", Round("Loss"), Round("Loss")));
        await CreateSessionAsync(otherUser, Sparring(ids, "2026-08-10", Round("Loss")));

        var sparring = await GetSparringAsync(user, "from=2026-09-01&to=2026-09-30");

        sparring.Totals.Should().Be(new RoundTotalsDto(1, 1, 0, 0, 1m, 1));
        sparring.Trend.Previous.Rounds.Should().Be(0);
    }

    [Fact]
    public async Task Sparring_runs_one_query_and_one_more_without_from()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10", Round("Win", ids.Armbar, ids.Mount), Round("Loss")));
        await CreateSessionAsync(user, Sparring(ids, "2026-08-10", Round("Draw")));

        var withFrom = await CountQueriesAsync(user.Id, service => service.GetSparringAsync(
            new AnalyticsFilterQuery { From = new DateOnly(2026, 9, 1), To = new DateOnly(2026, 9, 30), DisciplineId = ids.Bjj },
            CancellationToken.None));
        var withoutFrom = await CountQueriesAsync(user.Id, service => service.GetSparringAsync(
            new AnalyticsFilterQuery(), CancellationToken.None));

        withFrom.Should().Be(1);
        withoutFrom.Should().Be(2);
    }

    [Fact]
    public async Task Sparring_validates_the_period_like_the_overview()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        await ExpectValidationErrorAsync(
            await user.Client.GetAsync("/api/analytics/sparring?from=2026-09-10&to=2026-09-09"), "to");
    }

    [Fact]
    public async Task Competitions_returns_totals_methods_finishes_and_timeline_with_dictionary_names()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var first = await CreateCompetitionAsync(user, ids, Date("2026-09-05"), "Warsaw Open", 1,
            [Fight("Win", "Submission", ids.Armbar), Fight("Win", "Points"), Fight("Draw")]);
        var second = await CreateCompetitionAsync(user, ids, Date("2026-09-12"), "Krakow Cup", 4,
            [Fight("Loss", "Submission", ids.Rnc), Fight("Win", "Walkover")]);
        var third = await CreateCompetitionAsync(user, ids, Date("2026-09-20"), "Gdansk Open");

        var competitions = await GetCompetitionsAsync(user, "from=2026-09-01&to=2026-09-30");

        competitions.Totals.Should().Be(new FightTotalsDto(3, 5, 3, 1, 1, 0.6m, 1, 0, 0));
        competitions.Methods.Wins.Should().Equal(
            new MethodGroupDto(FightMethod.Points, 1, 0.3333m),
            new MethodGroupDto(FightMethod.Submission, 1, 0.3333m),
            new MethodGroupDto(FightMethod.Walkover, 1, 0.3333m));
        competitions.Methods.Losses.Should().Equal(new MethodGroupDto(FightMethod.Submission, 1, 1m));
        competitions.FinishesFor.Should().BeEquivalentTo(new FightFinishesDto(1,
            [new FinishGroupDto(ids.Armbar, "Armbar", 1, 1m)], [new FinishGroupDto(ids.ArmLock, "Arm Lock", 1, 1m)]));
        competitions.FinishesAgainst.Should().BeEquivalentTo(new FightFinishesDto(1,
            [new FinishGroupDto(ids.Rnc, "Rear Naked Choke", 1, 1m)], [new FinishGroupDto(ids.Choke, "Choke", 1, 1m)]));
        competitions.Timeline.Should().Equal(
            new TimelineItemDto(first, Date("2026-09-05"), "Warsaw Open", 1, 3, 2, 0),
            new TimelineItemDto(second, Date("2026-09-12"), "Krakow Cup", 4, 2, 1, 1),
            new TimelineItemDto(third, Date("2026-09-20"), "Gdansk Open", null, 0, 0, 0));
        competitions.Previous.Should().Be(new PreviousFightsDto(0, null));
        competitions.WinRateDelta.Should().BeNull();
    }

    [Fact]
    public async Task Competitions_json_uses_strings_for_methods_and_null_for_empty_rates()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateCompetitionAsync(user, ids, "2026-09-12", Fight("Win", "Submission", ids.Armbar));
        await CreateCompetitionAsync(user, ids, "2026-08-12");

        var json = await GetJsonAsync(user, "/api/analytics/competitions?from=2026-09-01&to=2026-09-30");

        json["totals"]!["winRate"]!.GetValue<decimal>().Should().Be(1m);
        json["methods"]!["wins"]![0]!["method"]!.GetValue<string>().Should().Be("Submission");
        json["finishesFor"]!.AsObject().Select(p => p.Key).Should().Equal("total", "byTechnique", "byCategory");
        json["timeline"]![0]!["date"]!.GetValue<string>().Should().Be("2026-09-12");
        json["timeline"]![0]!.AsObject().Select(p => p.Key).Should().Contain("place");
        json["timeline"]![0]!["place"].Should().BeNull();
        json["previous"]!["fights"]!.GetValue<int>().Should().Be(0);
        json["previous"]!["winRate"].Should().BeNull();
        json["winRateDelta"].Should().BeNull();
    }

    [Fact]
    public async Task Competitions_counts_competitions_on_both_boundary_dates_only()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        foreach (var date in new[] { "2026-09-09", "2026-09-10", "2026-09-20", "2026-09-21" })
        {
            await CreateCompetitionAsync(user, ids, date, Fight("Win", "Points"));
        }

        var competitions = await GetCompetitionsAsync(user, "from=2026-09-10&to=2026-09-20");

        competitions.Totals.Competitions.Should().Be(2);
        competitions.Timeline.Select(t => t.Date).Should().Equal(Date("2026-09-10"), Date("2026-09-20"));
        competitions.Previous.Fights.Should().Be(1);
    }

    [Fact]
    public async Task Competitions_without_data_returns_zeros_and_nulls()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var competitions = await GetCompetitionsAsync(user, "from=2026-09-01&to=2026-09-30");

        competitions.Totals.Should().Be(new FightTotalsDto(0, 0, 0, 0, 0, null, 0, 0, 0));
        competitions.Methods.Wins.Should().BeEmpty();
        competitions.Methods.Losses.Should().BeEmpty();
        competitions.FinishesFor.Should().BeEquivalentTo(new FightFinishesDto(0, [], []));
        competitions.FinishesAgainst.Should().BeEquivalentTo(new FightFinishesDto(0, [], []));
        competitions.Timeline.Should().BeEmpty();
        competitions.Previous.Should().Be(new PreviousFightsDto(0, null));
        competitions.WinRateDelta.Should().BeNull();
    }

    [Fact]
    public async Task Competitions_count_a_future_competition_inside_the_period()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var upcoming = await CreateCompetitionAsync(user, ids, Today.AddDays(10), "Upcoming Open");

        var competitions = await GetCompetitionsAsync(user, $"from={Iso(Today)}&to={Iso(Today.AddDays(30))}");

        competitions.Totals.Should().Be(new FightTotalsDto(1, 0, 0, 0, 0, null, 0, 0, 0));
        competitions.Timeline.Should().Equal(new TimelineItemDto(upcoming, Today.AddDays(10), "Upcoming Open", null, 0, 0, 0));
    }

    [Fact]
    public async Task Competitions_compare_with_the_previous_period()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateCompetitionAsync(user, ids, "2026-09-15", Fights(3, 2));
        await CreateCompetitionAsync(user, ids, "2026-08-15", Fights(1, 4));

        var competitions = await GetCompetitionsAsync(user, "from=2026-09-01&to=2026-09-30");

        competitions.Totals.WinRate.Should().Be(0.6m);
        competitions.Previous.Should().Be(new PreviousFightsDto(5, 0.2m));
        competitions.WinRateDelta.Should().Be(0.4m);
    }

    [Fact]
    public async Task Competitions_have_no_delta_when_the_previous_period_has_too_few_fights()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateCompetitionAsync(user, ids, "2026-09-15", Fights(3, 2));
        await CreateCompetitionAsync(user, ids, "2026-08-15", Fights(1, 3));

        var competitions = await GetCompetitionsAsync(user, "from=2026-09-01&to=2026-09-30");

        competitions.Previous.Should().Be(new PreviousFightsDto(4, 0.25m));
        competitions.WinRateDelta.Should().BeNull();
    }

    [Fact]
    public async Task Competitions_filter_both_periods_by_discipline_and_format()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateCompetitionAsync(user, ids, Date("2026-09-10"), fights: Fights(2, 0));
        await CreateCompetitionAsync(user, ids, Date("2026-09-11"), fights: Fights(0, 1), formatId: ids.NoGi);
        await CreateCompetitionAsync(user, ids, Date("2026-09-12"), fights: [Fight("Draw")],
            disciplineId: ids.Wrestling, formatId: ids.Freestyle);
        await CreateCompetitionAsync(user, ids, Date("2026-08-20"), fights: Fights(1, 0));
        await CreateCompetitionAsync(user, ids, Date("2026-08-21"), fights: Fights(0, 1),
            disciplineId: ids.Wrestling, formatId: ids.Freestyle);

        async Task<(int Current, int Previous)> FightsAsync(string filters)
        {
            var competitions = await GetCompetitionsAsync(user, $"from=2026-09-01&to=2026-09-30{filters}");
            return (competitions.Totals.Fights, competitions.Previous.Fights);
        }

        (await FightsAsync("")).Should().Be((4, 2));
        (await FightsAsync($"&disciplineId={ids.Bjj}")).Should().Be((3, 1));
        (await FightsAsync($"&disciplineId={ids.Bjj}&formatId={ids.NoGi}")).Should().Be((1, 0));
        (await FightsAsync($"&formatId={ids.Freestyle}")).Should().Be((1, 1));
    }

    [Fact]
    public async Task Competitions_ignore_another_users_data()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var otherUser = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateCompetitionAsync(user, ids, "2026-09-10", Fight("Win", "Points"));
        await CreateCompetitionAsync(otherUser, ids, Date("2026-09-10"), place: 1, fights: Fights(0, 2));
        await CreateCompetitionAsync(otherUser, ids, "2026-08-10", Fight("Loss", "Points"));

        var competitions = await GetCompetitionsAsync(user, "from=2026-09-01&to=2026-09-30");

        competitions.Totals.Should().Be(new FightTotalsDto(1, 1, 1, 0, 0, 1m, 0, 0, 0));
        competitions.Previous.Fights.Should().Be(0);
    }

    [Fact]
    public async Task Competitions_runs_one_query_and_one_more_without_from()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateCompetitionAsync(user, ids, "2026-09-10", Fight("Win", "Submission", ids.Armbar), Fight("Loss", "Points"));
        await CreateCompetitionAsync(user, ids, "2026-09-12");
        await CreateCompetitionAsync(user, ids, "2026-08-10", Fight("Draw"));

        var withFrom = await CountQueriesAsync(user.Id, service => service.GetCompetitionsAsync(
            new AnalyticsFilterQuery { From = new DateOnly(2026, 9, 1), To = new DateOnly(2026, 9, 30), DisciplineId = ids.Bjj },
            CancellationToken.None));
        var withoutFrom = await CountQueriesAsync(user.Id, service => service.GetCompetitionsAsync(
            new AnalyticsFilterQuery(), CancellationToken.None));

        withFrom.Should().Be(1);
        withoutFrom.Should().Be(2);
    }

    [Fact]
    public async Task Competitions_validates_the_period_like_the_overview()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        await ExpectValidationErrorAsync(
            await user.Client.GetAsync("/api/analytics/competitions?from=2026-09-10&to=2026-09-09"), "to");
    }

    [Fact]
    public async Task Insights_return_training_items_with_params_and_insufficient_competition_data()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10",
        [
            ..Enumerable.Repeat(Round("Win", ids.Armbar, ids.Mount), 3),
            ..Enumerable.Repeat(Round("Win", ids.Rnc, ids.BackControl), 2),
            ..Enumerable.Repeat(Round("Loss"), 5)
        ]));

        var json = await GetJsonAsync(user, "/api/analytics/insights?from=2026-09-01&to=2026-09-30");

        var training = json["training"]!;
        training["status"]!.GetValue<string>().Should().Be("Ok");
        training["samples"]!.GetValue<int>().Should().Be(10);
        training["minSamples"]!.GetValue<int>().Should().Be(10);
        RuleCodes(training).Should().Equal("DominantFinisherTechnique", "DominantFinisherCategory", "StrongPosition");
        var first = training["items"]![0]!;
        first["type"]!.GetValue<string>().Should().Be("Strength");
        first["params"]!.AsObject().Select(p => p.Key).Should().Equal("id", "name", "count", "share");
        first["params"]!["id"]!.GetValue<int>().Should().Be(ids.Armbar);
        first["params"]!["name"]!.GetValue<string>().Should().Be("Armbar");
        first["params"]!["count"]!.GetValue<int>().Should().Be(3);
        first["params"]!["share"]!.GetValue<decimal>().Should().Be(0.6m);
        training["items"]![2]!["params"]!["name"]!.GetValue<string>().Should().Be("Mount");

        var competition = json["competition"]!;
        competition["status"]!.GetValue<string>().Should().Be("InsufficientData");
        competition["samples"]!.GetValue<int>().Should().Be(0);
        competition["minSamples"]!.GetValue<int>().Should().Be(5);
        competition["items"]!.AsArray().Should().BeEmpty();
        json["comparison"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Insights_without_data_are_insufficient_on_both_sides()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var json = await GetJsonAsync(user, "/api/analytics/insights?from=2026-09-01&to=2026-09-30");

        json["training"]!["status"]!.GetValue<string>().Should().Be("InsufficientData");
        json["training"]!["items"]!.AsArray().Should().BeEmpty();
        json["competition"]!["status"]!.GetValue<string>().Should().Be("InsufficientData");
        json["competition"]!["items"]!.AsArray().Should().BeEmpty();
        json["comparison"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Insights_compare_training_and_competitions_when_both_have_enough_data()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10", Rounds(6, 4)));
        await CreateCompetitionAsync(user, ids, "2026-09-12", Fights(2, 3));

        var json = await GetJsonAsync(user, "/api/analytics/insights?from=2026-09-01&to=2026-09-30");

        var gap = json["comparison"]!.AsArray().Should().ContainSingle().Subject!;
        gap["rule"]!.GetValue<string>().Should().Be("CompetitionVsTrainingGap");
        gap["type"]!.GetValue<string>().Should().Be("Info");
        gap["params"]!.AsObject().Select(p => p.Key).Should().Equal("trainingWinRate", "competitionWinRate", "delta");
        gap["params"]!["trainingWinRate"]!.GetValue<decimal>().Should().Be(0.6m);
        gap["params"]!["competitionWinRate"]!.GetValue<decimal>().Should().Be(0.4m);
        gap["params"]!["delta"]!.GetValue<decimal>().Should().Be(-0.2m);
    }

    [Fact]
    public async Task Insights_apply_the_filters_to_both_sides()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10", Rounds(10, 0)));
        await CreateCompetitionAsync(user, ids, Date("2026-09-12"), fights: Fights(5, 0),
            disciplineId: ids.Wrestling, formatId: ids.Freestyle);

        async Task<(string Training, int Rounds, string Competition, int Fights)> SidesAsync(string filters)
        {
            var json = await GetJsonAsync(user, $"/api/analytics/insights?from=2026-09-01&to=2026-09-30{filters}");
            return (json["training"]!["status"]!.GetValue<string>(), json["training"]!["samples"]!.GetValue<int>(),
                json["competition"]!["status"]!.GetValue<string>(), json["competition"]!["samples"]!.GetValue<int>());
        }

        (await SidesAsync("")).Should().Be(("Ok", 10, "Ok", 5));
        (await SidesAsync($"&disciplineId={ids.Bjj}")).Should().Be(("Ok", 10, "InsufficientData", 0));
        (await SidesAsync($"&disciplineId={ids.Wrestling}&formatId={ids.Freestyle}"))
            .Should().Be(("InsufficientData", 0, "Ok", 5));
    }

    [Fact]
    public async Task Insights_skip_the_seeded_other_entries()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10",
        [
            ..Enumerable.Repeat(Round("Win", ids.OtherFinish, ids.OtherPosition), 4),
            ..Enumerable.Repeat(Round("Win", ids.Armbar, ids.Mount), 3),
            ..Enumerable.Repeat(Round("Loss"), 3)
        ]));

        var json = await GetJsonAsync(user, "/api/analytics/insights?from=2026-09-01&to=2026-09-30");

        var items = json["training"]!["items"]!.AsArray();
        RuleCodes(json["training"]!).Should().Equal("DominantFinisherTechnique", "DominantFinisherCategory", "StrongPosition");
        items.Select(i => i!["params"]!["name"]!.GetValue<string>()).Should().Equal("Armbar", "Arm Lock", "Mount");
        items.Select(i => i!["params"]!["share"]!.GetValue<decimal>()).Should().Equal(0.4286m, 0.4286m, 0.4286m);
    }

    [Fact]
    public async Task Insights_use_the_thresholds_from_configuration()
    {
        using var factory = new ApiFactory(fixture.ConnectionString).WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Analytics:Rules:Training:MinSamples"] = "1",
                ["Analytics:Rules:Training:MinFinishes"] = "1",
                ["Analytics:Rules:Training:MinCount"] = "1"
            })));
        var client = factory.CreateClient();
        var credentials = new { email = $"user-{Guid.NewGuid():N}@test.local", password = "Test1234!" };
        (await client.PostAsJsonAsync("/api/auth/register", credentials)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true", credentials)).EnsureSuccessStatusCode();
        var ids = await IdsAsync();
        (await client.PostAsJsonAsync("/api/sessions", Sparring(ids, "2026-09-10", Round("Win", ids.Armbar))))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var json = JsonNode.Parse(await client.GetStringAsync("/api/analytics/insights?from=2026-09-01&to=2026-09-30"))!;

        json["training"]!["status"]!.GetValue<string>().Should().Be("Ok");
        json["training"]!["minSamples"]!.GetValue<int>().Should().Be(1);
        RuleCodes(json["training"]!).Should().Equal("DominantFinisherTechnique", "DominantFinisherCategory");
    }

    [Fact]
    public async Task Insights_ignore_another_users_data()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var otherUser = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10", Rounds(9, 0)));
        await CreateSessionAsync(otherUser, Sparring(ids, "2026-09-10", Rounds(5, 5)));
        await CreateCompetitionAsync(otherUser, ids, "2026-09-12", Fights(5, 0));

        var json = await GetJsonAsync(user, "/api/analytics/insights?from=2026-09-01&to=2026-09-30");

        json["training"]!["samples"]!.GetValue<int>().Should().Be(9);
        json["training"]!["status"]!.GetValue<string>().Should().Be("InsufficientData");
        json["competition"]!["samples"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task Insights_run_three_queries_and_one_more_without_from()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateSessionAsync(user, Sparring(ids, "2026-09-10", Rounds(6, 4)));
        await CreateCompetitionAsync(user, ids, "2026-09-12", Fights(2, 3));

        var withFrom = await CountQueriesAsync(user.Id, service => service.GetInsightsAsync(
            new AnalyticsFilterQuery { From = new DateOnly(2026, 9, 1), To = new DateOnly(2026, 9, 30), DisciplineId = ids.Bjj },
            CancellationToken.None));
        var withoutFrom = await CountQueriesAsync(user.Id, service => service.GetInsightsAsync(
            new AnalyticsFilterQuery(), CancellationToken.None));

        withFrom.Should().Be(3);
        withoutFrom.Should().Be(4);
    }

    [Fact]
    public async Task Insights_validate_the_period_like_the_overview()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        await ExpectValidationErrorAsync(
            await user.Client.GetAsync("/api/analytics/insights?from=2026-09-10&to=2026-09-09"), "to");
    }

    [Theory]
    [InlineData("sparring")]
    [InlineData("competitions")]
    [InlineData("insights")]
    public async Task Unknown_discipline_and_format_of_another_discipline_return_400_on_the_field(string endpoint)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        await ExpectValidationErrorAsync(
            await user.Client.GetAsync($"/api/analytics/{endpoint}?disciplineId=0"), "disciplineId");
        await ExpectValidationErrorAsync(
            await user.Client.GetAsync($"/api/analytics/{endpoint}?disciplineId={ids.Bjj}&formatId={ids.Freestyle}"), "formatId");
        await ExpectValidationErrorAsync(
            await user.Client.GetAsync($"/api/analytics/{endpoint}?formatId=0"), "formatId");
    }

    [Fact]
    public void Every_insight_rule_is_registered_in_catalog_order()
    {
        Type[] catalog =
        [
            typeof(DominantFinisherTechniqueRule), typeof(DominantFinisherCategoryRule),
            typeof(RecurringLossTechniqueRule), typeof(RecurringLossCategoryRule),
            typeof(StrongPositionRule), typeof(VulnerablePositionRule),
            typeof(ImprovingWinRateRule), typeof(DecliningWinRateRule),
            typeof(CompetitionVsTrainingGapRule)
        ];
        var implemented = typeof(IInsightRule).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IInsightRule).IsAssignableFrom(t));

        var registered = fixture.ApiFactory.Services.GetServices<IInsightRule>().Select(r => r.GetType());

        registered.Should().Equal(catalog);
        implemented.Should().BeEquivalentTo(catalog);
    }

    [Fact]
    public void Api_does_not_start_when_min_samples_is_below_1()
    {
        using var factory = new ApiFactory(fixture.ConnectionString).WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Analytics:Rules:Training:MinSamples"] = "0" })));

        var start = () => factory.Server;

        start.Should().Throw<OptionsValidationException>().WithMessage("*MinSamples must be at least 1*");
    }

    [Fact]
    public void Api_binds_the_thresholds_from_appsettings()
    {
        var options = fixture.ApiFactory.Services.GetRequiredService<IOptions<AnalyticsRulesOptions>>().Value;

        options.Training.Should().BeEquivalentTo(new TrainingThresholds
        {
            MinSamples = 10, MinFinishes = 5, MinCount = 3, TechniqueShare = 0.30m, PositionShare = 0.40m, TrendDelta = 0.10m
        });
        options.Competition.Should().BeEquivalentTo(new RuleThresholds
        {
            MinSamples = 5, MinFinishes = 3, MinCount = 2, TechniqueShare = 0.30m, TrendDelta = 0.10m
        });
        options.GapDelta.Should().Be(0.20m);
    }

    [Fact]
    public void Api_does_not_start_when_a_competition_share_is_0()
    {
        using var factory = new ApiFactory(fixture.ConnectionString).WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Analytics:Rules:Competition:TechniqueShare"] = "0" })));

        var start = () => factory.Server;

        start.Should().Throw<OptionsValidationException>()
            .WithMessage("*Analytics:Rules:Competition:TechniqueShare must be above 0 and at most 1*");
    }

    [Theory]
    [InlineData("from=1999-12-31", "from")]
    [InlineData("to=1999-12-31", "to")]
    [InlineData("from=2026-09-10&to=2026-09-09", "to")]
    public async Task Invalid_period_returns_400_on_the_field(string query, string field)
    {
        var user = await fixture.CreateLoggedInUserAsync();

        await ExpectValidationErrorAsync(await user.Client.GetAsync($"/api/analytics/overview?{query}"), field);
    }

    [Fact]
    public async Task Future_from_without_to_and_to_after_next_year_return_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var afterNextYear = new DateOnly(Today.Year + 2, 1, 1);

        await ExpectValidationErrorAsync(
            await user.Client.GetAsync($"/api/analytics/overview?from={Iso(Today.AddDays(1))}"), "from");
        await ExpectValidationErrorAsync(
            await user.Client.GetAsync($"/api/analytics/overview?to={Iso(afterNextYear)}"), "to");
    }

    [Fact]
    public async Task Requests_without_login_return_401()
    {
        var client = fixture.ApiFactory.CreateClient();

        (await client.GetAsync("/api/analytics/overview")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/analytics/sparring")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/analytics/competitions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/analytics/insights")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task ExpectValidationErrorAsync(HttpResponseMessage response, string key)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.Should().Equal(key);
    }

    private static async Task<OverviewDto> GetOverviewAsync(LoggedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<OverviewDto>($"/api/analytics/overview?{query}", Json))!;

    private static async Task<SparringDto> GetSparringAsync(LoggedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<SparringDto>($"/api/analytics/sparring?{query}", Json))!;

    private static async Task<CompetitionAnalyticsDto> GetCompetitionsAsync(LoggedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<CompetitionAnalyticsDto>($"/api/analytics/competitions?{query}", Json))!;

    private static IEnumerable<string> RuleCodes(JsonNode side) =>
        side["items"]!.AsArray().Select(i => i!["rule"]!.GetValue<string>());

    private static async Task<JsonNode> GetJsonAsync(LoggedInUser user, string url) =>
        JsonNode.Parse(await user.Client.GetStringAsync(url))!;

    private async Task<int> CountQueriesAsync(Guid userId, Func<AnalyticsService, Task> call)
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateDbContext(userId, counter);

        var services = fixture.ApiFactory.Services;
        await call(new AnalyticsService(db, services.GetRequiredService<IOptions<AnalyticsRulesOptions>>(),
            services.GetServices<IInsightRule>()));

        return counter.Count;
    }

    private static object Grappling(DictionaryIds ids, string date, int formatId, int? clubId, int minutes = 90,
        int feeling = 4, object[]? rounds = null, int? disciplineId = null) =>
        new
        {
            kind = "Grappling",
            date,
            durationMinutes = minutes,
            feeling,
            disciplineId = disciplineId ?? ids.Bjj,
            formatId,
            characterId = ids.Sparring,
            clubId,
            rounds = rounds ?? []
        };

    private static object Sparring(DictionaryIds ids, string date, params object[] rounds) =>
        Grappling(ids, date, ids.Gi, null, rounds: rounds);

    private static object Round(string result, int? techniqueId = null, int? positionId = null) =>
        new { result, techniqueId, positionId };

    private static object[] Rounds(int wins, int losses) =>
        [..Enumerable.Repeat(Round("Win"), wins), ..Enumerable.Repeat(Round("Loss"), losses)];

    private static object Other(DictionaryIds ids, string date, int feeling = 4) =>
        new { kind = "Other", date, durationMinutes = 45, feeling, activityTypeId = ids.Running };

    private static async Task CreateSessionAsync(LoggedInUser user, object body)
    {
        var response = await user.Client.PostAsJsonAsync("/api/sessions", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<ClubDto> CreateClubAsync(LoggedInUser user, string name)
    {
        var response = await user.Client.PostAsJsonAsync("/api/clubs", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClubDto>())!;
    }

    private static async Task<int> CreateCompetitionAsync(LoggedInUser user, DictionaryIds ids, DateOnly date,
        string name = "Test Open", int? place = null, object[]? fights = null, int? disciplineId = null, int? formatId = null)
    {
        var response = await user.Client.PostAsJsonAsync("/api/competitions", new
        {
            name,
            date = Iso(date),
            disciplineId = disciplineId ?? ids.Bjj,
            formatId = formatId ?? ids.Gi,
            place,
            fights = fights ?? []
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<int>();
    }

    private static Task<int> CreateCompetitionAsync(LoggedInUser user, DictionaryIds ids, string date,
        params object[] fights) =>
        CreateCompetitionAsync(user, ids, Date(date), fights: fights);

    private static object Fight(string result, string? method = null, int? techniqueId = null) =>
        new { result, method, techniqueId };

    private static object[] Fights(int wins, int losses) =>
        [..Enumerable.Repeat(Fight("Win", "Points"), wins), ..Enumerable.Repeat(Fight("Loss", "Points"), losses)];

    private static DateOnly Date(string value) => DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private async Task<DictionaryIds> IdsAsync()
    {
        await using var db = fixture.CreateDbContext();
        var bjj = await db.Disciplines.SingleAsync(d => d.Code == "bjj");
        var gi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "gi");
        var noGi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "nogi");
        var sparring = await db.TrainingCharacters.SingleAsync(c => c.Code == "sparring");
        var running = await db.ActivityTypes.SingleAsync(a => a.Code == "running");
        var wrestling = await db.Disciplines.SingleAsync(d => d.Code == "wrestling");
        var freestyle = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == wrestling.Id && f.Code == "freestyle");
        var armbar = await db.Techniques.SingleAsync(t => t.Code == "armbar");
        var rnc = await db.Techniques.SingleAsync(t => t.Code == "rear_naked_choke");
        var mount = await db.Positions.SingleAsync(p => p.Code == "mount");
        var backControl = await db.Positions.SingleAsync(p => p.Code == "back_control");
        var otherFinish = await db.Techniques.SingleAsync(t => t.Code == "other_finish");
        var otherPosition = await db.Positions.SingleAsync(p => p.Code == "other_position");
        return new DictionaryIds(bjj.Id, gi.Id, noGi.Id, sparring.Id, running.Id, wrestling.Id, freestyle.Id,
            armbar.Id, armbar.CategoryId, rnc.Id, rnc.CategoryId, mount.Id, backControl.Id, otherFinish.Id, otherPosition.Id);
    }

    private record DictionaryIds(int Bjj, int Gi, int NoGi, int Sparring, int Running, int Wrestling, int Freestyle,
        int Armbar, int ArmLock, int Rnc, int Choke, int Mount, int BackControl, int OtherFinish, int OtherPosition);

    private class QueryCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
