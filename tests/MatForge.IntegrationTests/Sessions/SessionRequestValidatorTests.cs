using System.Data.Common;
using FluentAssertions;
using MatForge.Application.Features.Sessions;
using MatForge.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MatForge.IntegrationTests.Sessions;

// Runs the validator directly against the container database (no HTTP)
[Collection("Database")]
public class SessionRequestValidatorTests(DatabaseFixture fixture)
{
    private const int UnknownId = 0;

    [Fact]
    public async Task Valid_grappling_session_with_rounds_passes()
    {
        var round = Win(await TechniqueIdAsync("armbar"), await PositionIdAsync("mount"));

        var errors = await ValidateAsync(await SessionAsync(rounds: [round, new RoundRequest { Result = RoundResult.Draw }]));

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Valid_other_session_passes()
    {
        var errors = await ValidateAsync(Other(await ActivityTypeIdAsync("running")));

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Field_rules_run_as_part_of_this_validator()
    {
        var draw = new RoundRequest { Result = RoundResult.Draw, TechniqueId = await TechniqueIdAsync("armbar") };

        var errors = await ValidateAsync(await SessionAsync(rounds: [draw]));

        errors.Should().Equal("Rounds[0].TechniqueId");
    }

    [Fact]
    public async Task Format_of_another_discipline_fails_on_format_id()
    {
        var request = Grappling(await DisciplineIdAsync("bjj"), await FormatIdAsync("wrestling", "freestyle"),
            await CharacterIdAsync("sparring"));

        var errors = await ValidateAsync(request);

        errors.Should().Equal("FormatId");
    }

    [Fact]
    public async Task Unknown_format_fails_on_format_id()
    {
        var errors = await ValidateAsync(Grappling(await DisciplineIdAsync("bjj"), UnknownId, await CharacterIdAsync("sparring")));

        errors.Should().Equal("FormatId");
    }

    [Fact]
    public async Task Unknown_discipline_fails_on_format_id()
    {
        var errors = await ValidateAsync(Grappling(UnknownId, await FormatIdAsync("bjj", "gi"), await CharacterIdAsync("sparring")));

        errors.Should().Equal("FormatId");
    }

    [Fact]
    public async Task Missing_discipline_skips_the_offered_checks()
    {
        var round = Win(await TechniqueIdAsync("armbar"), await PositionIdAsync("mount"));
        var request = Grappling(null, await FormatIdAsync("bjj", "gi"), await CharacterIdAsync("sparring"), rounds: [round]);

        var errors = await ValidateAsync(request);

        errors.Should().Equal("DisciplineId");
    }

    [Fact]
    public async Task Unknown_character_fails_on_character_id()
    {
        var request = Grappling(await DisciplineIdAsync("bjj"), await FormatIdAsync("bjj", "gi"), UnknownId);

        var errors = await ValidateAsync(request);

        errors.Should().Equal("CharacterId");
    }

    [Fact]
    public async Task Unknown_activity_type_fails_on_activity_type_id()
    {
        var errors = await ValidateAsync(Other(UnknownId));

        errors.Should().Equal("ActivityTypeId");
    }

    [Fact]
    public async Task Own_club_passes()
    {
        var (ownerId, clubId) = await AddUserWithClubAsync();

        var errors = await ValidateAsync(await SessionAsync(clubId: clubId), ownerId);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Session_without_a_club_passes()
    {
        var errors = await ValidateAsync(await SessionAsync(clubId: null));

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_club_fails_on_club_id()
    {
        var errors = await ValidateAsync(await SessionAsync(clubId: UnknownId), Guid.NewGuid());

        errors.Should().Equal("ClubId");
    }

    [Fact]
    public async Task Club_of_another_user_fails_on_club_id()
    {
        var (_, clubId) = await AddUserWithClubAsync();

        var errors = await ValidateAsync(await SessionAsync(clubId: clubId), Guid.NewGuid());

        errors.Should().Equal("ClubId");
    }

    [Fact]
    public async Task Club_of_another_user_gets_the_same_message_as_an_unknown_club()
    {
        var (_, clubId) = await AddUserWithClubAsync();
        var otherUserId = Guid.NewGuid();

        var foreignClub = await ValidateMessagesAsync(await SessionAsync(clubId: clubId), otherUserId);
        var unknownClub = await ValidateMessagesAsync(await SessionAsync(clubId: UnknownId), otherUserId);

        foreignClub.Should().Equal(unknownClub);
    }

    [Theory]
    [InlineData("technique")]
    [InlineData("drilling")]
    [InlineData("situational_drills")]
    [InlineData("seminar")]
    [InlineData("yoga_mobility")]
    public async Task Rounds_fail_when_character_does_not_allow_them(string character)
    {
        var errors = await ValidateAsync(await SessionAsync(character: character, rounds: [new RoundRequest { Result = RoundResult.Win }]));

        errors.Should().Equal("Rounds");
    }

    [Theory]
    [InlineData("sparring")]
    [InlineData("mixed")]
    [InlineData("open_mat")]
    public async Task Rounds_pass_when_character_allows_them(string character)
    {
        var errors = await ValidateAsync(await SessionAsync(character: character, rounds: [new RoundRequest { Result = RoundResult.Win }]));

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("technique")]
    [InlineData("drilling")]
    [InlineData("situational_drills")]
    [InlineData("seminar")]
    [InlineData("yoga_mobility")]
    [InlineData("sparring")]
    [InlineData("mixed")]
    [InlineData("open_mat")]
    public async Task Session_without_rounds_passes_for_any_character(string character)
    {
        var errors = await ValidateAsync(await SessionAsync(character: character));

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("wrestling", "freestyle", "armbar")]
    [InlineData("judo", "standard", "inside_heel_hook")]
    [InlineData("judo", "standard", "neck_crank")]
    [InlineData("bjj", "gi", "pin_fall")]
    public async Task Technique_not_offered_in_discipline_fails(string discipline, string format, string technique)
    {
        var round = Win(await TechniqueIdAsync(technique));

        var errors = await ValidateAsync(await SessionAsync(discipline, format, rounds: [round]));

        errors.Should().Equal("Rounds[0].TechniqueId");
    }

    [Theory]
    [InlineData("bjj", "gi", "double_leg")]
    [InlineData("wrestling", "freestyle", "double_leg")]
    [InlineData("judo", "standard", "uchi_mata")]
    [InlineData("sambo", "sport", "seoi_nage")]
    public async Task Technique_of_a_shared_category_passes(string discipline, string format, string technique)
    {
        var round = Win(await TechniqueIdAsync(technique));

        var errors = await ValidateAsync(await SessionAsync(discipline, format, rounds: [round]));

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_technique_fails_on_technique_id()
    {
        var errors = await ValidateAsync(await SessionAsync(rounds: [Win(UnknownId)]));

        errors.Should().Equal("Rounds[0].TechniqueId");
    }

    [Fact]
    public async Task Only_the_round_with_the_bad_technique_is_flagged()
    {
        var doubleLeg = Win(await TechniqueIdAsync("double_leg"));
        var armbar = Win(await TechniqueIdAsync("armbar"));

        var errors = await ValidateAsync(await SessionAsync("wrestling", "freestyle", rounds: [doubleLeg, armbar, doubleLeg]));

        errors.Should().Equal("Rounds[1].TechniqueId");
    }

    [Theory]
    [InlineData("wrestling", "freestyle", "double_leg", "closed_guard")]
    [InlineData("bjj", "gi", "armbar", "leg_ride")]
    [InlineData("judo", "standard", "armbar", "saddle")]
    public async Task Position_not_offered_in_discipline_fails(string discipline, string format, string technique, string position)
    {
        var round = Win(await TechniqueIdAsync(technique), await PositionIdAsync(position));

        var errors = await ValidateAsync(await SessionAsync(discipline, format, rounds: [round]));

        errors.Should().Equal("Rounds[0].PositionId");
    }

    [Theory]
    [InlineData("wrestling", "freestyle", "double_leg", "leg_ride")]
    [InlineData("sambo", "sport", "armbar", "saddle")]
    public async Task Position_offered_in_discipline_passes(string discipline, string format, string technique, string position)
    {
        var round = Win(await TechniqueIdAsync(technique), await PositionIdAsync(position));

        var errors = await ValidateAsync(await SessionAsync(discipline, format, rounds: [round]));

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_position_fails_on_position_id()
    {
        var round = Win(await TechniqueIdAsync("armbar"), UnknownId);

        var errors = await ValidateAsync(await SessionAsync(rounds: [round]));

        errors.Should().Equal("Rounds[0].PositionId");
    }

    [Fact]
    public async Task Null_round_is_reported_once_and_does_not_break_the_database_rules()
    {
        var round = Win(await TechniqueIdAsync("armbar"));

        var errors = await ValidateAsync(await SessionAsync(rounds: [null!, round]));

        errors.Should().Equal("Rounds[0]");
    }

    [Fact]
    public async Task Fifty_rounds_run_the_same_queries_as_one_round()
    {
        var (ownerId, clubId) = await AddUserWithClubAsync();
        var (techniqueIds, positionIds) = await OfferedInBjjAsync();
        var fiftyRounds = Enumerable.Range(0, 50)
            .Select(i => Win(techniqueIds[i % techniqueIds.Count], positionIds[i % positionIds.Count]))
            .ToList();

        var forOneRound = await CountQueriesAsync(await SessionAsync(clubId: clubId, rounds: fiftyRounds[..1]), ownerId);
        var forFiftyRounds = await CountQueriesAsync(await SessionAsync(clubId: clubId, rounds: fiftyRounds), ownerId);

        forOneRound.Should().Be(5);
        forFiftyRounds.Should().Be(forOneRound);
    }

    private async Task<List<string>> ValidateAsync(SessionRequest request, Guid? userId = null)
    {
        await using var db = fixture.CreateDbContext(userId);
        var result = await new SessionRequestValidator(db).ValidateAsync(request);
        return result.Errors.Select(e => e.PropertyName).ToList();
    }

    private async Task<List<string>> ValidateMessagesAsync(SessionRequest request, Guid userId)
    {
        await using var db = fixture.CreateDbContext(userId);
        var result = await new SessionRequestValidator(db).ValidateAsync(request);
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }

    private async Task<int> CountQueriesAsync(SessionRequest request, Guid userId)
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateDbContext(userId, counter);

        var result = await new SessionRequestValidator(db).ValidateAsync(request);

        result.Errors.Should().BeEmpty();
        return counter.Count;
    }

    private static RoundRequest Win(int techniqueId, int? positionId = null) =>
        new() { Result = RoundResult.Win, TechniqueId = techniqueId, PositionId = positionId };

    private static SessionRequest Grappling(int? disciplineId, int formatId, int characterId, int? clubId = null,
        List<RoundRequest>? rounds = null) =>
        new()
        {
            Kind = SessionKind.Grappling,
            Date = new DateOnly(2026, 10, 20),
            DurationMinutes = 90,
            Feeling = 4,
            DisciplineId = disciplineId,
            FormatId = formatId,
            CharacterId = characterId,
            ClubId = clubId,
            Rounds = rounds ?? []
        };

    private static SessionRequest Other(int activityTypeId) =>
        new()
        {
            Kind = SessionKind.Other,
            Date = new DateOnly(2026, 10, 20),
            DurationMinutes = 45,
            Feeling = 3,
            ActivityTypeId = activityTypeId
        };

    private async Task<SessionRequest> SessionAsync(string discipline = "bjj", string format = "gi",
        string character = "sparring", int? clubId = null, List<RoundRequest>? rounds = null) =>
        Grappling(await DisciplineIdAsync(discipline), await FormatIdAsync(discipline, format),
            await CharacterIdAsync(character), clubId, rounds);

    private async Task<int> DisciplineIdAsync(string code)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Disciplines.Where(d => d.Code == code).Select(d => d.Id).SingleAsync();
    }

    private async Task<int> FormatIdAsync(string discipline, string code)
    {
        await using var db = fixture.CreateDbContext();
        return await db.DisciplineFormats.Where(f => f.Discipline.Code == discipline && f.Code == code).Select(f => f.Id).SingleAsync();
    }

    private async Task<int> CharacterIdAsync(string code)
    {
        await using var db = fixture.CreateDbContext();
        return await db.TrainingCharacters.Where(c => c.Code == code).Select(c => c.Id).SingleAsync();
    }

    private async Task<int> ActivityTypeIdAsync(string code)
    {
        await using var db = fixture.CreateDbContext();
        return await db.ActivityTypes.Where(a => a.Code == code).Select(a => a.Id).SingleAsync();
    }

    private async Task<int> TechniqueIdAsync(string code)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Techniques.Where(t => t.Code == code).Select(t => t.Id).SingleAsync();
    }

    private async Task<int> PositionIdAsync(string code)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Positions.Where(p => p.Code == code).Select(p => p.Id).SingleAsync();
    }

    private async Task<(List<int> TechniqueIds, List<int> PositionIds)> OfferedInBjjAsync()
    {
        await using var db = fixture.CreateDbContext();
        var techniqueIds = await db.Techniques
            .Where(t => t.Category.Disciplines.Any(d => d.Discipline.Code == "bjj"))
            .Select(t => t.Id)
            .ToListAsync();
        var positionIds = await db.PositionDisciplines
            .Where(p => p.Discipline.Code == "bjj")
            .Select(p => p.PositionId)
            .ToListAsync();
        return (techniqueIds, positionIds);
    }

    private async Task<(Guid OwnerId, int ClubId)> AddUserWithClubAsync()
    {
        var ownerId = Guid.NewGuid();
        await using var db = fixture.CreateDbContext();
        await TestData.AddUserWithFullDataAsync(db, ownerId);
        var clubId = await db.Clubs.IgnoreQueryFilters().Where(c => c.UserId == ownerId).Select(c => c.Id).SingleAsync();
        return (ownerId, clubId);
    }

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
