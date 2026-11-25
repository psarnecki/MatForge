using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using MatForge.Application.Features.Analytics;
using MatForge.Application.Features.Clubs;
using MatForge.Data.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
    }

    private static async Task ExpectValidationErrorAsync(HttpResponseMessage response, string key)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.Should().Equal(key);
    }

    private static async Task<OverviewDto> GetOverviewAsync(LoggedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<OverviewDto>($"/api/analytics/overview?{query}", Json))!;

    private static async Task<JsonNode> GetJsonAsync(LoggedInUser user, string url) =>
        JsonNode.Parse(await user.Client.GetStringAsync(url))!;

    private async Task<int> CountQueriesAsync(Guid userId, Func<AnalyticsService, Task> call)
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateDbContext(userId, counter);

        await call(new AnalyticsService(db));

        return counter.Count;
    }

    private static object Grappling(DictionaryIds ids, string date, int formatId, int? clubId, int minutes = 90,
        int feeling = 4) =>
        new
        {
            kind = "Grappling",
            date,
            durationMinutes = minutes,
            feeling,
            disciplineId = ids.Bjj,
            formatId,
            characterId = ids.Sparring,
            clubId,
            rounds = Array.Empty<object>()
        };

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

    private static async Task CreateCompetitionAsync(LoggedInUser user, DictionaryIds ids, DateOnly date)
    {
        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            new { name = "Test Open", date = Iso(date), disciplineId = ids.Bjj, formatId = ids.Gi });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private async Task<DictionaryIds> IdsAsync()
    {
        await using var db = fixture.CreateDbContext();
        var bjj = await db.Disciplines.SingleAsync(d => d.Code == "bjj");
        var gi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "gi");
        var noGi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "nogi");
        var sparring = await db.TrainingCharacters.SingleAsync(c => c.Code == "sparring");
        var running = await db.ActivityTypes.SingleAsync(a => a.Code == "running");
        return new DictionaryIds(bjj.Id, gi.Id, noGi.Id, sparring.Id, running.Id);
    }

    private record DictionaryIds(int Bjj, int Gi, int NoGi, int Sparring, int Running);

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
