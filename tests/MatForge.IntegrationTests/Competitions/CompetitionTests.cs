using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using MatForge.Application.Features.Competitions;
using MatForge.Data.Entities;
using MatForge.Data.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Competitions;

[Collection("Database")]
public class CompetitionTests(DatabaseFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Create_returns_201_with_location_and_the_competition()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions", Full(ids, fights:
        [
            new { opponentName = (string?)"J. Kowalski", result = "Win", method = (string?)"Submission",
                techniqueId = (int?)ids.Armbar, durationSeconds = (int?)212, notes = (string?)"Tight" },
            Fight("Loss", "Points", durationSeconds: 360),
            Fight("Draw")
        ]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var competition = await response.Content.ReadFromJsonAsync<CompetitionDto>(Json);
        response.Headers.Location!.ToString().Should().Be($"/api/competitions/{competition!.Id}");
        competition.Should().BeEquivalentTo(new
        {
            Name = "Warsaw Open",
            Date = new DateOnly(2026, 9, 12),
            Location = "Warsaw",
            DisciplineId = ids.Bjj,
            FormatId = ids.NoGi,
            AgeCategory = "Adult",
            SkillLevel = "Blue",
            WeightCategory = "-82.3 kg",
            Place = 2,
            ResultNote = "Lost in the final",
            Notes = "Good day."
        });
        competition.Fights.Should().Equal(
            new FightDto("J. Kowalski", FightResult.Win, FightMethod.Submission, ids.Armbar, 212, "Tight"),
            new FightDto(null, FightResult.Loss, FightMethod.Points, null, 360, null),
            new FightDto(null, FightResult.Draw, null, null, null, null));
    }

    [Fact]
    public async Task Create_with_only_required_fields_returns_201()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var competition = await CreateAsync(user, Minimal(ids));

        competition.Should().BeEquivalentTo(new
        {
            Name = "Local Cup",
            Location = (string?)null,
            Place = (int?)null,
            ResultNote = (string?)null,
            Notes = (string?)null,
            Fights = new List<FightDto>()
        });
    }

    [Fact]
    public async Task Future_date_is_saved()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var nextYear = DateOnly.FromDateTime(DateTime.Now).AddYears(1);

        var competition = await CreateAsync(user, Minimal(ids, date: nextYear.ToString("yyyy-MM-dd")));

        competition.Date.Should().Be(nextYear);
    }

    [Fact]
    public async Task Get_returns_the_competition_with_fights_in_list_order()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Minimal(ids, fights: [Fight("Loss", "Points"), Fight("Draw"), Fight("Win", "Walkover")]));

        var competition = await GetAsync(user, created.Id);

        competition.Fights.Select(f => f.Result).Should().Equal(FightResult.Loss, FightResult.Draw, FightResult.Win);
        (await StoredOrdersAsync(created.Id)).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Get_sorts_fights_by_their_stored_order()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Minimal(ids, fights: [Fight("Win", "Points"), Fight("Loss", "Points"), Fight("Draw")]));

        await using (var db = fixture.CreateDbContext())
        {
            await db.Fights.IgnoreQueryFilters()
                .Where(f => f.CompetitionId == created.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(f => f.Order, f => 4 - f.Order));
        }

        var competition = await GetAsync(user, created.Id);

        competition.Fights.Select(f => f.Result).Should().Equal(FightResult.Draw, FightResult.Loss, FightResult.Win);
    }

    [Fact]
    public async Task Update_returns_200_with_the_changed_fields()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Full(ids));

        var response = await user.Client.PutAsJsonAsync($"/api/competitions/{created.Id}", Minimal(ids, date: "2026-09-13"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var competition = await response.Content.ReadFromJsonAsync<CompetitionDto>(Json);
        competition!.Id.Should().Be(created.Id);
        (await GetAsync(user, created.Id)).Should().BeEquivalentTo(new
        {
            Name = "Local Cup",
            Date = new DateOnly(2026, 9, 13),
            Location = (string?)null,
            FormatId = ids.Gi,
            AgeCategory = (string?)null,
            Place = (int?)null,
            Notes = (string?)null
        });
    }

    [Fact]
    public async Task Update_replaces_fights_and_renumbers()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Minimal(ids, fights:
            [Fight("Win", "Points", opponent: "A"), Fight("Loss", "Points", opponent: "B"), Fight("Win", "Points", opponent: "C")]));

        var response = await user.Client.PutAsJsonAsync($"/api/competitions/{created.Id}", Minimal(ids, fights:
            [Fight("Win", "Points", opponent: "C"), Fight("Win", "Points", opponent: "A")]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAsync(user, created.Id)).Fights.Select(f => f.OpponentName).Should().Equal("C", "A");
        (await StoredOrdersAsync(created.Id)).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Delete_returns_204_and_removes_the_fights()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Minimal(ids, fights: [Fight("Win", "Points"), Fight("Draw")]));

        var response = await user.Client.DeleteAsync($"/api/competitions/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await user.Client.GetAsync($"/api/competitions/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await StoredOrdersAsync(created.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_of_competition_used_by_a_goal_returns_409()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Minimal(ids, fights: [Fight("Win", "Points")]));
        await using (var db = fixture.CreateDbContext())
        {
            db.Goals.Add(new Goal
            {
                UserId = user.Id, Type = GoalType.CompetitionPrep, Title = "Prepare", CompetitionId = created.Id,
                StartDate = new DateOnly(2026, 8, 1), CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var response = await user.Client.DeleteAsync($"/api/competitions/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail.Should().Contain("Local Cup");
        (await GetAsync(user, created.Id)).Fights.Should().HaveCount(1);
    }

    [Fact]
    public async Task List_returns_summaries_with_fight_counts()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Full(ids, fights:
            [Fight("Win", "Points"), Fight("Win", "Points"), Fight("Win", "Points"), Fight("Loss", "Points"), Fight("Loss", "Points"), Fight("Draw")]));

        var competitions = await ListAsync(user, "");

        competitions.Should().Equal(new CompetitionSummaryDto(created.Id, "Warsaw Open", new DateOnly(2026, 9, 12), "Warsaw",
            ids.Bjj, ids.NoGi, "Adult", "Blue", "-82.3 kg", 2, "Lost in the final", 6, 3, 2, 1));
    }

    [Fact]
    public async Task List_is_sorted_by_date_descending_then_newest_first()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var earlier = await CreateAsync(user, Minimal(ids, date: "2026-03-01"));
        var firstOfDay = await CreateAsync(user, Minimal(ids, date: "2026-05-10"));
        var secondOfDay = await CreateAsync(user, Minimal(ids, date: "2026-05-10"));

        var competitions = await ListAsync(user, "");

        competitions.Select(c => c.Id).Should().Equal(secondOfDay.Id, firstOfDay.Id, earlier.Id);
    }

    [Theory]
    [InlineData("from=2026-05-10&to=2026-05-20", new[] { "2026-05-20", "2026-05-10" })]
    [InlineData("from=2026-05-10", new[] { "2026-05-21", "2026-05-20", "2026-05-10" })]
    [InlineData("to=2026-05-20", new[] { "2026-05-20", "2026-05-10", "2026-05-09" })]
    [InlineData("", new[] { "2026-05-21", "2026-05-20", "2026-05-10", "2026-05-09" })]
    public async Task List_filters_by_the_optional_dates_including_both_boundaries(string query, string[] expectedDates)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        foreach (var date in new[] { "2026-05-09", "2026-05-10", "2026-05-20", "2026-05-21" })
        {
            await CreateAsync(user, Minimal(ids, date: date));
        }

        var competitions = await ListAsync(user, query);

        competitions.Select(c => c.Date.ToString("yyyy-MM-dd")).Should().Equal(expectedDates);
    }

    [Fact]
    public async Task List_with_to_before_from_returns_400_on_to()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var response = await user.Client.GetAsync("/api/competitions?from=2026-05-10&to=2026-05-09");

        await ExpectValidationErrorAsync(response, "to");
    }

    [Theory]
    [InlineData("place", 0)]
    [InlineData("place", 1001)]
    public async Task Place_out_of_range_returns_400(string field, int value)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions", With(Minimal(ids), field, value));

        await ExpectValidationErrorAsync(response, field);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public async Task Place_at_the_limit_is_saved(int place)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        (await CreateAsync(user, With(Minimal(ids), "place", place))).Place.Should().Be(place);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    public async Task Duration_out_of_range_returns_400(int seconds)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            Minimal(ids, fights: [Fight("Win", "Points", durationSeconds: seconds)]));

        await ExpectValidationErrorAsync(response, "fights[0].durationSeconds");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3600)]
    public async Task Duration_at_the_limit_is_saved(int seconds)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var competition = await CreateAsync(user, Minimal(ids, fights: [Fight("Win", "Points", durationSeconds: seconds)]));

        competition.Fights[0].DurationSeconds.Should().Be(seconds);
    }

    [Theory]
    [InlineData("name", 150)]
    [InlineData("location", 150)]
    [InlineData("ageCategory", 50)]
    [InlineData("skillLevel", 50)]
    [InlineData("weightCategory", 50)]
    [InlineData("resultNote", 100)]
    [InlineData("notes", 4000)]
    public async Task Too_long_text_returns_400_and_the_limit_is_saved(string field, int maxLength)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            With(Minimal(ids), field, new string('x', maxLength + 1)));

        await ExpectValidationErrorAsync(response, field);
        await CreateAsync(user, With(Minimal(ids), field, new string('x', maxLength)));
    }

    [Theory]
    [InlineData("opponentName", 100)]
    [InlineData("notes", 500)]
    public async Task Too_long_fight_text_returns_400_and_the_limit_is_saved(string field, int maxLength)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            Minimal(ids, fights: [With(Fight("Draw"), field, new string('x', maxLength + 1))]));

        await ExpectValidationErrorAsync(response, $"fights[0].{field}");
        await CreateAsync(user, Minimal(ids, fights: [With(Fight("Draw"), field, new string('x', maxLength))]));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("date")]
    [InlineData("disciplineId")]
    [InlineData("formatId")]
    public async Task Missing_required_field_returns_400(string field)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var body = With(Minimal(ids), field, null);
        body.Remove(field);

        var response = await user.Client.PostAsJsonAsync("/api/competitions", body);

        await ExpectValidationErrorAsync(response, field);
    }

    [Fact]
    public async Task Blank_name_returns_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions", With(Minimal(ids), "name", "  "));

        await ExpectValidationErrorAsync(response, "name");
    }

    [Fact]
    public async Task Missing_fight_result_returns_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions", Minimal(ids, fights: [new JsonObject()]));

        await ExpectValidationErrorAsync(response, "fights[0].result");
    }

    [Theory]
    [InlineData("result", "\"Foo\"")]
    [InlineData("result", "0")]
    [InlineData("method", "\"Foo\"")]
    [InlineData("method", "3")]
    public async Task Invalid_fight_enum_returns_400(string field, string json)
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            Minimal(ids, fights: [With(Fight("Win", "Points"), field, JsonNode.Parse(json))]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys
            .Should().Contain($"$.fights[0].{field}");
    }

    [Fact]
    public async Task Null_fights_list_returns_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions", With(Minimal(ids), "fights", null));

        await ExpectValidationErrorAsync(response, "fights");
    }

    [Fact]
    public async Task Null_fight_returns_400_on_its_index()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions", Minimal(ids, fights: [Fight("Draw"), null!]));

        await ExpectValidationErrorAsync(response, "fights[1]");
    }

    [Fact]
    public async Task Draw_with_method_returns_400_on_its_fight()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            Minimal(ids, fights: [Fight("Win", "Points"), Fight("Draw", "Points")]));

        await ExpectValidationErrorAsync(response, "fights[1].method");
    }

    [Fact]
    public async Task Technique_with_points_returns_400_on_its_fight()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            Minimal(ids, fights: [Fight("Win", "Points", ids.Armbar)]));

        await ExpectValidationErrorAsync(response, "fights[0].techniqueId");
    }

    [Fact]
    public async Task Technique_not_offered_in_the_discipline_returns_400_on_its_fight()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            Minimal(ids, fights: [Fight("Win", "Submission", ids.Armbar), Fight("Win", "Pin", ids.PinFall)]));

        await ExpectValidationErrorAsync(response, "fights[1].techniqueId");
    }

    [Fact]
    public async Task Format_of_another_discipline_returns_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions", With(Minimal(ids), "formatId", ids.Freestyle));

        await ExpectValidationErrorAsync(response, "formatId");
    }

    [Fact]
    public async Task Update_runs_the_same_validation()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(user, Minimal(ids));

        var response = await user.Client.PutAsJsonAsync($"/api/competitions/{created.Id}",
            Minimal(ids, fights: [Fight("Win", "Pin", ids.PinFall)]));

        await ExpectValidationErrorAsync(response, "fights[0].techniqueId");
    }

    [Fact]
    public async Task Thirty_fights_are_saved_in_order()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var names = Enumerable.Range(1, 30).Select(i => $"Opponent {i}").ToList();
        var reversedNames = Enumerable.Reverse(names).ToList();

        var created = await CreateAsync(user, Minimal(ids, fights: [..names.Select(n => Fight("Win", "Points", opponent: n))]));

        created.Fights.Select(f => f.OpponentName).Should().Equal(names);
        (await GetAsync(user, created.Id)).Fights.Select(f => f.OpponentName).Should().Equal(names);

        var response = await user.Client.PutAsJsonAsync($"/api/competitions/{created.Id}",
            Minimal(ids, fights: [..reversedNames.Select(n => Fight("Loss", "Decision", opponent: n))]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAsync(user, created.Id)).Fights.Select(f => f.OpponentName).Should().Equal(reversedNames);
        (await StoredOrdersAsync(created.Id)).Should().Equal(Enumerable.Range(1, 30));
    }

    [Fact]
    public async Task Thirty_one_fights_return_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        var response = await user.Client.PostAsJsonAsync("/api/competitions",
            Minimal(ids, fights: [..Enumerable.Range(0, 31).Select(_ => Fight("Draw"))]));

        await ExpectValidationErrorAsync(response, "fights");
    }

    [Fact]
    public async Task Unknown_competition_returns_404()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();

        (await user.Client.GetAsync("/api/competitions/0")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await user.Client.PutAsJsonAsync("/api/competitions/0", Minimal(ids))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await user.Client.DeleteAsync("/api/competitions/0")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Requests_without_login_return_401()
    {
        var client = fixture.ApiFactory.CreateClient();
        var ids = await IdsAsync();

        (await client.GetAsync("/api/competitions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/competitions/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/competitions", Minimal(ids))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/api/competitions/1", Minimal(ids))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/api/competitions/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Other_user_cannot_list_read_update_or_delete_a_competition()
    {
        var userA = await fixture.CreateLoggedInUserAsync();
        var userB = await fixture.CreateLoggedInUserAsync();
        var ids = await IdsAsync();
        var created = await CreateAsync(userA, Full(ids, fights: [Fight("Win", "Submission", ids.Armbar)]));

        (await ListAsync(userB, "")).Should().BeEmpty();
        (await userB.Client.GetAsync($"/api/competitions/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await userB.Client.PutAsJsonAsync($"/api/competitions/{created.Id}", Minimal(ids))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await userB.Client.DeleteAsync($"/api/competitions/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await GetAsync(userA, created.Id)).Should().BeEquivalentTo(created);
        (await ListAsync(userA, "")).Select(c => c.Id).Should().Equal(created.Id);
    }

    private static object Fight(string result, string? method = null, int? techniqueId = null, int? durationSeconds = null,
        string? opponent = null) =>
        new { opponentName = opponent, result, method, techniqueId, durationSeconds, notes = (string?)null };

    private static object Full(DictionaryIds ids, object[]? fights = null) =>
        new
        {
            name = "Warsaw Open",
            date = "2026-09-12",
            location = "Warsaw",
            disciplineId = ids.Bjj,
            formatId = ids.NoGi,
            ageCategory = "Adult",
            skillLevel = "Blue",
            weightCategory = "-82.3 kg",
            place = 2,
            resultNote = "Lost in the final",
            notes = "Good day.",
            fights = fights ?? []
        };

    private static object Minimal(DictionaryIds ids, string date = "2026-05-10", object[]? fights = null) =>
        new { name = "Local Cup", date, disciplineId = ids.Bjj, formatId = ids.Gi, fights = fights ?? [] };

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

    private static async Task<CompetitionDto> CreateAsync(LoggedInUser user, object body)
    {
        var response = await user.Client.PostAsJsonAsync("/api/competitions", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CompetitionDto>(Json))!;
    }

    private static async Task<CompetitionDto> GetAsync(LoggedInUser user, int id) =>
        (await user.Client.GetFromJsonAsync<CompetitionDto>($"/api/competitions/{id}", Json))!;

    private static async Task<List<CompetitionSummaryDto>> ListAsync(LoggedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<List<CompetitionSummaryDto>>($"/api/competitions?{query}", Json))!;

    private async Task<List<int>> StoredOrdersAsync(int competitionId)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Fights.IgnoreQueryFilters()
            .Where(f => f.CompetitionId == competitionId)
            .OrderBy(f => f.Order)
            .Select(f => f.Order)
            .ToListAsync();
    }

    private async Task<DictionaryIds> IdsAsync()
    {
        await using var db = fixture.CreateDbContext();
        var bjj = await db.Disciplines.SingleAsync(d => d.Code == "bjj");
        var gi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "gi");
        var noGi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "nogi");
        var freestyle = await db.DisciplineFormats.SingleAsync(f => f.Discipline.Code == "wrestling" && f.Code == "freestyle");
        var armbar = await db.Techniques.SingleAsync(t => t.Code == "armbar");
        var pinFall = await db.Techniques.SingleAsync(t => t.Code == "pin_fall");
        return new DictionaryIds(bjj.Id, gi.Id, noGi.Id, freestyle.Id, armbar.Id, pinFall.Id);
    }

    private record DictionaryIds(int Bjj, int Gi, int NoGi, int Freestyle, int Armbar, int PinFall);
}
