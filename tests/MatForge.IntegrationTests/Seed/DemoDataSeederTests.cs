using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using MatForge.Application.Features.Competitions;
using MatForge.Application.Features.Sessions;
using MatForge.Data.Entities;
using MatForge.Data.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Seed;

[Collection("Database")]
public class DemoDataSeederTests(DatabaseFixture fixture)
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public async Task Start_with_demo_data_creates_the_demo_user_who_can_log_in()
    {
        await using (var db = fixture.CreateDbContext())
        {
            await db.Users.Where(u => u.Email == DemoDataSeeder.Email).ExecuteDeleteAsync();
        }

        await StartWithDemoDataAsync();

        var client = fixture.ApiFactory.CreateClient();
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true",
            new { email = DemoDataSeeder.Email, password = "Demo1234!" })).EnsureSuccessStatusCode();
        var profile = (await client.GetFromJsonAsync<JsonObject>("/api/me"))!;
        profile["firstName"]!.GetValue<string>().Should().Be("Alex");
        profile["clubs"]!.AsArray().Should().HaveCount(3);
        profile["belts"]!.AsArray().Should().ContainSingle();
        var rows = await CountDemoRowsAsync();
        rows["Sessions"].Should().BeGreaterThan(1000);
        rows["Rounds"].Should().BeGreaterThan(3000);
        rows["Competitions"].Should().BeGreaterThan(20);
    }

    [Fact]
    public async Task Second_start_creates_nothing()
    {
        await StartWithDemoDataAsync();
        var before = await CountDemoRowsAsync();

        await StartWithDemoDataAsync();

        before["Users"].Should().Be(1);
        (await CountDemoRowsAsync()).Should().Equal(before);
    }

    [Fact]
    public async Task Layout_is_the_same_relative_to_the_week_of_the_end_date()
    {
        await using var db = fixture.CreateDbContext();
        var dictionary = await DemoDictionary.LoadAsync(db);
        var monday = new DateOnly(2026, 11, 23);

        var wednesday = Layout(dictionary, monday.AddDays(2));
        var sunday = Layout(dictionary, monday.AddDays(6));

        Layout(dictionary, monday.AddDays(2)).Should().Equal(wednesday);
        Layout(dictionary, monday.AddDays(9)).Should().Equal(wednesday);
        wednesday.Should().Equal(sunday.Where(s => s.Day <= 2));
        Layout(dictionary, monday).Should().Equal(sunday.Where(s => s.Day <= 0));

        var competitions = CompetitionLayout(dictionary, monday.AddDays(2));
        CompetitionLayout(dictionary, monday.AddDays(9)).Should().Equal(competitions);
        CompetitionLayout(dictionary, monday).Should().Equal(competitions);
        CompetitionLayout(dictionary, monday.AddDays(6)).Should().Equal(competitions);

        // The injury note is on Monday of week 24 and the comeback note on Wednesday of week 21
        DemoDataGenerator.Generate(monday, dictionary).Sessions.Where(s => s.Notes != null)
            .Select(s => s.Date.DayNumber - monday.DayNumber)
            .Should().Equal(-24 * 7, -21 * 7 + 2);
    }

    [Fact]
    public async Task Every_demo_session_passes_the_session_validators()
    {
        var userId = await EnsureDemoUserAsync();
        await using var db = fixture.CreateDbContext(userId);
        var sessions = await db.TrainingSessions.AsNoTracking()
            .Include(s => s.Rounds.OrderBy(r => r.Order))
            .OrderBy(s => s.Date).ThenBy(s => s.Id)
            .ToListAsync();
        var validator = new SessionRequestValidator(db);

        var errors = new List<string>();
        foreach (var session in sessions)
        {
            var request = ToRequest(session);
            errors.AddRange(DataAnnotationErrors(request).Concat(request.Rounds.SelectMany(DataAnnotationErrors))
                .Select(error => $"{session.Date}: {error}"));
            var result = await validator.ValidateAsync(request);
            errors.AddRange(result.Errors.Select(error => $"{session.Date} {error.PropertyName}: {error.ErrorMessage}"));
        }

        sessions.Should().NotBeEmpty();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Every_demo_competition_passes_the_competition_validators()
    {
        var userId = await EnsureDemoUserAsync();
        await using var db = fixture.CreateDbContext(userId);
        var competitions = await db.Competitions.AsNoTracking()
            .Include(c => c.Fights.OrderBy(f => f.Order))
            .OrderBy(c => c.Date).ThenBy(c => c.Id)
            .ToListAsync();
        var validator = new CompetitionRequestValidator(db);

        var errors = new List<string>();
        foreach (var competition in competitions)
        {
            var request = ToRequest(competition);
            errors.AddRange(DataAnnotationErrors(request).Concat(request.Fights.SelectMany(DataAnnotationErrors))
                .Select(error => $"{competition.Date}: {error}"));
            var result = await validator.ValidateAsync(request);
            errors.AddRange(result.Errors.Select(error => $"{competition.Date} {error.PropertyName}: {error.ErrorMessage}"));
        }

        competitions.Should().NotBeEmpty();
        errors.Should().BeEmpty();
    }

    // Tests the 90-day window ending on each of the last 7 days to cover every weekday
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Last_90_days_show_every_training_rule_except_a_decline(int daysBeforeToday)
    {
        await EnsureDemoUserAsync();
        var to = Today.AddDays(-daysBeforeToday);

        var training = (await GetDemoInsightsAsync(to.AddDays(-89), to))["training"]!;

        training["status"]!.GetValue<string>().Should().Be("Ok");
        var items = Items(training["items"]!);
        items.Should().Contain(i => i.StartsWith("Strength ")).And.Contain(i => i.StartsWith("Weakness "));
        items.Should().Contain([
            "Strength DominantFinisherTechnique Rear Naked Choke",
            "Strength DominantFinisherCategory Choke",
            "Weakness RecurringLossTechnique Inside Heel Hook",
            "Weakness RecurringLossCategory Leg Lock",
            "Strength StrongPosition Back Control",
            "Weakness VulnerablePosition Saddle / Cross Ashi",
            "Trend ImprovingWinRate "
        ]);
        items.Should().NotContain(i => i.Contains("DecliningWinRate"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Last_90_days_show_competition_strengths_weaknesses_decline_and_the_gap(int daysBeforeToday)
    {
        await EnsureDemoUserAsync();
        var to = Today.AddDays(-daysBeforeToday);

        var insights = await GetDemoInsightsAsync(to.AddDays(-89), to);

        var competition = insights["competition"]!;
        competition["status"]!.GetValue<string>().Should().Be("Ok");
        competition["samples"]!.GetValue<int>().Should().Be(8);
        Items(competition["items"]!).Should().Equal(
            "Strength DominantFinisherTechnique Rear Naked Choke",
            "Strength DominantFinisherCategory Choke",
            "Weakness RecurringLossTechnique Inside Heel Hook",
            "Weakness RecurringLossCategory Leg Lock",
            "Trend DecliningWinRate ");
        Items(insights["comparison"]!).Should().Equal("Info CompetitionVsTrainingGap ");
    }

    [Fact]
    public async Task Weeks_after_the_injury_show_a_declining_training_win_rate()
    {
        await EnsureDemoUserAsync();
        var endWeekMonday = Today.AddDays(-(((int)Today.DayOfWeek + 6) % 7));

        var training = (await GetDemoInsightsAsync(endWeekMonday.AddDays(-21 * 7), endWeekMonday.AddDays(-16 * 7 + 6)))["training"]!;

        Items(training["items"]!).Should().Contain("Trend DecliningWinRate ");
    }

    // Starts the API with Seed:DemoData=true (migrations, dictionaries, then demo data)
    private async Task StartWithDemoDataAsync()
    {
        await using var factory = fixture.ApiFactory.WithWebHostBuilder(builder => builder.UseSetting("Seed:DemoData", "true"));
        _ = factory.Server;
    }

    private async Task<JsonNode> GetDemoInsightsAsync(DateOnly from, DateOnly to)
    {
        var client = fixture.ApiFactory.CreateClient();
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true",
            new { email = DemoDataSeeder.Email, password = "Demo1234!" })).EnsureSuccessStatusCode();
        return JsonNode.Parse(await client.GetStringAsync($"/api/analytics/insights?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"))!;
    }

    // Each insight's type, rule and parameter name (empty for trends)
    private static List<string> Items(JsonNode items) =>
        items.AsArray()
            .Select(i => $"{i!["type"]!.GetValue<string>()} {i["rule"]!.GetValue<string>()} {i["params"]!["name"]?.GetValue<string>()}")
            .ToList();

    private async Task<Guid> EnsureDemoUserAsync()
    {
        await using var db = fixture.CreateDbContext();
        if (!await db.Users.AnyAsync(u => u.Email == DemoDataSeeder.Email))
        {
            await StartWithDemoDataAsync();
        }

        return await db.Users.Where(u => u.Email == DemoDataSeeder.Email).Select(u => u.Id).SingleAsync();
    }

    private async Task<Dictionary<string, int>> CountDemoRowsAsync()
    {
        await using var db = fixture.CreateDbContext();
        var userIds = db.Users.Where(u => u.Email == DemoDataSeeder.Email).Select(u => u.Id);
        return new Dictionary<string, int>
        {
            ["Users"] = await userIds.CountAsync(),
            ["Clubs"] = await db.Clubs.IgnoreQueryFilters().CountAsync(e => userIds.Contains(e.UserId)),
            ["UserBelts"] = await db.UserBelts.IgnoreQueryFilters().CountAsync(e => userIds.Contains(e.UserId)),
            ["Sessions"] = await db.TrainingSessions.IgnoreQueryFilters().CountAsync(e => userIds.Contains(e.UserId)),
            ["Rounds"] = await db.SparringRounds.IgnoreQueryFilters().CountAsync(e => userIds.Contains(e.TrainingSession.UserId)),
            ["Competitions"] = await db.Competitions.IgnoreQueryFilters().CountAsync(e => userIds.Contains(e.UserId)),
            ["Fights"] = await db.Fights.IgnoreQueryFilters().CountAsync(e => userIds.Contains(e.Competition.UserId))
        };
    }

    // Each session as its day counted from the Monday of the end week, followed by all of its fields
    private static List<(int Day, string Session)> Layout(DemoDictionary dictionary, DateOnly endDate)
    {
        var data = DemoDataGenerator.Generate(endDate, dictionary);
        var monday = endDate.AddDays(-(((int)endDate.DayOfWeek + 6) % 7));

        data.Sessions.Should().NotBeEmpty()
            .And.OnlyContain(s => s.Date <= endDate && s.Date >= monday.AddDays(-7 * (DemoDataGenerator.Weeks - 1)));

        return data.Sessions
            .Select(s => (s.Date.DayNumber - monday.DayNumber,
                $"{s.Kind} {s.DisciplineId} {s.FormatId} {s.CharacterId} {s.Club?.Name} {s.ActivityTypeId} " +
                $"{s.DurationMinutes} {s.Feeling} {s.Notes} " +
                string.Join(",", s.Rounds.Select(r => $"{r.Result}/{r.TechniqueId}/{r.PositionId}"))))
            .ToList();
    }

    // Each competition as its day counted from the Monday of the end week, followed by its fields and fights
    private static List<string> CompetitionLayout(DemoDictionary dictionary, DateOnly endDate)
    {
        var monday = endDate.AddDays(-(((int)endDate.DayOfWeek + 6) % 7));
        return DemoDataGenerator.Generate(endDate, dictionary).Competitions
            .Select(c => $"{c.Date.DayNumber - monday.DayNumber} {c.Name} {c.DisciplineId} {c.FormatId} {c.Place} {c.ResultNote} " +
                string.Join(",", c.Fights.Select(f => $"{f.Result}/{f.Method}/{f.TechniqueId}/{f.DurationSeconds}/{f.OpponentName}")))
            .ToList();
    }

    private static CompetitionRequest ToRequest(Competition competition) => new()
    {
        Name = competition.Name,
        Date = competition.Date,
        Location = competition.Location,
        DisciplineId = competition.DisciplineId,
        FormatId = competition.FormatId,
        AgeCategory = competition.AgeCategory,
        SkillLevel = competition.SkillLevel,
        WeightCategory = competition.WeightCategory,
        Place = competition.Place,
        ResultNote = competition.ResultNote,
        Notes = competition.Notes,
        Fights = competition.Fights
            .Select(f => new FightRequest
            {
                OpponentName = f.OpponentName, Result = f.Result, Method = f.Method, TechniqueId = f.TechniqueId,
                DurationSeconds = f.DurationSeconds, Notes = f.Notes
            })
            .ToList()
    };

    private static SessionRequest ToRequest(TrainingSession session) => new()
    {
        Kind = session.Kind,
        Date = session.Date,
        DurationMinutes = session.DurationMinutes,
        Feeling = session.Feeling,
        Notes = session.Notes,
        DisciplineId = session.DisciplineId,
        FormatId = session.FormatId,
        CharacterId = session.CharacterId,
        ClubId = session.ClubId,
        ActivityTypeId = session.ActivityTypeId,
        Rounds = session.Rounds
            .Select(r => new RoundRequest { Result = r.Result, TechniqueId = r.TechniqueId, PositionId = r.PositionId, Notes = r.Notes })
            .ToList()
    };

    // The same DataAnnotations checks the API runs before the validators
    private static IEnumerable<string> DataAnnotationErrors(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage!);
    }
}
