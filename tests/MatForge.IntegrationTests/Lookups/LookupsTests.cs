using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MatForge.Application.Features.Lookups;

namespace MatForge.IntegrationTests.Lookups;

[Collection("Database")]
public class LookupsTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Returns_all_dictionaries()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var lookups = await user.Client.GetFromJsonAsync<LookupsDto>("/api/lookups");

        lookups!.Disciplines.Should().HaveCount(4);
        lookups.Characters.Should().HaveCount(8);
        lookups.ActivityTypes.Should().HaveCount(6);
        lookups.TechniqueCategories.Should().HaveCount(11);
        lookups.Techniques.Should().HaveCount(61);
        lookups.Positions.Should().HaveCount(24);
        lookups.MaterialCategories.Should().HaveCount(12);

        var bjj = lookups.Disciplines.Single(d => d.Code == "bjj");
        var sambo = lookups.Disciplines.Single(d => d.Code == "sambo");
        bjj.Formats.Select(f => f.Code).Should().Equal("gi", "nogi");
        bjj.Belts.Single(b => b.Code == "white").Should().Match<BeltDto>(b => b.MinDegree == 0 && b.MaxDegree == 4);
        sambo.Belts.Should().BeEmpty();
        lookups.Characters.Single(c => c.Code == "sparring").AllowsRounds.Should().BeTrue();
        lookups.Positions.Single(p => p.Code == "saddle").DisciplineIds.Should().BeEquivalentTo([bjj.Id, sambo.Id]);
    }

    [Fact]
    public async Task Response_may_be_cached_by_the_browser_for_an_hour()
    {
        var user = await fixture.CreateLoggedInUserAsync();

        var response = await user.Client.GetAsync("/api/lookups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromHours(1));
    }
}
