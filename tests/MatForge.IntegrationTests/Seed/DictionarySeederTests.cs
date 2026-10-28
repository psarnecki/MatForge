using System.Text.Json;
using FluentAssertions;
using MatForge.Data;
using MatForge.Data.Seed;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests.Seed;

[Collection("Database")]
public class DictionarySeederTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Seeder_fills_all_dictionaries()
    {
        await SeedAsync();

        await using var db = fixture.CreateDbContext();
        (await db.Disciplines.CountAsync()).Should().Be(4);
        (await db.DisciplineFormats.CountAsync()).Should().Be(7);
        (await db.Belts.CountAsync()).Should().Be(12);
        (await db.TrainingCharacters.CountAsync()).Should().Be(8);
        (await db.ActivityTypes.CountAsync()).Should().Be(6);
        (await db.TechniqueCategories.CountAsync()).Should().Be(11);
        (await db.TechniqueCategoryDisciplines.CountAsync()).Should().Be(28);
        (await db.Techniques.CountAsync()).Should().Be(61);
        (await db.Positions.CountAsync()).Should().Be(24);
        (await db.PositionDisciplines.CountAsync()).Should().Be(69);
        (await db.MaterialCategories.CountAsync()).Should().Be(12);
    }

    [Fact]
    public async Task Running_seeder_twice_changes_nothing()
    {
        await SeedAsync();
        var before = await SnapshotAsync();

        await SeedAsync();
        var after = await SnapshotAsync();

        after.Should().Equal(before);
    }

    [Theory]
    [InlineData("disciplines.json")]
    [InlineData("training-characters.json")]
    [InlineData("activity-types.json")]
    [InlineData("technique-categories.json")]
    [InlineData("techniques.json")]
    [InlineData("positions.json")]
    [InlineData("material-categories.json")]
    public void Seed_file_has_no_duplicate_codes(string fileName)
    {
        using var file = ReadSeedFile(fileName);
        var entries = file.RootElement.EnumerateArray().ToList();

        CodesOf(entries).Should().OnlyHaveUniqueItems();

        // Format and belt codes only have to be unique within their discipline
        if (fileName == "disciplines.json")
        {
            foreach (var discipline in entries)
            {
                CodesOf(discipline.GetProperty("formats").EnumerateArray()).Should().OnlyHaveUniqueItems();
                CodesOf(discipline.GetProperty("belts").EnumerateArray()).Should().OnlyHaveUniqueItems();
            }
        }
    }

    private async Task SeedAsync()
    {
        await using var db = fixture.CreateDbContext();
        await new DictionarySeeder(db).SeedAsync();
    }

    private async Task<List<string>> SnapshotAsync()
    {
        await using var db = fixture.CreateDbContext();
        var rows = new List<string>();
        rows.AddRange(await db.Disciplines.OrderBy(e => e.Id)
            .Select(e => $"discipline {e.Id} {e.Code} {e.Name} {e.SortOrder}").ToListAsync());
        rows.AddRange(await db.DisciplineFormats.OrderBy(e => e.Id)
            .Select(e => $"format {e.Id} {e.Code} {e.Name} {e.SortOrder} {e.DisciplineId}").ToListAsync());
        rows.AddRange(await db.Belts.OrderBy(e => e.Id)
            .Select(e => $"belt {e.Id} {e.Code} {e.Name} {e.SortOrder} {e.DisciplineId} {e.Rank} {e.MinDegree} {e.MaxDegree}").ToListAsync());
        rows.AddRange(await db.TrainingCharacters.OrderBy(e => e.Id)
            .Select(e => $"character {e.Id} {e.Code} {e.Name} {e.SortOrder} {e.AllowsRounds}").ToListAsync());
        rows.AddRange(await db.ActivityTypes.OrderBy(e => e.Id)
            .Select(e => $"activity {e.Id} {e.Code} {e.Name} {e.SortOrder}").ToListAsync());
        rows.AddRange(await db.TechniqueCategories.OrderBy(e => e.Id)
            .Select(e => $"category {e.Id} {e.Code} {e.Name} {e.SortOrder}").ToListAsync());
        rows.AddRange(await db.TechniqueCategoryDisciplines.OrderBy(e => e.TechniqueCategoryId).ThenBy(e => e.DisciplineId)
            .Select(e => $"category link {e.TechniqueCategoryId} {e.DisciplineId}").ToListAsync());
        rows.AddRange(await db.Techniques.OrderBy(e => e.Id)
            .Select(e => $"technique {e.Id} {e.Code} {e.Name} {e.SortOrder} {e.CategoryId}").ToListAsync());
        rows.AddRange(await db.Positions.OrderBy(e => e.Id)
            .Select(e => $"position {e.Id} {e.Code} {e.Name} {e.SortOrder}").ToListAsync());
        rows.AddRange(await db.PositionDisciplines.OrderBy(e => e.PositionId).ThenBy(e => e.DisciplineId)
            .Select(e => $"position link {e.PositionId} {e.DisciplineId}").ToListAsync());
        rows.AddRange(await db.MaterialCategories.OrderBy(e => e.Id)
            .Select(e => $"material category {e.Id} {e.Code} {e.Name} {e.SortOrder}").ToListAsync());
        return rows;
    }

    // Reads the seed files embedded in the Data assembly (the same files the seeder uses)
    private static JsonDocument ReadSeedFile(string fileName)
    {
        using var stream = typeof(AppDbContext).Assembly.GetManifestResourceStream($"Seed.{fileName}")
            ?? throw new InvalidOperationException($"Seed file '{fileName}' not found.");
        return JsonDocument.Parse(stream);
    }

    private static List<string> CodesOf(IEnumerable<JsonElement> entries) =>
        entries.Select(e => e.GetProperty("code").GetString()!).ToList();
}
