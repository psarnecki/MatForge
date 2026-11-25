using System.Data.Common;
using FluentAssertions;
using MatForge.Application.Features.Competitions;
using MatForge.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MatForge.IntegrationTests.Competitions;

// Runs the validator directly against the container database (no HTTP)
[Collection("Database")]
public class CompetitionRequestValidatorTests(DatabaseFixture fixture)
{
    private const int UnknownId = 0;

    [Fact]
    public async Task Valid_competition_with_fights_passes()
    {
        var fights = new List<FightRequest>
        {
            Fight(FightMethod.Submission, await TechniqueIdAsync("armbar")),
            new() { Result = FightResult.Loss, Method = FightMethod.Points },
            new() { Result = FightResult.Draw }
        };

        var errors = await ValidateAsync(await CompetitionAsync(fights: fights));

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Field_rules_run_as_part_of_this_validator()
    {
        var draw = new FightRequest { Result = FightResult.Draw, Method = FightMethod.Points };

        var errors = await ValidateAsync(await CompetitionAsync(fights: [draw]));

        errors.Should().Equal("Fights[0].Method");
    }

    [Fact]
    public async Task Format_of_another_discipline_fails_on_format_id()
    {
        var request = Competition(await DisciplineIdAsync("bjj"), await FormatIdAsync("wrestling", "freestyle"));

        var errors = await ValidateAsync(request);

        errors.Should().Equal("FormatId");
    }

    [Fact]
    public async Task Unknown_format_fails_on_format_id()
    {
        var errors = await ValidateAsync(Competition(await DisciplineIdAsync("bjj"), UnknownId));

        errors.Should().Equal("FormatId");
    }

    [Fact]
    public async Task Unknown_discipline_fails_on_format_id()
    {
        var errors = await ValidateAsync(Competition(UnknownId, await FormatIdAsync("bjj", "gi")));

        errors.Should().Equal("FormatId");
    }

    [Theory]
    [InlineData("wrestling", "freestyle", FightMethod.Submission, "armbar")]
    [InlineData("judo", "standard", FightMethod.Submission, "inside_heel_hook")]
    [InlineData("judo", "standard", FightMethod.Submission, "neck_crank")]
    [InlineData("bjj", "gi", FightMethod.Pin, "pin_fall")]
    [InlineData("wrestling", "freestyle", FightMethod.Pin, "kesa_gatame")]
    public async Task Technique_not_offered_in_discipline_fails(string discipline, string format, FightMethod method,
        string technique)
    {
        var fight = Fight(method, await TechniqueIdAsync(technique));

        var errors = await ValidateAsync(await CompetitionAsync(discipline, format, [fight]));

        errors.Should().Equal("Fights[0].TechniqueId");
    }

    [Theory]
    [InlineData("bjj", "nogi", FightMethod.Throw, "double_leg")]
    [InlineData("wrestling", "greco_roman", FightMethod.Throw, "suplex")]
    [InlineData("wrestling", "freestyle", FightMethod.Pin, "pin_fall")]
    [InlineData("judo", "standard", FightMethod.Pin, "kesa_gatame")]
    [InlineData("sambo", "combat", FightMethod.Submission, "rear_naked_choke")]
    public async Task Technique_offered_in_discipline_passes(string discipline, string format, FightMethod method,
        string technique)
    {
        var fight = Fight(method, await TechniqueIdAsync(technique));

        var errors = await ValidateAsync(await CompetitionAsync(discipline, format, [fight]));

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_technique_fails_on_technique_id()
    {
        var errors = await ValidateAsync(await CompetitionAsync(fights: [Fight(FightMethod.Submission, UnknownId)]));

        errors.Should().Equal("Fights[0].TechniqueId");
    }

    [Fact]
    public async Task Only_the_fight_with_the_bad_technique_is_flagged()
    {
        var doubleLeg = Fight(FightMethod.Throw, await TechniqueIdAsync("double_leg"));
        var armbar = Fight(FightMethod.Submission, await TechniqueIdAsync("armbar"));

        var errors = await ValidateAsync(await CompetitionAsync("wrestling", "freestyle", [doubleLeg, armbar, doubleLeg]));

        errors.Should().Equal("Fights[1].TechniqueId");
    }

    [Fact]
    public async Task Null_fight_is_reported_once_and_does_not_break_the_database_rules()
    {
        var fight = Fight(FightMethod.Submission, await TechniqueIdAsync("armbar"));

        var errors = await ValidateAsync(await CompetitionAsync(fights: [null!, fight]));

        errors.Should().Equal("Fights[0]");
    }

    [Fact]
    public async Task Thirty_fights_run_the_same_queries_as_one_fight()
    {
        var techniqueIds = await OfferedInBjjAsync();
        var thirtyFights = Enumerable.Range(0, 30)
            .Select(i => Fight(FightMethod.Submission, techniqueIds[i % techniqueIds.Count]))
            .ToList();

        var forOneFight = await CountQueriesAsync(await CompetitionAsync(fights: thirtyFights[..1]));
        var forThirtyFights = await CountQueriesAsync(await CompetitionAsync(fights: thirtyFights));

        forOneFight.Should().Be(2);
        forThirtyFights.Should().Be(forOneFight);
    }

    private async Task<List<string>> ValidateAsync(CompetitionRequest request)
    {
        await using var db = fixture.CreateDbContext(Guid.NewGuid());
        var result = await new CompetitionRequestValidator(db).ValidateAsync(request);
        return result.Errors.Select(e => e.PropertyName).ToList();
    }

    private async Task<int> CountQueriesAsync(CompetitionRequest request)
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateDbContext(Guid.NewGuid(), counter);

        var result = await new CompetitionRequestValidator(db).ValidateAsync(request);

        result.Errors.Should().BeEmpty();
        return counter.Count;
    }

    private static FightRequest Fight(FightMethod method, int techniqueId) =>
        new() { Result = FightResult.Win, Method = method, TechniqueId = techniqueId };

    private static CompetitionRequest Competition(int disciplineId, int formatId, List<FightRequest>? fights = null) =>
        new()
        {
            Name = "Warsaw Open",
            Date = new DateOnly(2026, 9, 12),
            DisciplineId = disciplineId,
            FormatId = formatId,
            Fights = fights ?? []
        };

    private async Task<CompetitionRequest> CompetitionAsync(string discipline = "bjj", string format = "gi",
        List<FightRequest>? fights = null) =>
        Competition(await DisciplineIdAsync(discipline), await FormatIdAsync(discipline, format), fights);

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

    private async Task<int> TechniqueIdAsync(string code)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Techniques.Where(t => t.Code == code).Select(t => t.Id).SingleAsync();
    }

    private async Task<List<int>> OfferedInBjjAsync()
    {
        await using var db = fixture.CreateDbContext();
        return await db.Techniques
            .Where(t => t.Category.Disciplines.Any(d => d.Discipline.Code == "bjj"))
            .Select(t => t.Id)
            .ToListAsync();
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
