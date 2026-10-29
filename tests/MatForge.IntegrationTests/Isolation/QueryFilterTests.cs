using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Isolation;

[Collection("Database")]
public class QueryFilterTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Query_filter_hides_other_users_data()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await using (var setup = fixture.CreateDbContext())
        {
            await TestData.AddUserWithFullDataAsync(setup, userA);
        }

        await using (var asUserB = fixture.CreateDbContext(userB))
        {
            (await asUserB.Clubs.CountAsync()).Should().Be(0);
            (await asUserB.TrainingSessions.CountAsync()).Should().Be(0);
            (await asUserB.SparringRounds.CountAsync()).Should().Be(0);
            (await asUserB.Competitions.CountAsync()).Should().Be(0);
            (await asUserB.Fights.CountAsync()).Should().Be(0);
            (await asUserB.Goals.CountAsync()).Should().Be(0);
            (await asUserB.UserBelts.CountAsync()).Should().Be(0);
            (await asUserB.Materials.CountAsync()).Should().Be(0);
            (await asUserB.BodyMeasurements.CountAsync()).Should().Be(0);
        }

        await using (var asUserA = fixture.CreateDbContext(userA))
        {
            (await asUserA.Clubs.CountAsync()).Should().Be(1);
            (await asUserA.TrainingSessions.CountAsync()).Should().Be(1);
            (await asUserA.SparringRounds.CountAsync()).Should().Be(1);
            (await asUserA.Competitions.CountAsync()).Should().Be(1);
            (await asUserA.Fights.CountAsync()).Should().Be(1);
            (await asUserA.Goals.CountAsync()).Should().Be(1);
            (await asUserA.UserBelts.CountAsync()).Should().Be(1);
            (await asUserA.Materials.CountAsync()).Should().Be(1);
            (await asUserA.BodyMeasurements.CountAsync()).Should().Be(1);
        }
    }
}
