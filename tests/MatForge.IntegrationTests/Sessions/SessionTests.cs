using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using MatForge.Application.Features.Clubs;
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

    [Theory]
    [InlineData("durationMinutes", 0)]
    [InlineData("durationMinutes", 601)]
    [InlineData("feeling", 0)]
    [InlineData("feeling", 6)]
    public async Task Number_out_of_range_returns_400_on_that_field(string field, int value)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions", With(Grappling(ids), field, value));

        await ExpectValidationErrorAsync(response, field);
    }

    [Theory]
    [InlineData("durationMinutes", 1)]
    [InlineData("durationMinutes", 600)]
    [InlineData("feeling", 1)]
    [InlineData("feeling", 5)]
    public async Task Number_at_the_limit_is_saved(string field, int value)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        await CreateAsync(user, With(Grappling(ids), field, value));
    }

    [Fact]
    public async Task Too_long_session_notes_return_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions",
            With(Grappling(ids), "notes", new string('x', 4001)));

        await ExpectValidationErrorAsync(response, "notes");
    }

    [Fact]
    public async Task Too_long_round_notes_return_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions",
            Grappling(ids, rounds: [Round("Win", notes: new string('x', 501))]));

        await ExpectValidationErrorAsync(response, "rounds[0].notes");
    }

    [Fact]
    public async Task Notes_at_the_limit_are_saved()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        await CreateAsync(user, With(Grappling(ids, rounds: [Round("Win", notes: new string('x', 500))]),
            "notes", new string('x', 4000)));
    }

    [Theory]
    [InlineData("kind")]
    [InlineData("date")]
    public async Task Missing_required_field_returns_400(string field)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var body = With(Grappling(ids), field, null);
        body.Remove(field);

        var response = await user.Client.PostAsJsonAsync("/api/sessions", body);

        await ExpectValidationErrorAsync(response, field);
    }

    [Fact]
    public async Task Missing_round_result_returns_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions", Grappling(ids, rounds: [new JsonObject()]));

        await ExpectValidationErrorAsync(response, "rounds[0].result");
    }

    [Theory]
    [InlineData("\"Foo\"")]
    [InlineData("1")]
    public async Task Invalid_enum_value_returns_400(string json)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions", With(Grappling(ids), "kind", JsonNode.Parse(json)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.Should().Contain("$.kind");
    }

    [Theory]
    [InlineData("\"Foo\"")]
    [InlineData("0")]
    [InlineData("7")]
    public async Task Invalid_round_result_returns_400(string json)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions",
            Grappling(ids, rounds: [new JsonObject { ["result"] = JsonNode.Parse(json) }]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys
            .Should().Contain("$.rounds[0].result");
    }

    [Fact]
    public async Task Null_rounds_list_returns_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions", With(Grappling(ids), "rounds", null));

        await ExpectValidationErrorAsync(response, "rounds");
    }

    [Fact]
    public async Task Null_round_returns_400_on_its_index()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions", Grappling(ids, rounds: [null!]));

        await ExpectValidationErrorAsync(response, "rounds[0]");
    }

    [Fact]
    public async Task Rounds_on_a_character_that_does_not_allow_them_return_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions",
            Grappling(ids, characterId: ids.Drilling, rounds: [Round("Win")]));

        await ExpectValidationErrorAsync(response, "rounds");
    }

    [Fact]
    public async Task Technique_not_offered_in_the_discipline_returns_400_on_its_round()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions",
            Grappling(ids, rounds: [Round("Win", ids.Armbar), Round("Win", ids.PinFall)]));

        await ExpectValidationErrorAsync(response, "rounds[1].techniqueId");
    }

    [Fact]
    public async Task Update_runs_the_same_validation()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Grappling(ids));

        var response = await user.Client.PutAsJsonAsync($"/api/sessions/{created.Id}",
            Grappling(ids, characterId: ids.Drilling, rounds: [Round("Win")]));

        await ExpectValidationErrorAsync(response, "rounds");
    }

    [Fact]
    public async Task Fifty_rounds_are_saved_in_order()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var notes = Enumerable.Range(1, 50).Select(i => $"Round {i}").ToList();
        var reversedNotes = Enumerable.Reverse(notes).ToList();

        var created = await CreateAsync(user, Grappling(ids, rounds: [..notes.Select(n => Round("Win", notes: n))]));

        created.Rounds.Select(r => r.Notes).Should().Equal(notes);
        (await GetAsync(user, created.Id)).Rounds.Select(r => r.Notes).Should().Equal(notes);

        var response = await user.Client.PutAsJsonAsync($"/api/sessions/{created.Id}",
            Grappling(ids, rounds: [..reversedNotes.Select(n => Round("Loss", notes: n))]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<SessionDto>(Json))!.Rounds.Select(r => r.Notes).Should().Equal(reversedNotes);
        (await GetAsync(user, created.Id)).Rounds.Select(r => r.Notes).Should().Equal(reversedNotes);
        (await StoredOrdersAsync(created.Id)).Should().Equal(Enumerable.Range(1, 50));
    }

    [Fact]
    public async Task Fifty_one_rounds_return_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/sessions",
            Grappling(ids, rounds: [..Enumerable.Range(0, 51).Select(_ => Round("Draw"))]));

        await ExpectValidationErrorAsync(response, "rounds");
    }

    [Fact]
    public async Task Session_with_own_club_is_saved()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var club = await CreateClubAsync(user, "Own Club");

        var session = await CreateAsync(user, Grappling(ids, clubId: club.Id));

        session.ClubId.Should().Be(club.Id);
    }

    [Fact]
    public async Task Session_with_another_users_club_returns_400()
    {
        var userA = await fixture.CreateLoggedInUserAsync();
        var userB = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var club = await CreateClubAsync(userA, "Private Club");

        var response = await userB.Client.PostAsJsonAsync("/api/sessions", Grappling(ids, clubId: club.Id));

        await ExpectValidationErrorAsync(response, "clubId");
    }

    [Fact]
    public async Task Unknown_session_returns_404()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        (await user.Client.GetAsync("/api/sessions/0")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await user.Client.PutAsJsonAsync("/api/sessions/0", Grappling(ids))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await user.Client.DeleteAsync("/api/sessions/0")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Requests_without_login_return_401()
    {
        var client = fixture.ApiFactory.CreateClient();
        var ids = await IdsAsync();

        (await client.GetAsync("/api/sessions?from=2026-10-01&to=2026-10-31")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/sessions/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/sessions", Grappling(ids))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/api/sessions/1", Grappling(ids))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/api/sessions/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Other_user_cannot_read_update_or_delete_a_session()
    {
        var userA = await fixture.CreateLoggedInUserAsync();
        var userB = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(userA, Grappling(ids, rounds: [Round("Win", ids.Armbar)]));

        (await userB.Client.GetAsync($"/api/sessions/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await userB.Client.PutAsJsonAsync($"/api/sessions/{created.Id}", Other(ids))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await userB.Client.DeleteAsync($"/api/sessions/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ListAsync(userB, "from=2026-10-01&to=2026-10-31")).Should().BeEmpty();

        (await GetAsync(userA, created.Id)).Should().BeEquivalentTo(created);
    }

    private static object Round(string result, int? techniqueId = null, string? notes = null) =>
        new { result, techniqueId, positionId = (int?)null, notes };

    private static JsonObject With(object body, string field, JsonNode? value)
    {
        var json = JsonSerializer.SerializeToNode(body)!.AsObject();
        json[field] = value;
        return json;
    }

    private static async Task ExpectValidationErrorAsync(HttpResponseMessage response, string key)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.Should().Equal(key);
    }

    private static async Task<SessionDto> GetAsync(LoggedInUser user, int id) =>
        (await user.Client.GetFromJsonAsync<SessionDto>($"/api/sessions/{id}", Json))!;

    private static async Task<ClubDto> CreateClubAsync(LoggedInUser user, string name)
    {
        var response = await user.Client.PostAsJsonAsync("/api/clubs", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClubDto>())!;
    }

    private static object Grappling(DictionaryIds ids, string date = "2026-10-20", int? formatId = null,
        object[]? rounds = null, int? characterId = null, int? clubId = null) =>
        new
        {
            kind = "Grappling",
            date,
            durationMinutes = 90,
            feeling = 4,
            notes = "Half guard passing.",
            disciplineId = ids.Bjj,
            formatId = formatId ?? ids.Gi,
            characterId = characterId ?? ids.Sparring,
            clubId,
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
        var drilling = await db.TrainingCharacters.SingleAsync(c => c.Code == "drilling");
        var pinFall = await db.Techniques.SingleAsync(t => t.Code == "pin_fall");
        return new DictionaryIds(bjj.Id, gi.Id, noGi.Id, sparring.Id, running.Id, armbar.Id, mount.Id, drilling.Id, pinFall.Id);
    }

    private record DictionaryIds(int Bjj, int Gi, int NoGi, int Sparring, int Running, int Armbar, int Mount, int Drilling, int PinFall);
}
