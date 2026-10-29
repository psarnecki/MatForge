using System.Net;
using FluentAssertions;
using MatForge.Data;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Profile;

[Collection("Database")]
public class ProfileTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Deleting_account_removes_all_user_data()
    {
        var userA = await fixture.CreateLoggedInUserAsync();
        var userB = await fixture.CreateLoggedInUserAsync();
        await using (var setup = fixture.CreateDbContext())
        {
            await TestData.AddUserWithFullDataAsync(setup, userA.Id);
            await TestData.AddUserWithFullDataAsync(setup, userB.Id);
        }

        var response = await userA.Client.DeleteAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await userA.Client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var db = fixture.CreateDbContext();
        (await CountRowsAsync(db, userA.Id)).Should().AllSatisfy(table => table.Value.Should().Be(0, table.Key));
        (await CountRowsAsync(db, userB.Id)).Should().AllSatisfy(table => table.Value.Should().Be(1, table.Key));
    }

    // Counts every user table without query filters (rounds and fights through their parent)
    private static async Task<Dictionary<string, int>> CountRowsAsync(AppDbContext db, Guid userId) => new()
    {
        ["Users"] = await db.Users.CountAsync(u => u.Id == userId),
        ["Clubs"] = await db.Clubs.IgnoreQueryFilters().CountAsync(e => e.UserId == userId),
        ["TrainingSessions"] = await db.TrainingSessions.IgnoreQueryFilters().CountAsync(e => e.UserId == userId),
        ["SparringRounds"] = await db.SparringRounds.IgnoreQueryFilters().CountAsync(e => e.TrainingSession.UserId == userId),
        ["Competitions"] = await db.Competitions.IgnoreQueryFilters().CountAsync(e => e.UserId == userId),
        ["Fights"] = await db.Fights.IgnoreQueryFilters().CountAsync(e => e.Competition.UserId == userId),
        ["Goals"] = await db.Goals.IgnoreQueryFilters().CountAsync(e => e.UserId == userId),
        ["UserBelts"] = await db.UserBelts.IgnoreQueryFilters().CountAsync(e => e.UserId == userId),
        ["Materials"] = await db.Materials.IgnoreQueryFilters().CountAsync(e => e.UserId == userId),
        ["BodyMeasurements"] = await db.BodyMeasurements.IgnoreQueryFilters().CountAsync(e => e.UserId == userId)
    };
}
