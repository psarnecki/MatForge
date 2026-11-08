using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MatForge.Application.Features.Profile;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Profile;

[Collection("Database")]
public class BeltTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Put_replaces_the_whole_list()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var bjjWhite = await BeltAsync("bjj", "white");
        var bjjBlue = await BeltAsync("bjj", "blue");
        var judoGreen = await BeltAsync("judo", "green");
        await PutBeltsAsync(user, new BeltRequest(bjjWhite.DisciplineId, bjjWhite.BeltId, 3),
            new BeltRequest(judoGreen.DisciplineId, judoGreen.BeltId, 0));

        var response = await PutBeltsAsync(user, new BeltRequest(bjjBlue.DisciplineId, bjjBlue.BeltId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expected = new ProfileBeltDto(bjjBlue.DisciplineId, bjjBlue.BeltId, 1);
        (await response.Content.ReadFromJsonAsync<List<ProfileBeltDto>>()).Should().Equal(expected);
        (await user.Client.GetFromJsonAsync<ProfileDto>("/api/me"))!.Belts.Should().Equal(expected);
    }

    [Fact]
    public async Task Empty_list_removes_all_belts()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var bjjWhite = await BeltAsync("bjj", "white");
        await PutBeltsAsync(user, new BeltRequest(bjjWhite.DisciplineId, bjjWhite.BeltId, 0));

        var response = await PutBeltsAsync(user);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await user.Client.GetFromJsonAsync<ProfileDto>("/api/me"))!.Belts.Should().BeEmpty();
    }

    [Fact]
    public async Task Invalid_belt_returns_400_with_the_field_key()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var bjjWhite = await BeltAsync("bjj", "white");

        var response = await PutBeltsAsync(user, new BeltRequest(bjjWhite.DisciplineId, bjjWhite.BeltId, 9));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys
            .Should().Equal("belts[0].degree");
    }

    [Fact]
    public async Task Null_entry_returns_400_on_its_index()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var response = await user.Client.PutAsJsonAsync("/api/me/belts", new { belts = new object?[] { null } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys
            .Should().Equal("belts[0]");
    }

    [Fact]
    public async Task Put_does_not_touch_other_users_belts()
    {
        var userA = await fixture.CreateLoggedInUserAsync();
        var userB = await fixture.CreateLoggedInUserAsync();
        var bjjWhite = await BeltAsync("bjj", "white");
        await PutBeltsAsync(userA, new BeltRequest(bjjWhite.DisciplineId, bjjWhite.BeltId, 2));

        await PutBeltsAsync(userB);

        (await userA.Client.GetFromJsonAsync<ProfileDto>("/api/me"))!.Belts
            .Should().Equal(new ProfileBeltDto(bjjWhite.DisciplineId, bjjWhite.BeltId, 2));
    }

    private static Task<HttpResponseMessage> PutBeltsAsync(LoggedInUser user, params BeltRequest[] belts) =>
        user.Client.PutAsJsonAsync("/api/me/belts", new { belts });

    private async Task<(int DisciplineId, int BeltId)> BeltAsync(string disciplineCode, string beltCode)
    {
        await using var db = fixture.CreateDbContext();
        var belt = await db.Belts.SingleAsync(b => b.Discipline.Code == disciplineCode && b.Code == beltCode);
        return (belt.DisciplineId, belt.Id);
    }
}
