using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MatForge.Application.Features.Sessions;
using MatForge.Data.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Sessions;

[Collection("Database")]
public class SessionTests(DatabaseFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Create_returns_201_with_location_and_the_session()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions", Grappling(ids, rounds:
        [
            new { result = "Win", techniqueId = (int?)ids.Armbar, positionId = (int?)ids.Mount, notes = (string?)"Tight" },
            new { result = "Draw", techniqueId = (int?)null, positionId = (int?)null, notes = (string?)null }
        ]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await response.Content.ReadFromJsonAsync<SessionDto>(Json);
        response.Headers.Location!.ToString().Should().Be($"/api/sessions/{session!.Id}");
        session.Should().BeEquivalentTo(new
        {
            Kind = SessionKind.Grappling,
            Date = new DateOnly(2026, 10, 20),
            DurationMinutes = 90,
            Feeling = 4,
            Notes = "Half guard passing.",
            DisciplineId = (int?)ids.Bjj,
            FormatId = (int?)ids.Gi,
            CharacterId = (int?)ids.Sparring,
            ClubId = (int?)null,
            ActivityTypeId = (int?)null
        });
        session.Rounds.Should().Equal(
            new RoundDto(RoundResult.Win, ids.Armbar, ids.Mount, "Tight"),
            new RoundDto(RoundResult.Draw, null, null, null));
    }

    [Fact]
    public async Task Create_of_other_session_returns_201()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var session = await CreateAsync(user, Other(ids));

        session.Kind.Should().Be(SessionKind.Other);
        session.ActivityTypeId.Should().Be(ids.Running);
        session.DisciplineId.Should().BeNull();
        session.Rounds.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_returns_the_session_with_rounds_in_list_order()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Grappling(ids, rounds:
            [Round("Loss"), Round("Draw"), Round("Win", ids.Armbar)]));

        var session = await user.Client.GetFromJsonAsync<SessionDto>($"/api/sessions/{created.Id}", Json);

        session!.Rounds.Select(r => r.Result).Should().Equal(RoundResult.Loss, RoundResult.Draw, RoundResult.Win);
        (await StoredOrdersAsync(created.Id)).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Get_sorts_rounds_by_their_stored_order()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Grappling(ids, rounds: [Round("Win"), Round("Loss"), Round("Draw")]));

        await using (var db = fixture.CreateDbContext())
        {
            await db.SparringRounds.IgnoreQueryFilters()
                .Where(r => r.TrainingSessionId == created.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(r => r.Order, r => 4 - r.Order));
        }

        var session = await user.Client.GetFromJsonAsync<SessionDto>($"/api/sessions/{created.Id}", Json);

        session!.Rounds.Select(r => r.Result).Should().Equal(RoundResult.Draw, RoundResult.Loss, RoundResult.Win);
    }

    [Fact]
    public async Task Update_returns_200_with_the_changed_fields()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Grappling(ids));

        var response = await user.Client.PutAsJsonAsync($"/api/sessions/{created.Id}",
            Grappling(ids, date: "2026-10-21", formatId: ids.NoGi));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<SessionDto>(Json);
        session!.Id.Should().Be(created.Id);
        session.Date.Should().Be(new DateOnly(2026, 10, 21));
        session.FormatId.Should().Be(ids.NoGi);
    }

    [Fact]
    public async Task Update_replaces_rounds_and_renumbers()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Grappling(ids, rounds: [Round("Win"), Round("Win"), Round("Win")]));

        var response = await user.Client.PutAsJsonAsync($"/api/sessions/{created.Id}",
            Grappling(ids, rounds: [Round("Draw"), Round("Loss")]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await user.Client.GetFromJsonAsync<SessionDto>($"/api/sessions/{created.Id}", Json);
        session!.Rounds.Select(r => r.Result).Should().Equal(RoundResult.Draw, RoundResult.Loss);
        (await StoredOrdersAsync(created.Id)).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Update_from_grappling_to_other_clears_grappling_fields()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Grappling(ids, rounds: [Round("Win")]));

        var response = await user.Client.PutAsJsonAsync($"/api/sessions/{created.Id}", Other(ids));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await user.Client.GetFromJsonAsync<SessionDto>($"/api/sessions/{created.Id}", Json);
        session!.Kind.Should().Be(SessionKind.Other);
        session.ActivityTypeId.Should().Be(ids.Running);
        session.DisciplineId.Should().BeNull();
        session.FormatId.Should().BeNull();
        session.CharacterId.Should().BeNull();
        session.Rounds.Should().BeEmpty();
        (await StoredOrdersAsync(created.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_returns_204_and_removes_the_rounds()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Grappling(ids, rounds: [Round("Win"), Round("Loss")]));

        var response = await user.Client.DeleteAsync($"/api/sessions/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await user.Client.GetAsync($"/api/sessions/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await StoredOrdersAsync(created.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task List_returns_summaries_with_round_counts()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Grappling(ids, rounds:
            [Round("Win"), Round("Win"), Round("Win"), Round("Loss"), Round("Loss"), Round("Draw")]));

        var sessions = await ListAsync(user, "from=2026-10-01&to=2026-10-31");

        sessions.Should().Equal(new SessionSummaryDto(created.Id, SessionKind.Grappling, new DateOnly(2026, 10, 20), 90, 4,
            ids.Bjj, ids.Gi, ids.Sparring, null, null, 6, 3, 2, 1));
    }

    [Fact]
    public async Task List_is_sorted_by_date_then_id()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var later = await CreateAsync(user, Grappling(ids, date: "2026-10-22"));
        var firstOfDay = await CreateAsync(user, Grappling(ids, date: "2026-10-20"));
        var secondOfDay = await CreateAsync(user, Other(ids, date: "2026-10-20"));

        var sessions = await ListAsync(user, "from=2026-10-01&to=2026-10-31");

        sessions.Select(s => s.Id).Should().Equal(firstOfDay.Id, secondOfDay.Id, later.Id);
    }

    [Fact]
    public async Task List_filters_by_kind()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateAsync(user, Grappling(ids));
        var other = await CreateAsync(user, Other(ids));

        var sessions = await ListAsync(user, "from=2026-10-01&to=2026-10-31&kind=Other");

        sessions.Select(s => s.Id).Should().Equal(other.Id);
    }

    [Fact]
    public async Task List_includes_both_boundary_dates()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        await CreateAsync(user, Grappling(ids, date: "2026-10-09"));
        var first = await CreateAsync(user, Grappling(ids, date: "2026-10-10"));
        var last = await CreateAsync(user, Grappling(ids, date: "2026-10-20"));
        await CreateAsync(user, Grappling(ids, date: "2026-10-21"));

        var sessions = await ListAsync(user, "from=2026-10-10&to=2026-10-20");

        sessions.Select(s => s.Id).Should().Equal(first.Id, last.Id);
    }

    [Fact]
    public async Task List_without_a_range_returns_400_on_from_and_to()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var response = await user.Client.GetAsync("/api/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys
            .Should().BeEquivalentTo("from", "to");
    }

    [Theory]
    [InlineData("from=2026-01-01&to=2027-02-06")]
    [InlineData("from=2026-10-20&to=2026-10-19")]
    public async Task List_with_an_invalid_range_returns_400_on_to(string range)
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var response = await user.Client.GetAsync($"/api/sessions?{range}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.Should().Equal("to");
    }

    private static object Round(string result, int? techniqueId = null) =>
        new { result, techniqueId, positionId = (int?)null, notes = (string?)null };

    private static object Grappling(DictionaryIds ids, string date = "2026-10-20", int? formatId = null,
        object[]? rounds = null) =>
        new
        {
            kind = "Grappling",
            date,
            durationMinutes = 90,
            feeling = 4,
            notes = "Half guard passing.",
            disciplineId = ids.Bjj,
            formatId = formatId ?? ids.Gi,
            characterId = ids.Sparring,
            clubId = (int?)null,
            activityTypeId = (int?)null,
            rounds = rounds ?? []
        };

    private static object Other(DictionaryIds ids, string date = "2026-10-20") =>
        new { kind = "Other", date, durationMinutes = 45, feeling = 3, activityTypeId = ids.Running };

    private static async Task<SessionDto> CreateAsync(LoggedInUser user, object body)
    {
        var response = await user.Client.PostAsJsonAsync("/api/sessions", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<SessionDto>(Json))!;
    }

    private static async Task<List<SessionSummaryDto>> ListAsync(LoggedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<List<SessionSummaryDto>>($"/api/sessions?{query}", Json))!;

    private async Task<List<int>> StoredOrdersAsync(int sessionId)
    {
        await using var db = fixture.CreateDbContext();
        return await db.SparringRounds.IgnoreQueryFilters()
            .Where(r => r.TrainingSessionId == sessionId)
            .OrderBy(r => r.Order)
            .Select(r => r.Order)
            .ToListAsync();
    }

    private async Task<DictionaryIds> IdsAsync()
    {
        await using var db = fixture.CreateDbContext();
        var bjj = await db.Disciplines.SingleAsync(d => d.Code == "bjj");
        var gi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "gi");
        var noGi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "nogi");
        var sparring = await db.TrainingCharacters.SingleAsync(c => c.Code == "sparring");
        var running = await db.ActivityTypes.SingleAsync(a => a.Code == "running");
        var armbar = await db.Techniques.SingleAsync(t => t.Code == "armbar");
        var mount = await db.Positions.SingleAsync(p => p.Code == "mount");
        return new DictionaryIds(bjj.Id, gi.Id, noGi.Id, sparring.Id, running.Id, armbar.Id, mount.Id);
    }

    private record DictionaryIds(int Bjj, int Gi, int NoGi, int Sparring, int Running, int Armbar, int Mount);
}
