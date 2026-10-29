using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace MatForge.IntegrationTests.Auth;

[Collection("Database")]
public class AuthTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Register_login_profile_logout_flow_works()
    {
        var client = fixture.ApiFactory.CreateClient();
        var credentials = new { email = $"flow-{Guid.NewGuid():N}@test.local", password = "Test1234!" };

        (await client.PostAsJsonAsync("/api/auth/register", credentials)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true", credentials)).StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await client.GetAsync("/api/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await me.Content.ReadAsStringAsync()).Should().Contain(credentials.email);

        (await client.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var client = fixture.ApiFactory.CreateClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
