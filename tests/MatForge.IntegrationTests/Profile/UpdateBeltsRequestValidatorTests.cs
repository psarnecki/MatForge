using FluentAssertions;
using MatForge.Application.Features.Profile;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Profile;

// Runs the validator directly against the container database (no HTTP)
[Collection("Database")]
public class UpdateBeltsRequestValidatorTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Valid_belts_pass()
    {
        var bjjPurple = await BeltAsync("bjj", "purple");
        var judoBlack = await BeltAsync("judo", "black");

        var errors = await ValidateAsync(
            new BeltRequest(bjjPurple.DisciplineId, bjjPurple.BeltId, 4),
            new BeltRequest(judoBlack.DisciplineId, judoBlack.BeltId, 1));

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Belt_of_another_discipline_fails_on_belt_id()
    {
        var bjjWhite = await BeltAsync("bjj", "white");
        var judoYellow = await BeltAsync("judo", "yellow");

        var errors = await ValidateAsync(new BeltRequest(bjjWhite.DisciplineId, judoYellow.BeltId, 0));

        errors.Should().Equal("Belts[0].BeltId");
    }

    [Fact]
    public async Task Unknown_belt_fails_on_belt_id()
    {
        var bjjWhite = await BeltAsync("bjj", "white");

        var errors = await ValidateAsync(new BeltRequest(bjjWhite.DisciplineId, 0, 0));

        errors.Should().Equal("Belts[0].BeltId");
    }

    [Theory]
    [InlineData("bjj", "white", 5)]
    [InlineData("bjj", "white", -1)]
    [InlineData("judo", "black", 0)]
    [InlineData("judo", "green", 1)]
    public async Task Degree_outside_the_belt_range_fails_on_degree(string discipline, string belt, int degree)
    {
        var chosen = await BeltAsync(discipline, belt);

        var errors = await ValidateAsync(new BeltRequest(chosen.DisciplineId, chosen.BeltId, degree));

        errors.Should().Equal("Belts[0].Degree");
    }

    [Fact]
    public async Task Second_belt_for_the_same_discipline_fails_on_discipline_id()
    {
        var bjjWhite = await BeltAsync("bjj", "white");
        var bjjBlue = await BeltAsync("bjj", "blue");

        var errors = await ValidateAsync(
            new BeltRequest(bjjWhite.DisciplineId, bjjWhite.BeltId, 0),
            new BeltRequest(bjjBlue.DisciplineId, bjjBlue.BeltId, 0));

        errors.Should().Equal("Belts[1].DisciplineId");
    }

    private async Task<List<string>> ValidateAsync(params BeltRequest[] belts)
    {
        await using var db = fixture.CreateDbContext();
        var result = await new UpdateBeltsRequestValidator(db).ValidateAsync(new UpdateBeltsRequest { Belts = [..belts] });
        return result.Errors.Select(e => e.PropertyName).ToList();
    }

    private async Task<(int DisciplineId, int BeltId)> BeltAsync(string disciplineCode, string beltCode)
    {
        await using var db = fixture.CreateDbContext();
        var belt = await db.Belts.SingleAsync(b => b.Discipline.Code == disciplineCode && b.Code == beltCode);
        return (belt.DisciplineId, belt.Id);
    }
}
