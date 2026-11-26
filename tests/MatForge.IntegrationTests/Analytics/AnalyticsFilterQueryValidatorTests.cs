using System.Data.Common;
using FluentAssertions;
using MatForge.Application.Features.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MatForge.IntegrationTests.Analytics;

// Runs the validator directly against the container database (no HTTP)
[Collection("Database")]
public class AnalyticsFilterQueryValidatorTests(DatabaseFixture fixture)
{
    private const int UnknownId = 0;

    [Fact]
    public async Task Query_without_filters_passes()
    {
        var errors = await ValidateAsync(new AnalyticsFilterQuery());

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Period_rules_run_as_part_of_this_validator()
    {
        var query = new AnalyticsFilterQuery { From = new DateOnly(2026, 9, 10), To = new DateOnly(2026, 9, 9) };

        var errors = await ValidateAsync(query);

        errors.Should().Equal("To");
    }

    [Fact]
    public async Task Known_discipline_passes()
    {
        var errors = await ValidateAsync(new AnalyticsFilterQuery { DisciplineId = await DisciplineIdAsync("bjj") });

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Discipline_with_its_own_format_passes()
    {
        var query = new AnalyticsFilterQuery
        {
            DisciplineId = await DisciplineIdAsync("wrestling"),
            FormatId = await FormatIdAsync("wrestling", "freestyle")
        };

        var errors = await ValidateAsync(query);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Known_format_alone_passes()
    {
        var errors = await ValidateAsync(new AnalyticsFilterQuery { FormatId = await FormatIdAsync("wrestling", "freestyle") });

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_discipline_fails_on_discipline_id()
    {
        var errors = await ValidateAsync(new AnalyticsFilterQuery { DisciplineId = UnknownId });

        errors.Should().Equal("DisciplineId");
    }

    [Fact]
    public async Task Unknown_discipline_with_a_format_fails_on_both_fields()
    {
        var query = new AnalyticsFilterQuery { DisciplineId = UnknownId, FormatId = await FormatIdAsync("bjj", "gi") };

        var errors = await ValidateAsync(query);

        errors.Should().Equal("DisciplineId", "FormatId");
    }

    [Fact]
    public async Task Format_of_another_discipline_fails_on_format_id()
    {
        var query = new AnalyticsFilterQuery
        {
            DisciplineId = await DisciplineIdAsync("bjj"),
            FormatId = await FormatIdAsync("wrestling", "freestyle")
        };

        var errors = await ValidateAsync(query);

        errors.Should().Equal("FormatId");
    }

    [Fact]
    public async Task Unknown_format_with_a_discipline_fails_on_format_id()
    {
        var query = new AnalyticsFilterQuery { DisciplineId = await DisciplineIdAsync("bjj"), FormatId = UnknownId };

        var errors = await ValidateAsync(query);

        errors.Should().Equal("FormatId");
    }

    [Fact]
    public async Task Unknown_format_alone_fails_on_format_id()
    {
        var errors = await ValidateAsync(new AnalyticsFilterQuery { FormatId = UnknownId });

        errors.Should().Equal("FormatId");
    }

    [Fact]
    public async Task Each_filter_adds_one_query()
    {
        var withoutFilters = await CountQueriesAsync(new AnalyticsFilterQuery());
        var withBothFilters = await CountQueriesAsync(new AnalyticsFilterQuery
        {
            DisciplineId = await DisciplineIdAsync("bjj"),
            FormatId = await FormatIdAsync("bjj", "gi")
        });

        withoutFilters.Should().Be(0);
        withBothFilters.Should().Be(2);
    }

    private async Task<List<string>> ValidateAsync(AnalyticsFilterQuery query)
    {
        await using var db = fixture.CreateDbContext(Guid.NewGuid());
        var result = await new AnalyticsFilterQueryValidator(db).ValidateAsync(query);
        return result.Errors.Select(e => e.PropertyName).ToList();
    }

    private async Task<int> CountQueriesAsync(AnalyticsFilterQuery query)
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateDbContext(Guid.NewGuid(), counter);

        var result = await new AnalyticsFilterQueryValidator(db).ValidateAsync(query);

        result.Errors.Should().BeEmpty();
        return counter.Count;
    }

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
