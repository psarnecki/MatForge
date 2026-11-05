using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MatForge.Application.Features.Clubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Clubs;

[Collection("Database")]
public class ClubTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Create_returns_201_with_location_and_trimmed_name()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var response = await user.Client.PostAsJsonAsync("/api/clubs", new { name = "  Gracie Barra  " });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var club = await response.Content.ReadFromJsonAsync<ClubDto>();
        club!.Name.Should().Be("Gracie Barra");
        response.Headers.Location!.ToString().Should().Be($"/api/clubs/{club.Id}");
    }

    [Fact]
    public async Task List_returns_own_clubs_sorted_by_name()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        await CreateClubAsync(user, "Zeta");
        await CreateClubAsync(user, "Alpha");

        var clubs = await user.Client.GetFromJsonAsync<List<ClubDto>>("/api/clubs");

        clubs!.Select(c => c.Name).Should().Equal("Alpha", "Zeta");
    }

    [Fact]
    public async Task Create_with_existing_name_in_other_case_returns_409()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        await CreateClubAsync(user, "Checkmat");

        var response = await user.Client.PostAsJsonAsync("/api/clubs", new { name = "CHECKMAT" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_with_empty_name_returns_400()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var response = await user.Client.PostAsJsonAsync("/api/clubs", new { name = "  " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Should().ContainKey("name");
    }

    [Fact]
    public async Task Rename_changes_the_name()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var club = await CreateClubAsync(user, "Alliance");

        var response = await user.Client.PutAsJsonAsync($"/api/clubs/{club.Id}", new { name = "ALLIANCE Warsaw" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<ClubDto>()).Should().Be(new ClubDto(club.Id, "ALLIANCE Warsaw"));
    }

    [Fact]
    public async Task Rename_to_name_of_another_club_returns_409()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        await CreateClubAsync(user, "Atos");
        var club = await CreateClubAsync(user, "Nova Uniao");

        var response = await user.Client.PutAsJsonAsync($"/api/clubs/{club.Id}", new { name = "atos" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_of_unused_club_returns_204()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        var club = await CreateClubAsync(user, "Unused");

        var response = await user.Client.DeleteAsync($"/api/clubs/{club.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await user.Client.GetFromJsonAsync<List<ClubDto>>("/api/clubs")).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_of_club_used_by_a_session_returns_409()
    {
        var user = await fixture.CreateLoggedInUserAsync();
        int clubId;
        await using (var db = fixture.CreateDbContext())
        {
            await TestData.AddUserWithFullDataAsync(db, user.Id);
            clubId = await db.Clubs.IgnoreQueryFilters().Where(c => c.UserId == user.Id).Select(c => c.Id).SingleAsync();
        }

        var response = await user.Client.DeleteAsync($"/api/clubs/{clubId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail.Should().Contain("Test Club");
    }

    [Fact]
    public async Task Other_user_cannot_see_rename_or_delete_a_club()
    {
        var userA = await fixture.CreateLoggedInUserAsync();
        var userB = await fixture.CreateLoggedInUserAsync();
        var club = await CreateClubAsync(userA, "Private Club");

        (await userB.Client.GetFromJsonAsync<List<ClubDto>>("/api/clubs")).Should().BeEmpty();
        (await userB.Client.PutAsJsonAsync($"/api/clubs/{club.Id}", new { name = "Taken" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await userB.Client.DeleteAsync($"/api/clubs/{club.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await userA.Client.GetFromJsonAsync<List<ClubDto>>("/api/clubs")).Should().Equal(club);
    }

    [Fact]
    public async Task Two_users_may_have_clubs_with_the_same_name()
    {
        var userA = await fixture.CreateLoggedInUserAsync();
        var userB = await fixture.CreateLoggedInUserAsync();
        await CreateClubAsync(userA, "Same Name");

        var response = await userB.Client.PostAsJsonAsync("/api/clubs", new { name = "Same Name" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<ClubDto> CreateClubAsync(LoggedInUser user, string name)
    {
        var response = await user.Client.PostAsJsonAsync("/api/clubs", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClubDto>())!;
    }
}
